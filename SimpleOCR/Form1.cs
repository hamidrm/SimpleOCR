using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        double[,] inputs=new double[24,24];
        double[,] weights = new double[24, 24];
        double[,] wc = new double[24, 24];
        double[] outputs_first = new double[24];
        double[,] hidden_inputs = new double[100, 24];
        double[,] hidden_weights = new double[100, 24];
        double[,] hidden_wc = new double[100, 24];
        double[] out_errors = new double[100];
        double[] outputs = new double[100];
        double learnrate = 0.01;
        double momentum = 0.9;
        bool drawing = false;
        string taa = "";
        Bitmap DrawingImage;
        string[] train_letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        public Form1()
        {
            InitializeComponent();
        }

        void loadFromFile()
        {
            FileStream fs = new FileStream("c:\\simple_ocr.bin", FileMode.OpenOrCreate, FileAccess.Read);
            BinaryReader bw = new BinaryReader(fs);
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    weights[i, j] = bw.ReadDouble();
                }
            }

            for (int ci = 0; ci < train_letters.Length; ci++)
            {
                for (int j = 0; j < 24; j++)
                {
                    hidden_weights[ci, j] = bw.ReadDouble();
                }
            }
            bw.Close();
            fs.Close();
        }

        void saveToFile()
        {
            FileStream fs = new FileStream("c:\\simple_ocr.bin", FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    bw.Write(weights[i, j]);
                }
            }

            for (int ci = 0; ci < train_letters.Length; ci++)
            {
                for (int j = 0; j < 24; j++)
                {
                    bw.Write(hidden_weights[ci, j]);
                }
            }
            bw.Close();
            fs.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Random r = new Random();
            //for (int letter = 0; letter < 7/*train_letters.Length*/; letter++)
            {
                //pictureBox1.Image = bitmap;
                Font[] f = new Font[3];
                f[0] = new Font("Tahoma", 16, FontStyle.Regular);
                f[1] = new Font("Arial", 16, FontStyle.Regular);
                f[2] = new Font("Times New Roman", 16, FontStyle.Regular);
                for (int epoch = 0; epoch < 10000; epoch++)
                {
                    int letter = r.Next(0, train_letters.Length);
                    string firstText = train_letters[letter];
                    Bitmap org, bitmap = new Bitmap(24, 24);
                    Graphics graphics = Graphics.FromImage(bitmap);
                    //if(r.Next(0,2)==0)
                    //    graphics.DrawString(firstText, TahomaFont, Brushes.Black, new PointF(0, 0));
                    //else
                    graphics.DrawString(firstText, f[1/*r.Next(0, 1)*/], Brushes.Black, new PointF(0, 0));
                    
                    string a = "";
                    Point v = getVL(bitmap);
                    Point h = getHL(bitmap);
                    org = bitmap;



                    Bitmap bitmap1 = new Bitmap(org,24,24);


                    Noise(bitmap1,70);
                    bitmap1 = bitmap.Clone(new Rectangle(h.X, v.X, h.Y - h.X, v.Y - v.X), bitmap1.PixelFormat);
                    bitmap1 = new Bitmap(bitmap1, 24, 24);
                    for (int i = 0; i < 24; i++)
                    {
                        for (int j = 0; j < 24; j++)
                        {
                            inputs[i, j] = -1.0;
                        }
                    }
                    for (int i = 0; i < 24; i++)
                    {
                        for (int j = 0; j < 24; j++)
                        {
                            if (bitmap1.GetPixel(j, i).ToArgb() == 0)
                                inputs[i, j] = -1.0;
                            else
                                inputs[i, j] = 1.0;
                        }
                    } 
                    annFillCells();
                    for (int i = 0; i < train_letters.Length; i++)
                    {
                        out_errors[i] = CalculateError(i == letter ? 1.0 : 0, outputs[i]);
                        for (int j = 0; j < 24; j++)
                        {
                            hidden_wc[i, j] = momentum * hidden_wc[i, j] + out_errors[i] * learnrate * outputs_first[j];
                            hidden_weights[i, j] += hidden_wc[i, j];
                        }
                    }

                    for (int i = 0; i < 24; i++)
                    {
                        for (int j = 0; j < 24; j++)
                        {
                            wc[i, j] = momentum * wc[i, j] + getHiddenErrorGradient(i) * learnrate * inputs[i, j];
                            weights[i, j] += wc[i, j];
                        }
                    }
                }
                annFillCells();

            }
            string ab = "";
            for (int i = 0; i < train_letters.Length; i++)
                ab += "o" + i + " = " + ((1-Math.Abs(out_errors[i])) * 100) + " % \r\n";
            textBox1.Text = ab;
        }
        double getHiddenErrorGradient(int j)
        {
            double weightedSum = 0;
            for (int k = 0; k < train_letters.Length; k++) weightedSum += hidden_weights[k, j] * out_errors[k];
            return outputs_first[j] * (1 - outputs_first[j]) * weightedSum;
        }
        void annFillCells()
        {
            taa = "";
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    if (inputs[i, j] == 1)
                        taa += "1";
                    else
                        taa += "0";
                }
                taa += "\r\n";
            }


            double sum;
            for (int i = 0; i < 24; i++)
            {
                sum = 0;
                for (int j = 0; j < 24; j++)
                {
                    sum += inputs[i, j] * weights[i, j];
                }
                outputs_first[i] = sigmoid(sum);
                for (int ci = 0; ci < train_letters.Length; ci++)
                    hidden_inputs[ci, i] = outputs_first[i];
            }

            for (int ci = 0; ci < train_letters.Length; ci++)
            {
                sum = 0;
                for (int i = 0; i < 24; i++)
                {
                    sum += hidden_inputs[ci, i] * hidden_weights[ci, i];
                }
                outputs[ci] = sigmoid(sum);

                //outputs[ci] = getRoundedOutputValue(outputs[ci]);
            }

            //for (int j = 0; j < train_letters.Length; j++)
            //{
            //    //if ( == 1)
            //        taa += getRoundedOutputValue(outputs[j]);
            //    //else
            //    //    taa += "0";
            //}
        }
        Point getVL(Bitmap img)
        {
            Point p=new Point(0,0);
            //bool FoundFirst = false;
            for (int i = 0; i < img.Height; i++)
                for (int j = 0; j < img.Width; j++)
                {
                    Color t = img.GetPixel(j, i);
                    if (t.A !=0 && t.R==0)
                    {
                        p.X = i;
                        j = img.Width;
                        i = img.Height;
                    }
                }
            for (int i = img.Height - 1; i >= 0; i--)
                for (int j = 0; j < img.Width; j++)
                {
                    Color t = img.GetPixel(j, i);
                    if (t.A != 0 && t.R == 0)
                    {
                        p.Y = i + 1;
                        j = img.Width;
                        i = -1;
                    }
                }
            return p;
        }
        Point getHL(Bitmap img)
        {
            Point p = new Point(0, 0);
            //bool FoundFirst = false;
            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                {
                    Color c = img.GetPixel(i, j);
                    if (c.A != 0 && c.R == 0)
                    {
                        p.X = i;
                        j = img.Height;
                        i = img.Width;
                    }
                }
            for (int i = img.Width-1; i >= 0; i--)
                for (int j = 0; j < img.Height; j++){
                    Color c = img.GetPixel(i, j);
                    if (c.A != 0 && c.R == 0)
                    {
                        p.Y = i+1;
                        j = img.Height;
                        i = -1;
                    }
                }
            return p;
        }

        double sigmoid(double activation) {
          return 1.0 / (1.0 + Math.Exp(-activation));
        }

        double CalculateError(double o,double d)
        {
            return d * (1.0 - d) * (o - d);
        }
        int getRoundedOutputValue(double x)
        {
            if (x < 0.1) return 0;
            else if (x > 0.5) return 2;
            else return 1;
        }
        void Noise(Bitmap img,int q)
        {
            Random r = new Random();
            for (int i = 0; i < q; i++)
            {
                img.SetPixel(r.Next(1, 24), r.Next(1, 24),Color.Black);
            }      
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Random r = new Random();
            for (int i = 0; i < 24; i++)
                for (int j = 0; j < 24; j++)
                    weights[i, j] = r.NextDouble() - 0.5;
            for (int i = 0; i < train_letters.Length; i++)
                for (int j = 0; j < 24; j++)
                    hidden_weights[i, j] = r.NextDouble() - 0.5;
           DrawingImage  = new Bitmap(pictureBox3.Width, pictureBox3.Height);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox2.Text == "")
                return;
            string firstText = textBox2.Text.Substring(0,1);
            Bitmap bitmap = new Bitmap(24, 24);
            Graphics graphics = Graphics.FromImage(bitmap);
            Font TahomaFont = new Font("Tahoma", 16, FontStyle.Regular);
            graphics.DrawString(firstText, TahomaFont, Brushes.Black, new PointF(0, 0));
            string a = "";
            Point v = getVL(bitmap);
            Point h = getHL(bitmap);
            Noise(bitmap, Convert.ToInt32(textBox3.Text == "" ? "0" : textBox3.Text));
            //bitmap = new Bitmap(bitmap, 24, 24);
            //Graphics g = Graphics.FromImage(bitmap);
           bitmap = bitmap.Clone(new Rectangle(h.X, v.X, h.Y - h.X, v.Y - v.X),bitmap.PixelFormat);
           bitmap = new Bitmap(bitmap, 24, 24);
            //g.DrawImage(bitmap, );
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    inputs[i, j] = -1.0;
                }
            }
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    if (bitmap.GetPixel(j, i).ToArgb() == 0 || bitmap.GetPixel(j, i).R != 0)
                        inputs[i, j] = -1.0;
                    else
                        inputs[i, j] = 1.0;
                }
            }
            DrawingImage = new Bitmap(bitmap, new Size(pictureBox3.Width, pictureBox3.Height));
            pictureBox3.Image = DrawingImage;
            pictureBox2.Image = bitmap;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            annFillCells();
            textBox1.Text = taa;
            double max=0;
            int idx = 0,cnt=0;
            for (int j = 0; j < train_letters.Length; j++)
            {
                cnt+=getRoundedOutputValue(outputs[j]);
            }
            if (cnt == 0)
            {
                label1.Text = "|!|";
            }
            else
            {
                for (int i = 0; i < train_letters.Length; i++)
                    if (max < outputs[i])
                    {
                        max = outputs[i];
                        idx = i;
                    }
                label1.Text = "|" + train_letters[idx] + "|";
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            
        }
        Point lp, sp;
        private void pictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            sp.X = e.X;
            sp.Y = e.Y;
            lp.X = e.X;
            lp.Y = e.Y;

            drawing = true;
        }
        private void pictureBox3_MouseUp(object sender, MouseEventArgs e)
        {
            drawing = false;
        }

        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
            {
                Graphics graphics = Graphics.FromImage(DrawingImage);
                graphics.DrawLine(new Pen(Color.Black, 6), sp, lp);
            
                //DrawingImage.SetPixel(e.X, e.Y, Color.Black);
                pictureBox3.Image = DrawingImage;
                sp = lp;
                lp.X = e.X;
                lp.Y = e.Y;

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Bitmap objBitmap = new Bitmap(pictureBox3.Image, new Size(pictureBox3.Width, pictureBox3.Height));
            //
            Point v = getVL(objBitmap);
            Point h = getHL(objBitmap);
            //Bitmap bitmap = new Bitmap(24, 24);
            objBitmap = objBitmap.Clone(new Rectangle(h.X, v.X, h.Y - h.X, v.Y - v.X), objBitmap.PixelFormat);
            objBitmap = new Bitmap(objBitmap, 24, 24);
            pictureBox2.Image = objBitmap;
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    inputs[i, j] = -1.0;
                }
            }
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    if (objBitmap.GetPixel(j, i).ToArgb() == 0 || objBitmap.GetPixel(j, i).R != 0)
                        inputs[i, j] = -1.0;
                    else
                        inputs[i, j] = 1.0;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    inputs[i, j] = -1.0;
                }
            }
            Graphics graphics = Graphics.FromImage(DrawingImage);
            graphics.Clear(Color.White);
            pictureBox3.Image = DrawingImage;

        }

        private void button6_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "*.jpg|*.jpg";
            openFileDialog1.ShowDialog();
            Bitmap newImg = (Bitmap)(Bitmap.FromFile(openFileDialog1.FileName));
            for (int i = 0; i < newImg.Width; i++)
            {
                for (int j = 0; j < newImg.Height; j++)
                {
                    Color c = newImg.GetPixel(i, j);
                    newImg.SetPixel(i, j, Color.FromArgb(255, c.R > 100 ? 255 : 0, c.R > 100 ? 255 : 0, c.R > 100 ? 255 : 0));
                }
            }
            Point p = getHL(newImg);
            pictureBox3.Image = newImg;
            DrawingImage = newImg;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            saveToFile();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            loadFromFile();
        }
    }
}
