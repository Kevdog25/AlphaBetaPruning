using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace NeuralNet
{
    interface ICostFunction
    {
        double Of(Vector<double> expected, Vector<double> actual);
        double Of(double expected, double actual);
        Vector<double> Derivative(Vector<double> expected, Vector<double> actual);
        double Derivative(double expected, double actual);
    }
}
