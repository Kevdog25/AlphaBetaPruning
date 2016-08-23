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
        Matrix<double> Of(Matrix<double> expected, Matrix<double> actual);
        double Of(double expected, double actual);
        Matrix<double> Derivative(Matrix<double> expected, Matrix<double> actual);
        double Derivative(double expected, double actual);
    }
}
