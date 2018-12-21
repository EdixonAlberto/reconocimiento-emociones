using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace reconocimiento_emociones
{
    public partial class experto : Form
    {
        public experto()
        {
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "14100596")
            {
                MessageBox.Show("¡Se ha habilitado la Base de Datos!'", "INGRESAR", MessageBoxButtons.OK,MessageBoxIcon.Information);
                label4.Text = "Correcto";
                Close();
            }
            else
            {
                MessageBox.Show("Contraseña Incorrecta, Intentelo de Nuevo'", "INGRESAR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                label4.Text = "Incorrecto";
                textBox1.Text = "";
                textBox1.Focus();
            }
        }

        private void experto_Load(object sender, EventArgs e)
        {
            label4.Text = "";
            textBox1.Text = "";
            textBox1.PasswordChar = '*';
            textBox1.MaxLength = 8;
        }
    }
}
