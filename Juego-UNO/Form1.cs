using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Juego_UNO
{
    public partial class Ventana : Form
    {
        private const int NumJugadores = 4;
        private const int CartasPorJugador = 7;

        private static readonly Random aleatorio = new Random();

        // Ordenados por su posicion en la mesa (partida_jugador.posicion)
        private List<Jugador> jugadores = new List<Jugador>();
        private List<Carta> mazo = new List<Carta>();
        // Carta boca arriba (label1). Al jugarse otra encima, esta regresa al mazo.
        private Carta cartaArriba;
        private int idPartida;
        private bool partidaEnCurso;

        // Indice (en jugadores) de quien tiene el turno, y sentido del juego:
        // 1 = posiciones ascendentes (1, 2, 3, 4), -1 = descendentes (4, 3, 2, 1)
        private int turno;
        private int direccion = 1;

        // Controles de cada jugador, en orden de posicion:
        // 1 abajo, 2 derecha, 3 arriba, 4 izquierda
        private ComboBox[] combosMano;
        private Label[] etiquetasNombre;
        private Label[] indicadoresTurno;
        private Button[] botonesJugar;
        private Button[] botonesRobar;

        public Ventana()
        {
            InitializeComponent();

            combosMano = new[] { comboBox1, comboBox2, comboBox3, comboBox4 };
            etiquetasNombre = new[] { lblJugador1, lblJugador2, lblJugador3, lblJugador4 };
            botonesJugar = new[] { button1, button4, button7, button6 };
            botonesRobar = new[] { button2, button3, button8, button5 };
            indicadoresTurno = etiquetasNombre.Select(_ => CrearIndicadorTurno()).ToArray();

            // Solo se puede elegir una carta de la lista, no escribir texto.
            // La lista desplegable es mas ancha que el combo para que quepan nombres largos.
            foreach (var combo in combosMano)
            {
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.DropDownWidth = 200;
            }

            // Cada boton sabe a que jugador pertenece por su posicion en el arreglo
            for (int i = 0; i < NumJugadores; i++)
            {
                int posicion = i;
                botonesJugar[i].Click += (s, e) => JugarCarta(posicion);
                botonesRobar[i].Click += (s, e) => RobarCarta(posicion);
            }

            label1.Text = "";
            label1.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            ActualizarTurno();

            FormClosing += Ventana_FormClosing;
        }

        /// <summary>
        /// Circulo amarillo que se muestra junto al nombre del jugador en turno.
        /// </summary>
        private Label CrearIndicadorTurno()
        {
            var indicador = new Label
            {
                Text = "●",
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                AutoSize = true,
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                Visible = false
            };
            Controls.Add(indicador);
            indicador.BringToFront();
            return indicador;
        }

        // Empezar juego
        private void button9_Click(object sender, EventArgs e)
        {
            // Se sugieren los nombres de la partida anterior, si la hubo
            List<string> nombres;
            using (var dialogo = new FormNombres(NumJugadores, jugadores.Select(j => j.Nombre).ToList()))
            {
                if (dialogo.ShowDialog(this) != DialogResult.OK)
                    return;
                nombres = dialogo.Nombres;
            }

            try
            {
                // Si habia una partida sin terminar, queda abandonada
                if (partidaEnCurso)
                {
                    BaseDatos.TerminarPartida(idPartida, null);
                    partidaEnCurso = false;
                }

                jugadores = nombres.Select(BaseDatos.ObtenerOCrearJugador).ToList();

                mazo = BaseDatos.ObtenerCartas();
                Barajar(mazo);
                Repartir();

                cartaArriba = SacarCartaInicial();

                idPartida = BaseDatos.RegistrarPartida(jugadores, cartaArriba);
                partidaEnCurso = true;

                // El orden de los turnos sale de la posicion guardada en partida_jugador
                var posiciones = BaseDatos.ObtenerPosiciones(idPartida);
                foreach (var jugador in jugadores)
                    jugador.Posicion = posiciones[jugador.Id];
                jugadores = jugadores.OrderBy(j => j.Posicion).ToList();
            }
            catch (MySqlException ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            AplicarCartaInicial();
            MostrarManos();
            MostrarDescarte();
            ActualizarTurno();
        }

        /// <summary>
        /// El jugador tira la carta seleccionada en su combo sobre label1.
        /// La carta que estaba arriba regresa al mazo en una posicion aleatoria.
        /// </summary>
        private void JugarCarta(int posicion)
        {
            var jugador = jugadores[posicion];
            var carta = combosMano[posicion].SelectedItem as Carta;
            if (carta == null)
            {
                MessageBox.Show($"{jugador.Nombre}, selecciona una carta para jugar.", "UNO",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                BaseDatos.RegistrarJugada(idPartida, jugador.Id, "tirar", carta.Id);
            }
            catch (MySqlException ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }
            if (carta.Efecto != null && carta.Efecto.Equals("cambiar_color"))
            {

            }else if (!cartaArriba.Color.Equals(carta.Color))
            {
                return;
            }
            jugador.Mano.Remove(carta);
            RegresarAlMazo(cartaArriba);
            cartaArriba = carta;
            MostrarMano(posicion);
            MostrarDescarte();

            if (jugador.Mano.Count == 0)
                TerminarConGanador(jugador);
            else
                AplicarEfecto(carta);

            ActualizarTurno();
        }

        /// <summary>
        /// El jugador en turno roba una carta del mazo. Puede robar las veces que quiera;
        /// su turno solo termina cuando juega una carta.
        /// </summary>
        private void RobarCarta(int posicion)
        {
            if (DarCartas(posicion, 1) == 0)
                return;

            // Queda seleccionada la carta recien robada
            var combo = combosMano[posicion];
            combo.SelectedIndex = combo.Items.Count - 1;
            ActualizarTitulo();
        }

        /// <summary>
        /// Pasa el turno segun la carta que se acaba de jugar.
        /// </summary>
        private void AplicarEfecto(Carta carta)
        {
            switch (carta.Efecto)
            {
                case "cambiar_direccion":
                    direccion = -direccion;
                    turno = Siguiente(turno);
                    break;

                case "bloquear":
                    turno = Siguiente(turno, 2);
                    break;

                case "+2":
                    DarCartas(Siguiente(turno), 2);
                    turno = Siguiente(turno, 2);
                    break;
                case "+4":
                    // El siguiente roba y pierde su turno
                    DarCartas(Siguiente(turno), 4);
                    turno = Siguiente(turno, 2);
                    break;

                default:
                    turno = Siguiente(turno);
                    break;
            }
        }

        /// <summary>
        /// Define quien empieza segun la carta volteada al inicio (reglas oficiales).
        /// El que reparte es el ultimo en la mesa; empieza el que sigue de el.
        /// </summary>
        private void AplicarCartaInicial()
        {
            direccion = 1;
            turno = 0;

            switch (cartaArriba.Efecto)
            {
                case "cambiar_direccion":
                    // Empieza el que reparte y el juego va en sentido contrario
                    direccion = -1;
                    turno = jugadores.Count - 1;
                    break;

                case "bloquear":
                    turno = Siguiente(0);
                    break;

                case "+2":
                    DarCartas(0, 2);
                    turno = Siguiente(0);
                    break;
            }
        }

        /// <summary>
        /// Indice del jugador que esta 'pasos' lugares despues de 'desde', en el sentido actual.
        /// </summary>
        private int Siguiente(int desde, int pasos = 1)
        {
            int n = jugadores.Count;
            return ((desde + direccion * pasos) % n + n) % n;
        }

        /// <summary>
        /// Da 'cantidad' cartas del mazo al jugador y las registra en el log.
        /// Devuelve cuantas se pudieron dar.
        /// </summary>
        private int DarCartas(int posicion, int cantidad)
        {
            var jugador = jugadores[posicion];
            int dadas = 0;

            for (int i = 0; i < cantidad; i++)
            {
                // Solo pasa si todas las cartas estan en las manos de los jugadores
                if (mazo.Count == 0)
                {
                    MessageBox.Show("Ya no quedan cartas para robar.", "UNO",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }

                var carta = mazo[0];
                try
                {
                    BaseDatos.RegistrarJugada(idPartida, jugador.Id, "robar", carta.Id);
                }
                catch (MySqlException ex)
                {
                    MostrarErrorBaseDatos(ex);
                    break;
                }

                mazo.RemoveAt(0);
                jugador.Mano.Add(carta);
                dadas++;
            }

            if (dadas > 0)
                MostrarMano(posicion);
            return dadas;
        }

        private void TerminarConGanador(Jugador ganador)
        {
            try
            {
                BaseDatos.TerminarPartida(idPartida, ganador.Id);
            }
            catch (MySqlException ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            partidaEnCurso = false;
            ActualizarTurno();
            MessageBox.Show($"¡{ganador.Nombre} ganó la partida!", "UNO",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Al cerrar el programa, la partida en curso queda terminada sin ganador
        private void Ventana_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!partidaEnCurso)
                return;

            try
            {
                BaseDatos.TerminarPartida(idPartida, null);
            }
            catch (MySqlException ex)
            {
                // No se impide cerrar; solo se avisa
                MostrarErrorBaseDatos(ex);
            }
        }

        /// <summary>
        /// Mezcla la lista al azar (algoritmo Fisher-Yates).
        /// </summary>
        private static void Barajar(List<Carta> cartas)
        {
            for (int i = cartas.Count - 1; i > 0; i--)
            {
                int j = aleatorio.Next(i + 1);
                var temporal = cartas[i];
                cartas[i] = cartas[j];
                cartas[j] = temporal;
            }
        }

        /// <summary>
        /// Reparte una carta a cada jugador por vuelta, tomandolas del tope del mazo.
        /// Las cartas que sobran se quedan en el mazo para robar.
        /// </summary>
        private void Repartir()
        {
            for (int vuelta = 0; vuelta < CartasPorJugador; vuelta++)
            {
                foreach (var jugador in jugadores)
                {
                    jugador.Mano.Add(mazo[0]);
                    mazo.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Voltea la carta de arriba del mazo para empezar el juego.
        /// Segun las reglas oficiales, si sale un +4 se regresa al mazo y se voltea otra.
        /// </summary>
        private Carta SacarCartaInicial()
        {
            var carta = mazo[0];
            while (carta.Efecto == "+4")
            {
                mazo.RemoveAt(0);
                mazo.Insert(aleatorio.Next(1, mazo.Count + 1), carta);
                carta = mazo[0];
            }
            mazo.RemoveAt(0);
            return carta;
        }

        /// <summary>
        /// Mete la carta al mazo en una posicion aleatoria, para que robar siga siendo al azar.
        /// </summary>
        private void RegresarAlMazo(Carta carta)
        {
            mazo.Insert(aleatorio.Next(mazo.Count + 1), carta);
        }

        private void MostrarManos()
        {
            for (int i = 0; i < jugadores.Count; i++)
            {
                etiquetasNombre[i].Text = jugadores[i].Nombre;
                MostrarMano(i);
            }
        }

        private void MostrarMano(int posicion)
        {
            var combo = combosMano[posicion];
            combo.Items.Clear();
            foreach (var carta in jugadores[posicion].Mano)
                combo.Items.Add(carta);
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void MostrarDescarte()
        {
            label1.Text = cartaArriba.ToString();
        }

        /// <summary>
        /// Solo el jugador en turno tiene sus botones activos y el circulo amarillo junto a su nombre.
        /// </summary>
        private void ActualizarTurno()
        {
            for (int i = 0; i < NumJugadores; i++)
            {
                bool enTurno = partidaEnCurso && i == turno;
                botonesJugar[i].Enabled = enTurno;
                botonesRobar[i].Enabled = enTurno;
                indicadoresTurno[i].Visible = enTurno;

                if (enTurno)
                {
                    // Se acomoda a la derecha del nombre, que cambia de largo
                    var etiqueta = etiquetasNombre[i];
                    indicadoresTurno[i].Location = new Point(
                        etiqueta.Right + 2,
                        etiqueta.Top + (etiqueta.Height - indicadoresTurno[i].Height) / 2);
                }
            }

            ActualizarTitulo();
        }

        private void ActualizarTitulo()
        {
            if (!partidaEnCurso)
            {
                Text = idPartida == 0 ? "UNO" : $"UNO - Partida {idPartida} terminada";
                return;
            }

            string sentido = direccion == 1 ? "1→2→3→4" : "4→3→2→1";
            Text = $"UNO - Partida {idPartida} | Turno de {jugadores[turno].Nombre} | " +
                   $"Sentido {sentido} | {mazo.Count} cartas en el mazo";
        }

        private static void MostrarErrorBaseDatos(MySqlException ex)
        {
            MessageBox.Show("Error con la base de datos:\n" + ex.Message,
                "UNO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }
    }
}
