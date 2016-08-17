using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ARTPrototype
{
    static class Program
    {
        static Form1 form;
        static Random random = new Random();
        static List<float> X;
        static List<float> Y;
        static Timer timer;
        static ART art;

        static List<float[]> GenerateGaussian(float[] mu, float s, int n)
        {
            List<float[]> points = new List<float[]>();

            for(var i = 0; i < n; i++)
            {
                float[] p = new float[2];
                double x = random.NextDouble();
                double y = random.NextDouble();
                p[0] = (float)(Math.Sqrt(-2 * Math.Log(x)) * Math.Cos(2 * Math.PI * y));
                p[1] = (float)(Math.Sqrt(-2 * Math.Log(x)) * Math.Sin(2 * Math.PI * y));
                p[0] = p[0] * s + mu[0];
                p[1] = p[1] * s + mu[1];
                points.Add(p);
            }
            return points;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            form.Text = "HELLO";
            
            form.chart1.ChartAreas[0].AxisX.Minimum = -5;
            form.chart1.ChartAreas[0].AxisX.Maximum = 5;
            form.chart1.ChartAreas[0].AxisY.Minimum = -5;
            form.chart1.ChartAreas[0].AxisY.Maximum = 5;
            form.chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            form.chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            X = new List<float>();
            Y = new List<float>();


            form.chart1.MouseClick += Restart;
            form.chart1.KeyDown += Update;
            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += Update;
            Application.Run(form);
        }

        private static void Restart(object sender, EventArgs e)
        {
            art = new ART(2, 0.6f, 0.3f);
            Update(sender, e);
        }

        private static void Update(object sender, EventArgs e)
        {
            if (art == null)
                Restart(sender, e);
            else
            {
                List<float[]> points = GenerateGaussian(new float[2] { 0, 0 }, 1, 1);
                foreach (float[] p in points)
                    art.Process(new VectorN(p));
                X.Clear();
                Y.Clear();
                foreach (VectorN v in art.GetPositions())
                {
                    X.Add(v[0]);
                    Y.Add(v[1]);
                }
                form.chart1.Series["Series1"].Points.DataBindXY(X, Y);
                form.Refresh();
            }
        }
    }
}
