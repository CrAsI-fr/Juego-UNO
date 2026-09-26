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
        private int idPartida;

        // Combo y etiqueta de cada jugador, en orden de posicion (jugador 1 abajo)
        private ComboBox[] combosMano;
        private Label[] etiquetasNombre;

        public Ventana()
        {
            InitializeComponent();

            combosMano = new[] { comboBox1, comboBox2, comboBox3, comboBox4 };
            etiquetasNombre = new[] { lblJugador1, lblJugador2, lblJugador3, lblJugador4 };

            // Solo se puede elegir una carta de la lista, no escribir texto.
            // La lista desplegable es mas ancha que el combo para que quepan nombres largos.
            foreach (var combo in combosMano)
            {
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.DropDownWidth = 200;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

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
                jugadores = nombres.Select(BaseDatos.ObtenerOCrearJugador).ToList();

                mazo = BaseDatos.ObtenerCartas();
                Barajar(mazo);
                Repartir();

                idPartida = BaseDatos.RegistrarPartida(jugadores);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error con la base de datos:\n" + ex.Message,
                    "UNO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MostrarManos();
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

        private void MostrarManos()
        {
            for (int i = 0; i < jugadores.Count; i++)
            {
                etiquetasNombre[i].Text = jugadores[i].Nombre;

                var combo = combosMano[i];
                combo.Items.Clear();
                foreach (var carta in jugadores[i].Mano)
                    combo.Items.Add(carta);
                combo.SelectedIndex = 0;
            }

            Text = $"UNO - Partida {idPartida} ({mazo.Count} cartas en el mazo)";
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {

        }
    }
}
