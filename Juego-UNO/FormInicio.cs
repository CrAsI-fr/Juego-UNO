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
    public partial class FormInicio : Form
    {
        public FormInicio()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Ventana ventanaJuego = new Ventana();
            ventanaJuego.button9_Click(sender, e);
            ventanaJuego.ShowDialog();
            this.SuspendLayout();
            this.Close();
        }
    }
}
