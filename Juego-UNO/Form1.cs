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

        private List<Jugador> jugadores = new List<Jugador>();
        private List<Carta> mazo = new List<Carta>();
        // Pila de descarte; la ultima carta es la que esta boca arriba (label1)
        private List<Carta> descarte = new List<Carta>();
        private int idPartida;
        private bool partidaEnCurso;

        // Controles de cada jugador, en orden de posicion:
        // 1 abajo, 2 derecha, 3 arriba, 4 izquierda
        private ComboBox[] combosMano;
        private Label[] etiquetasNombre;
        private Button[] botonesJugar;
        private Button[] botonesRobar;

        public Ventana()
        {
            InitializeComponent();

            combosMano = new[] { comboBox1, comboBox2, comboBox3, comboBox4 };
            etiquetasNombre = new[] { lblJugador1, lblJugador2, lblJugador3, lblJugador4 };
            botonesJugar = new[] { button1, button4, button7, button6 };
            botonesRobar = new[] { button2, button3, button8, button5 };

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
            HabilitarBotonesJugadores(false);

            FormClosing += Ventana_FormClosing;
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

                descarte = new List<Carta> { SacarCartaInicial() };

                idPartida = BaseDatos.RegistrarPartida(jugadores, descarte.Last());
                partidaEnCurso = true;
            }
            catch (MySqlException ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            MostrarManos();
            MostrarDescarte();
            HabilitarBotonesJugadores(true);
        }

        /// <summary>
        /// El jugador tira la carta seleccionada en su combo a la pila de descarte.
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

            jugador.Mano.Remove(carta);
            descarte.Add(carta);
            MostrarMano(posicion);
            MostrarDescarte();

            if (jugador.Mano.Count == 0)
                TerminarConGanador(jugador);
        }

        /// <summary>
        /// El jugador toma la carta de arriba del mazo (que esta barajado, asi que es aleatoria).
        /// </summary>
        private void RobarCarta(int posicion)
        {
            if (mazo.Count == 0)
                RellenarMazo();

            if (mazo.Count == 0)
            {
                MessageBox.Show("Ya no quedan cartas para robar.", "UNO",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var jugador = jugadores[posicion];
            var carta = mazo[0];

            try
            {
                BaseDatos.RegistrarJugada(idPartida, jugador.Id, "robar", carta.Id);
            }
            catch (MySqlException ex)
            {
                MostrarErrorBaseDatos(ex);
                return;
            }

            mazo.RemoveAt(0);
            jugador.Mano.Add(carta);
            MostrarMano(posicion);
            combosMano[posicion].SelectedItem = carta;
            ActualizarTitulo();
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
            HabilitarBotonesJugadores(false);
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
        /// Voltea la carta de arriba del mazo para iniciar la pila de descarte.
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
        /// Cuando se acaba el mazo, se barajan las cartas del descarte (menos la de arriba)
        /// y pasan a ser el nuevo mazo.
        /// </summary>
        private void RellenarMazo()
        {
            if (descarte.Count <= 1)
                return;

            var cartaArriba = descarte.Last();
            mazo = descarte.Take(descarte.Count - 1).ToList();
            Barajar(mazo);
            descarte = new List<Carta> { cartaArriba };
        }

        private void MostrarManos()
        {
            for (int i = 0; i < jugadores.Count; i++)
            {
                etiquetasNombre[i].Text = jugadores[i].Nombre;
                MostrarMano(i);
            }
            ActualizarTitulo();
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
            label1.Text = descarte.Last().ToString();
        }

        private void ActualizarTitulo()
        {
            Text = $"UNO - Partida {idPartida} ({mazo.Count} cartas en el mazo)";
        }

        private void HabilitarBotonesJugadores(bool habilitar)
        {
            foreach (var boton in botonesJugar.Concat(botonesRobar))
                boton.Enabled = habilitar;
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
