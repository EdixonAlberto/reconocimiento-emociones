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
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Data.OleDb;
using System.IO;

namespace reconocimiento_emociones
{
    public partial class Form1 : Form
    {
        OpenFileDialog explore = new OpenFileDialog();
        Capture grabber;
        Image<Bgr, byte> imag, imag2, fracmento;
        HaarCascade detector;
        MCvAvgComp[][] imag_detec;
        DenseHistogram hi;
        Image<Gray, byte> imag_gris, boca, ojos, recorte, recorte_bit;

        OleDbConnection conec; //Para la coneccion con la BD

        int num, emo_ojos, emo_boca;
        double recort_pro, boca_pro, ojos_pro;
        string emo, memoria;

        //Formularios
        Base_de_Datos bd = new Base_de_Datos();
        Inputbox ip = new Inputbox();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            limpiar(); //Proceso de reiniciar componentes en el form
        }

        //CARGAR IMAGEN
        private void button1_Click(object sender, EventArgs e)
        {
            num++;
            explore.DefaultExt = ".jpg";
            explore.Filter = "Images (*.jpg)|*.jpg|All files (*.*)|*.*";
            if (explore.ShowDialog() == DialogResult.OK) //Se abre el esplorador de archivos
            {
                limpiar();
                memoria = explore.FileName;
                imag = new Image<Bgr, byte>(memoria); //Se guarda la imagen en un formato Bgr
                pictureBox1.Image = imag.ToBitmap(); //otra forma: imageBox1.Image = imag;
                button2.Enabled = true;
            }
            else button1.Focus();
        }

        //PROCESAR IMAGEN (CONVERCIONES)
        private void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Increment(10);

            //DETECTOR DE CARAS
            if (memoria != "")
            {
                imag = new Image<Bgr, byte>(memoria);
            }
            
            imag_gris = imag.Convert<Gray, byte>(); //Se convierte la imagen a gris
            hi = new DenseHistogram(255, new RangeF(0, 255));
            hi.Calculate(new Image<Gray, byte>[] { imag_gris }, true, null); //Se calcula el histograma de la imagen en gris, para mejorarla

            detector = new HaarCascade("haarcascade_frontalface_default.xml"); //Se carga el clasificador de rostros
            imag2 = imag;
            reconocer(1.1); //Cara recortada en gris

            pictureBox1.Image = imag.ToBitmap(); //Se muestra la imagen con la CARA reconocida

            progressBar1.Increment(10);

            //CARA
            imag_gris = recorte;
            hi.Calculate(new Image<Gray, byte>[] { imag_gris }, true, null); //Se calcula el histograma de la imagen en gris, para mejorarla

            pictureBox2.Image = imag_gris.ToBitmap(); //Se muestra solo la cara reconocida en gris
            pictureBox3.Image = recorte_bit.ToBitmap(); //Se muestra solo la cara reconocida en binario

            //BOCA
            imag_gris = imag.Convert<Gray, byte>(); //generamos la imagen en gris original
            detector = new HaarCascade("haarcascade_mcs_mouth.xml");
            boca_pro = recort_pro;
            imag2 = imag;
            boca = reconocer(3.5);

            if (boca != null)
            {
                pictureBox4.Image = boca.ToBitmap();
                label3.Text = "Boca Detectada con Exito";
                progressBar1.Increment(10);
            }
            else
            {
                label3.BackColor = Color.Blue;
                label3.Text = "No Detectado";
                progressBar1.Increment(10);
            }

            //OJOS
            detector = new HaarCascade("haarcascade_mcs_eyepair_big.xml");
            ojos_pro = recort_pro;
            imag2 = imag;
            ojos = reconocer(1.1);
            if (ojos != null)
            {
                pictureBox5.Image = ojos.ToBitmap();
                label4.Text = "Ojos Detectados con Exito";
                progressBar1.Increment(10);
            }
            else
            {
                label4.BackColor = Color.Blue;
                label4.Text = "No Detectado";
                progressBar1.Increment(10);
            }
            progressBar1.Increment(100);
            button3.Enabled = true;
        }

        //EMOCION
        private void button3_Click(object sender, EventArgs e)
        {
            emo_boca = Convert.ToInt16(boca_pro); //Promedio de la boca en entero
            label1.Text = emo_boca.ToString();

            emo_ojos = Convert.ToInt16(ojos_pro); //Promedio de los ojos en entero
            label2.Text = Convert.ToInt16(ojos_pro).ToString();

            consulta();
            if (emo == "DIFUSO")
            {
                if (emo_boca > 154 && emo_ojos >= 180 || emo == "ALEGRE")
                {
                    pictureBox6.Image = Image.FromFile(Application.StartupPath + "/emoticones/alegre.jpg");
                    emo = "ALEGRE";
                }
                else if (emo_boca <= 156 && emo_ojos < 180 || emo == "NORMAL")
                {
                    pictureBox6.Image = Image.FromFile(Application.StartupPath + "/emoticones/normal.jpg");
                    emo = "NORMAL";
                }
            }
            if (emo == "DIFUSO")
            {
                if (MessageBox.Show("No se puede determinar la emocion" + "\n" + "¿Desea ingresar en modo experto, para poder introducir la emocion?", "DIFUSO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    experto ep = new experto();
                    ep.ShowDialog(this);
                    if (ep.label4.Text == "Correcto")
                    {
                        groupBox1.Enabled = true;
                        button7.Focus();
                    }
                }
                else { button2.Focus(); }
            }
            else if (emo == "ENOJADO")
            {
                pictureBox6.Image = Image.FromFile(Application.StartupPath + "/emoticones/enojado.jpg");
                label5.Text = emo;
            }
            else if (emo == "TRISTE")
            {
                pictureBox6.Image = Image.FromFile(Application.StartupPath + "/emoticones/triste.jpg");
                label5.Text = emo;
            }
            label5.Text = emo;
        }

       //CAMARA
        private void button4_Click(object sender, EventArgs e)
        {
            //Inicia captura de webcam
            grabber = new Capture();
            grabber.QueryFrame();
            //Inicializa el evento FrameGraber
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;
        }

        //CAPTURAR
        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            experto ep = new experto();
            ep.ShowDialog(this);
            if (ep.label4.Text == "Correcto")
            {
                groupBox1.Enabled = true;
                button7.Focus();
            }
        }

        //SALIR
        private void button6_Click(object sender, EventArgs e)
        {
            Close();
        }

        //GUARDAR BD
        private void button7_Click(object sender, EventArgs e)
        {
            //imag.Save(Application.StartupPath + "/imagenes/" + imag_nombre);
            try
            {
                ip.textBox2.Text = emo;
                ip.ShowDialog(this);
                File.Copy(explore.FileName, Application.StartupPath + "/imagenes/" + ip.textBox1.Text + ".jpg"); //Se guarda la imagen y se verifica que no exista

                insertar(ip.textBox1.Text, ip.textBox2.Text); //Proceso para insertar alores en la BD
                
                MessageBox.Show("Guardado con Exito", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                groupBox1.Enabled = false;
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("ya existe"))
                {
                    MessageBox.Show("Esta Imagen ya Existe en la (Base de Datos)", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    groupBox1.Enabled = false;
                }
            }
        }

        //BASE DE DATOS
        private void button8_Click(object sender, EventArgs e)
        {
            BD_conec();
            bd.ShowDialog(this);
            limpiar();
            memoria = (Application.StartupPath + "/imagenes/" + bd.label1.Text + ".jpg");
            imag = new Image<Bgr, byte>(memoria);
            pictureBox1.Image = imag.ToBitmap();
            button2.Enabled = true;
            conec.Close();
        }

        //Proceso RECONOCER
        private Image<Gray, byte> reconocer(double factor)
        {
            //Esto es si se quiere redimencionar la imagen
            //imag_histo = imag_histo.Resize(100, 100, INTER.CV_INTER_CUBIC);
            imag_detec = imag_gris.DetectHaarCascade(detector, factor, 10,
                    HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20)); //Se detecta la zona de interes por medio del detector
            recorte = null;

            foreach (MCvAvgComp cara in imag_detec[0]) //Se dibujan tantos rectangulos como zonas detectadas existan
            {
                recorte = imag2.Copy(cara.rect).Convert<Gray, byte>().Resize(200, 200, INTER.CV_INTER_CUBIC); //Se copia la region de interes
                imag2.Draw(cara.rect, new Bgr(Color.Red), 4);
            }

            if (recorte == null && factor < 10)
            {
                progressBar1.Increment(10);
                factor += 0.1;
                imag2 = imag;
                return reconocer(factor);
            }
            else if (factor >= 10) { return null; }
            else
            {
                recorte_bit = recorte.ThresholdBinary(new Gray(140), new Gray(255));
                recort_pro = promedio(recorte_bit);
                return recorte_bit;
            }
        }

        //FUNCION PROMEDIO
        private double promedio(Image<Gray, byte>  im)
        {
            double Red_avg = 0.0;
            for (int j = 0; j < im.Cols; j++)
            {
                for (int i = 0; i < im.Rows; i++)
                {
                    Red_avg += im.Data[i, j, 0];
                }
            }
            Red_avg = Red_avg / (im.Cols * im.Rows);
            return Red_avg;

            //if (Red_avg == ) 
            //
            //            down vote
            //Have a look at the Image.AvgSdv method in EMGU:

            //public void AvgSdv(
            //    out TColor avg,
            //    out MCvScalar sdv
        }

        //CONECCION CON BD
        public void BD_conec()
        {
            string coneccion = @"Provider=Microsoft.Jet.OLEDB.4.0;" +
            @"Data source=" + Application.StartupPath + "/imagenes.mdb;persist security info = false";

            conec = new OleDbConnection(coneccion); //variable de coneccion
            try
            {
                conec.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to data source" + ex.ToString());
            }
        }

        //CONSULTA
        private void consulta()
        {
            emo = "DIFUSO";
            //Se consulta la base de datos
            string orden = "SELECT emocion FROM modelos WHERE boca_pro =" + emo_boca + "";
            BD_conec();
            OleDbCommand cmd = new OleDbCommand(orden, conec);
            OleDbDataReader data = cmd.ExecuteReader(); //para leer los datos

            while (data.Read())
            {
                emo = data["emocion"].ToString();
            }
            conec.Close();
        }


        //INSERTAR
        private void insertar(string str1, string str2)
        {
            try
            {
                BD_conec();
                const string insert = @"insert into modelos(imagen, emocion, boca_pro, ojos_pro)" +
                    "values (@imag, @emo, @boca, @ojos)";
                OleDbCommand cmd = new OleDbCommand(insert, conec);
                cmd.Parameters.AddWithValue("@imag", str1);
                cmd.Parameters.AddWithValue("@emo", str2);
                cmd.Parameters.AddWithValue("@boca", boca_pro);
                cmd.Parameters.AddWithValue("@ojos", ojos_pro);
                int exe = cmd.ExecuteNonQuery();
                conec.Close();
            }
            catch (OleDbException ex)
            {
                MessageBox.Show("Ocurrió un error al Guardar los Datos en la BD", ex.Message, MessageBoxButtons.OK);
            }
        }


        private void limpiar()
        {
            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;
            groupBox1.Enabled = false;

            progressBar1.Value = 0;
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;
            pictureBox4.Image = null;
            pictureBox5.Image = null;
            pictureBox6.Image = null;

            label3.BackColor = Color.ForestGreen;
            label4.BackColor = Color.ForestGreen;
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
        }

        void FrameGrabber(object sender, EventArgs e)
        {
            //Tomamos el frame actual de la cámara
            fracmento = grabber.QueryFrame().Resize(320, 240, INTER.CV_INTER_CUBIC);

            //Lo paso a escala de grises
            imag_gris = fracmento.Convert<Gray, byte>();

            //Detecto rostros en la imagen
            detector = new HaarCascade("haarcascade_frontalface_default.xml");
            MCvAvgComp[][] facesDetected = imag_gris.DetectHaarCascade(detector, 1.2, 10, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            imag2 = fracmento;
            //Acciones para cada rostro detectado
            foreach (MCvAvgComp f in facesDetected[0])
            {
                num = num + 1;
                recorte = imag2.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                //Enmarco la cara
                imag2.Draw(f.rect, new Bgr(Color.Red), 2);
            }
            pictureBox1.Image = imag2.ToBitmap();
            imag = fracmento;
            memoria = "";
            button2.Enabled = true;
            button3.Enabled = true;
        }
    }
}