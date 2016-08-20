using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNet
{
    static class KMath
    {
        static Random random = new Random();
        public static double RandGauss(double mu, double sigma)
        {
            double x1 = random.NextDouble();
            double x2 = random.NextDouble();
            return (Math.Sqrt(-2 * Math.Log(x1)) * Math.Cos(2 * Math.PI * x2));
        }
    }
}
