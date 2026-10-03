using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

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
        // Color que vale ahora: el de cartaArriba, o el elegido si es un comodin.
        // Se guarda aparte para no modificar la carta (los comodines no tienen color propio).
        private string colorActual;
        private int idPartida;
        private bool partidaEnCurso;

        // Indice (en jugadores) de quien tiene el turno, y sentido del juego:
        // 1 = posiciones ascendentes (1, 2, 3, 4), -1 = descendentes (4, 3, 2, 1)
        private int turno;
        private int direccion = 1;

        // Controles de cada jugador, en orden de posicion:
        // 1 abajo, 2 derecha, 3 arriba, 4 izquierda
        private Label[] etiquetasNombre;
        private Label[] indicadoresTurno;
        private PictureBox[] picturesMano;
        private PictureBox pictureBoxDescarte;

        // TODO (Parte 3): contenedores donde se mostraran las cartas de cada jugador
        // como imagenes individuales, con doble clic para jugar.
        // private FlowLayoutPanel[] manosJugadores;

        public Ventana()
        {
            InitializeComponent();

            etiquetasNombre = new[] { lblJugador1, lblJugador2, lblJugador3, lblJugador4 };
            indicadoresTurno = etiquetasNombre.Select(_ => CrearIndicadorTurno()).ToArray();

            picturesMano = new[] { pictureBox1, pictureBox2, pictureBox5, pictureBox3 };
            pictureBoxDescarte = pictureBox4;

            label1.Text = "";
            label1.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            ActualizarTurno();
            Load += (s, e) => AcomodarLayout();

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
                colorActual = cartaArriba.Color;

                idPartida = BaseDatos.RegistrarPartida(jugadores, cartaArriba);
                partidaEnCurso = true;

                // El orden de los turnos sale de la posicion guardada en partida_jugador
                var posiciones = BaseDatos.ObtenerPosiciones(idPartida);
                foreach (var jugador in jugadores)
                    jugador.Posicion = posiciones[jugador.Id];
                jugadores = jugadores.OrderBy(j => j.Posicion).ToList();
            }
            catch (ErrorBaseDatos ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            // La carta inicial siempre es de numero: empieza la posicion 1 en sentido 1→2→3→4
            direccion = 1;
            turno = 0;
            MostrarManos();
            MostrarDescarte();
            ActualizarTurno();
        }

        /* ============================================================
         * PENDIENTE (Parte 3): reemplazar JugarCarta y RobarCarta por la
         * logica de doble clic sobre las cartas mostradas en pantalla,
         * una vez que existan los FlowLayoutPanel de cada jugador.
         * Se dejan comentadas para no perder la logica de validacion.
         * ============================================================

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

            bool esComodin = (carta.Efecto != null && (carta.Efecto.Equals("cambiar_color") || carta.Efecto.Equals("+4")));
            bool coincideColor = (carta.Color != null && carta.Color.Equals(colorActual));
            bool coincideNumero = (carta.Numero != null && carta.Numero == cartaArriba.Numero);
            bool coincideEfecto = (carta.Efecto != null && carta.Efecto.Equals(cartaArriba.Efecto));

            if (!esComodin && !coincideColor && !coincideNumero && !coincideEfecto)
            {
                MessageBox.Show($"No puedes jugar {carta} sobre {cartaArriba}.\n" +
                    $"Debe ser color {colorActual}, el mismo número o símbolo, o un comodín.",
                    "Jugada inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string colorElegido = null;
            if (esComodin)
            {
                colorElegido = PedirColor();
                if (colorElegido == null)
                    return;
            }

            try
            {
                BaseDatos.RegistrarJugada(idPartida, jugador.Id, "tirar", carta.Id, colorElegido);
            }
            catch (ErrorBaseDatos ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            jugador.Mano.Remove(carta);
            RegresarAlMazo(cartaArriba);
            MostrarMano(posicion);

            if (jugador.Mano.Count == 0)
                TerminarConGanador(jugador);
            else
                AplicarEfecto(carta);
            cartaArriba = carta;
            colorActual = colorElegido ?? carta.Color;
            MostrarDescarte();
            ActualizarTurno();
        }

        private void RobarCarta(int posicion)
        {
            if (DarCartas(posicion, 1) == 0)
                return;

            var combo = combosMano[posicion];
            combo.SelectedIndex = combo.Items.Count - 1;

            ActualizarTurno();
        }

        private void MostrarCartaSeleccionada(int posicion)
        {
            if (combosMano[posicion].SelectedItem is Carta carta)
                MostrarCartaEnPictureBox(picturesMano[posicion], carta);
            else
                picturesMano[posicion].Image = null;
        }
        * ============================================================ */

        /// <summary>
        /// El jugador en turno pasa sin tirar. Solo se permite cuando el mazo esta vacio,
        /// para que un jugador sin cartas validas no se quede atorado.
        /// </summary>
        private void botonPasar_Click(object sender, EventArgs e)
        {
            try
            {
                BaseDatos.RegistrarJugada(idPartida, jugadores[turno].Id, "pasar", null);
            }
            catch (ErrorBaseDatos ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            turno = Siguiente(turno);
            ActualizarTurno();
        }

        /// <summary>
        /// Muestra la ventana para elegir color. Devuelve null si se cerro sin elegir.
        /// </summary>
        private string PedirColor()
        {
            using (FormElegirColor ventanaElegirColor = new FormElegirColor())
            {
                if (ventanaElegirColor.ShowDialog(this) != DialogResult.OK)
                    return null;

                string nuevoColor = ventanaElegirColor.getColor();
                MessageBox.Show("El nuevo color es: " + nuevoColor);
                return nuevoColor;
            }
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
                    DarCartas(Siguiente(turno), 4);
                    turno = Siguiente(turno, 2);
                    break;
                default:
                    turno = Siguiente(turno);
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
                catch (ErrorBaseDatos ex)
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
            catch (ErrorBaseDatos ex)
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
            catch (ErrorBaseDatos ex)
            {
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
        /// Solo puede ser una carta ordinaria (numero): si sale un comodin o una especial,
        /// se regresa al mazo y se voltea otra. Siempre quedan ordinarias en el mazo
        /// despues de repartir (hay 36 ordinarias y solo 24 especiales).
        /// </summary>
        private Carta SacarCartaInicial()
        {
            var carta = mazo[0];
            while (carta.Tipo != "ordinaria")
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

        /// <summary>
        /// PENDIENTE (Parte 3): hoy no hace nada visual, porque los combo box ya no existen.
        /// Aqui se va a mostrar la mano del jugador como imagenes dentro de su FlowLayoutPanel.
        /// </summary>
        private void MostrarMano(int posicion)
        {
            // TODO Parte 3: recorrer jugadores[posicion].Mano y agregar un PictureBox
            // por carta dentro de manosJugadores[posicion], con doble clic para jugarla.
        }

        /// <summary>
        /// Muestra la carta de arriba en label1, con el fondo del color que esta en juego.
        /// Si es un comodin, tambien se escribe el color elegido.
        /// </summary>
        private void MostrarDescarte()
        {
            label1.Text = cartaArriba.ToString();
            if (cartaArriba.Color == null && colorActual != null)
                label1.Text += " → " + colorActual;

            switch (colorActual)
            {
                case "rojo": label1.BackColor = Color.DarkRed; label1.ForeColor = Color.White; break;
                case "azul": label1.BackColor = Color.RoyalBlue; label1.ForeColor = Color.White; break;
                case "verde": label1.BackColor = Color.ForestGreen; label1.ForeColor = Color.White; break;
                case "amarillo": label1.BackColor = Color.Gold; label1.ForeColor = Color.Black; break;
                default: label1.BackColor = Color.Transparent; label1.ForeColor = Color.Black; break;
            }
            MostrarCartaEnPictureBox(pictureBoxDescarte, cartaArriba, colorActual);
        }

        /// <summary>
        /// Resalta al jugador en turno con el circulo amarillo junto a su nombre.
        /// "Pasar" solo se activa cuando ya no quedan cartas en el mazo.
        /// </summary>
        private void ActualizarTurno()
        {
            for (int i = 0; i < NumJugadores; i++)
            {
                bool enTurno = partidaEnCurso && i == turno;
                indicadoresTurno[i].Visible = enTurno;

                if (enTurno)
                {
                    var etiqueta = etiquetasNombre[i];
                    indicadoresTurno[i].Location = new Point(
                        etiqueta.Right + 2,
                        etiqueta.Top + (etiqueta.Height - indicadoresTurno[i].Height) / 2);
                }
            }

            botonPasar.Enabled = partidaEnCurso && mazo.Count == 0;

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

        private static void MostrarErrorBaseDatos(ErrorBaseDatos ex)
        {
            MessageBox.Show("Error con la base de datos:\n" + ex.Message,
                "UNO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private static string NombreArchivo(Carta carta, string colorMostrar = null)
        {
            if (carta.Tipo == "ordinaria")
                return $"{carta.Color}_{carta.Numero}.png";

            string color = carta.Color ?? colorMostrar;

            switch (carta.Efecto)
            {
                case "cambiar_color": return "comodin_color.png";
                case "+4": return "comodin_mas4.png";
                case "bloquear": return $"{color}_bloqueo.png";
                case "cambiar_direccion": return $"{color}_reversa.png";
                case "+2": return $"{color}_mas2.png";
                default: return "comodin_color.png";
            }
        }

        private static Image ObtenerImagenCarta(Carta carta, string colorMostrar = null)
        {
            string ruta = Path.Combine(Application.StartupPath, "Cartas", NombreArchivo(carta, colorMostrar));
            return File.Exists(ruta) ? Image.FromFile(ruta) : null;
        }

        private static void MostrarCartaEnPictureBox(PictureBox pb, Carta carta, string colorMostrar = null)
        {
            pb.Image?.Dispose();
            pb.Image = ObtenerImagenCarta(carta, colorMostrar);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
        }

        /// <summary>
        /// Reacomoda los controles segun el tamaño real de la pantalla (tras maximizar),
        /// porque sus posiciones de diseño son fijas y quedarian apretadas en una esquina.
        /// </summary>
        private void AcomodarLayout()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // Jugador 1 (abajo, centro)
            pictureBox1.Location = new Point(w / 2 - pictureBox1.Width / 2, h - 230);
            lblJugador1.Location = new Point(pictureBox1.Right + 15, pictureBox1.Top + pictureBox1.Height / 2 - 8);

            // Jugador 2 (derecha, centro vertical)
            pictureBox2.Location = new Point(w - 230, h / 2 - pictureBox2.Height / 2);
            lblJugador2.Location = new Point(pictureBox2.Left - 90, pictureBox2.Top + pictureBox2.Height / 2 - 8);

            // Jugador 3 (arriba, centro)
            pictureBox5.Location = new Point(w / 2 - pictureBox5.Width / 2, 20);
            lblJugador3.Location = new Point(pictureBox5.Right + 15, pictureBox5.Top + pictureBox5.Height / 2 - 8);

            // Jugador 4 (izquierda, centro vertical)
            pictureBox3.Location = new Point(130, h / 2 - pictureBox3.Height / 2);
            lblJugador4.Location = new Point(pictureBox3.Right + 15, pictureBox3.Top + pictureBox3.Height / 2 - 8);

            // Centro: mazo y descarte
            pictureBox4.Location = new Point(w / 2 - pictureBox4.Width / 2 - 60, h / 2 - pictureBox4.Height / 2);
            pictureBox6.Location = new Point(w / 2 - pictureBox6.Width / 2 + 60, h / 2 - pictureBox6.Height / 2);
            label1.Location = new Point(pictureBox4.Left, pictureBox4.Bottom + 10);
            botonPasar.Location = new Point(w / 2 - botonPasar.Width / 2, h / 2 + 90);

            // "Empezar juego" en la esquina
            button9.Location = new Point(w - button9.Width - 30, 20);
        }

    }
}