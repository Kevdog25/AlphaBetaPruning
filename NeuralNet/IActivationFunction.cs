using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace NeuralNet
{
    interface IActivationFunction
    {
        Vector<double> Of(Vector<double> input);
        Vector<double> Derivative(Vector<double> input);
    }
}
