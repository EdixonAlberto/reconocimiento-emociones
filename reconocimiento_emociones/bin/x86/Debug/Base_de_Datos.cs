using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Data.OleDb;

namespace reconocimiento_emociones
{
    public partial class Base_de_Datos : Form
    {
        public Base_de_Datos()
        {
            InitializeComponent();
        }

        private void Base_de_Datos_Load(object sender, EventArgs e)
        {
            string coneccion = "Provider=Microsoft.Jet.OLEDB.4.0;" +
            "Data source=" + Application.StartupPath + "/imagenes.mdb;persist security info = false";

            //ds.Tables.Add("tablaA");
            string orden = "select * from modelos";
            OleDbDataAdapter adaptador = new OleDbDataAdapter(orden, coneccion);
            DataTable table = new DataTable("tablaA");
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;
            adaptador.Fill(table);
            dataGridView1.DataSource = table;

            dataGridView1.Columns[0].Width = 100;
            dataGridView1.Columns[1].Width = 100;

            label1.Text = "archivo1";
            pictureBox1.Image = Image.FromFile(Application.StartupPath + "/imagenes/" + label1.Text + ".jpg");
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //Antes que nada, ir a propiedades de grid y seleccionar "select mode" en "fullrowcell"
            label1.Text = dataGridView1.CurrentRow.Cells[0].Value.ToString();
            pictureBox1.Image = Image.FromFile(Application.StartupPath + "/imagenes/" + label1.Text + ".jpg");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
