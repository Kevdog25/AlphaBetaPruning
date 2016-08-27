using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace NeuralNet
{
    [Serializable]
    class NeuralNet
    {
        #region Public Fields
        public int Depth { get; private set; }
        public HyperParameters Parameters;
        public List<double> ClassificationPercent;
        public List<double> Cost;
        public static Random random = new Random(1);
        #endregion
        #region Fields
        private delegate Matrix<double> RegularizationScheme(Matrix<double> w);
        private Matrix<double>[] Weights;
        private Vector<double>[] Biases;
        private Matrix<double>[] weightGradients;
        private Vector<double>[] biasGradients;
        private Vector<double>[] OutputCache;
        private Vector<double>[] InputCache;
        private RegularizationScheme Regularization;
        private ICostFunction CostFunction;
        private IActivationFunction ActivationFunction;
        #endregion
        #region Classes
        [Serializable]
        public class HyperParameters
        {
            public enum RegularizationMode { L1, L2 , None};
            public enum CostFunction { MeanSquare, CrossEntropy };
            public enum ActivationFunction { Sigmoid }

            public RegularizationMode Regularization;
            public CostFunction Cost;
            public ActivationFunction ActFunction;
            public double LearningRate;
            public double RegularizationWeight;
            public int MaxEpochCutoff;
            public double EpochPercentCutoff;
            public int EpochLookback;
            public int BatchSize;

            public HyperParameters(RegularizationMode regMode = RegularizationMode.L2,
                CostFunction costFunc = CostFunction.MeanSquare,
                ActivationFunction actFunc = ActivationFunction.Sigmoid,
                double learningRate = 3,
                double regularizationWeight = .01,
                int batchSize = 10,
                int maxEpochCutoff = 30,
                int epochLookback = 4,
                double epochPercentCutoff = 0.005)
            {
                Regularization = regMode;
                Cost = costFunc;
                LearningRate = learningRate;
                RegularizationWeight = regularizationWeight;
                MaxEpochCutoff = maxEpochCutoff;
                EpochPercentCutoff = epochPercentCutoff;
                EpochLookback = epochLookback;
                BatchSize = batchSize;
                ActFunction = actFunc;
            }
        }
        [Serializable]
        public class MeanSquare : ICostFunction
        {
            public double Derivative(double expected, double actual)
            {
                return actual - expected;
            }

            public Vector<double> Derivative(Vector<double> expected, Vector<double> actual)
            {
                return actual - expected;
            }

            public double Of(double expected, double actual)
            {
                return (expected - actual) * (expected - actual);
            }

            public double Of(Vector<double> expected, Vector<double> actual)
            {
                Vector<double> diff = expected - actual;
                return (diff * diff);
            }
        }
        [Serializable]
        public class Sigmoid : IActivationFunction
        {
            public Vector<double> Derivative(Vector<double> input)
            {
                Vector<double> ret = new DenseVector(input.Count);
                for (var i = 0; i < input.Count; i++)
                    ret[i] = Derivative(input[i]);
                return ret;
            }

            public Vector<double> Of(Vector<double> input)
            {
                Vector<double> ret = new DenseVector(input.Count);
                for (var i = 0; i < input.Count; i++)
                    ret[i] = Of(input[i]);
                return ret;
            }

            private double Of(double x)
            {
                return 1.0 / (1.0 + Math.Exp(-x));
            }
            private double Derivative(double x)
            {
                double s = Of(x);
                return s * (1 - s);
            }
        }
        #endregion
        #region Constructors
        public NeuralNet(int[] dimensions,HyperParameters hyperParameters = null)
        {
            Parameters = hyperParameters;
            if (Parameters == null)
                Parameters = new HyperParameters();

            ClassificationPercent = new List<double>();
            Cost = new List<double>();
            Depth = dimensions.Length-1;
            Weights = new Matrix<double>[Depth];
            Biases = new Vector<double>[Depth];
            weightGradients = new Matrix<double>[Depth];
            biasGradients = new Vector<double>[Depth];
            OutputCache = new Vector<double>[Depth];
            InputCache = new Vector<double>[Depth];
            for (var i = 0; i < Depth; i++)
            {
                Weights[i] = new DenseMatrix(dimensions[i + 1], dimensions[i]);
                Biases[i] = new DenseVector(dimensions[i + 1]);
                weightGradients[i] = new DenseMatrix(dimensions[i+1], dimensions[i]);
                biasGradients[i] = new DenseVector(dimensions[i + 1]);
                OutputCache[i] = new DenseVector(dimensions[i]);
                InputCache[i] = new DenseVector(dimensions[i + 1]);
            }

            SetParameters();
            Initialize();
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Updates the weights of the network based on the 
        /// </summary>
        /// <param name="trainingData"></param>
        /// <param name="trainingAnswers"></param>
        /// <param name="testingData"></param>
        /// <param name="testingAnswers"></param>
        public void Learn(TrainingData[] trainingData, TrainingData[] testingData = null)
        {
            int batchSize = Parameters.BatchSize;
            batchSize = Math.Min(trainingData.Length, batchSize);

            while (!ShouldCut())
            {
                double cost = 0;
                int nBatches = trainingData.Length / batchSize;
                Console.WriteLine();
                for (var j = 0; j < nBatches; j++)
                {
                    Console.Write(string.Format("Training on batch: {0} : {1}\r", j + 1, nBatches));
                    // Set up a place to store the partial derivatives
                    // The values will be averaged over all of the expected outputs given
                    for (var i = 0; i < Depth; i++)
                    {
                        weightGradients[i].Clear();
                        biasGradients[i].Clear();
                    }

                    for (var i = 0; i < batchSize; i++)
                    {
                        int index = random.Next(trainingData.Length);

                        Vector<double> output = FeedForward(trainingData[index].Data);
                        BackPropogate(trainingData[index].GetLabelVector(), output);

                        cost += CostFunction.Of(trainingData[index].GetLabelVector(), output);
                    }


                    // Finally update the weights using the gradient
                    for (var i = 0; i < Weights.Length; i++)
                    {
                        Weights[i] -= (Parameters.LearningRate / batchSize) * weightGradients[i] +
                             (Parameters.LearningRate * Parameters.RegularizationWeight / batchSize) * Regularization(Weights[i]);
                        Biases[i] -= (Parameters.LearningRate / batchSize) * biasGradients[i];
                    }
                }
                cost /= batchSize * nBatches;
                
                Cost.Add(cost);
                if (testingData != null)
                {
                    ClassificationPercent.Add(Test(testingData));
                    Console.WriteLine();
                    Console.WriteLine("Correctness: " + ClassificationPercent[ClassificationPercent.Count - 1]);
                }
            }
        }
        /// <summary>
        /// Loads a neural network state from a specified file location.
        /// Uses binary serialization.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static NeuralNet Load(string fileName)
        {
            NeuralNet net = null;
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                if (f.Length != 0)
                    net = bf.Deserialize(f) as NeuralNet;
            }
            return net;
        }
        /// <summary>
        /// Saves the state of the network to the specified file location.
        /// </summary>
        /// <param name="net"></param>
        /// <param name="fileName"></param>
        public static void Save(NeuralNet net, string fileName)
        {
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(f, net);
            }
        }
        /// <summary>
        /// Tests the classification ability of the net on the given test data.
        /// Only tests classification based on the max activation of the output layer.
        /// </summary>
        /// <param name="testData"></param>
        /// <param name="testAnswers"></param>
        /// <returns></returns>
        public double Test(TrainingData[] testData)
        {
            int correct = 0;
            for(var i = 0; i < testData.Length; i++)
            {
                if (testData[i].GetLabelVector().AbsoluteMaximumIndex() == Process(testData[i].Data).AbsoluteMaximumIndex())
                    correct++;
            }
            
            return (double)correct / testData.Length;
        }
        public Vector<double> Process(Vector<double> x)
        {
            for (var l = 0; l < Depth; l++)
                x = ActivationFunction.Of(Weights[l] * x);
            return x;
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Takes the array of inputs and computes the resulting array of outputs.
        /// Logs the inputs and outputs at each stage in the cache.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private Vector<double> FeedForward(Vector<double> x)
        {
            for (var i = 0; i < Depth; i++)
            {
                OutputCache[i] = x;
                InputCache[i] = Weights[i] * x + Biases[i];
                x = ActivationFunction.Of(InputCache[i]);
            }
            return x;
        }
        private void BackPropogate(Vector<double> expectedOutput, Vector<double> output)
        {
            // Get the derivate of the cost function WRT the inputs into the final layer
            Vector<double> delta = CostFunction.Derivative(expectedOutput, output).PointwiseMultiply(ActivationFunction.Derivative(InputCache[Depth-1]));

            // Find the derivates WRT the weights transitioning from L-1 -> L
            weightGradients[Depth - 1] += delta.OuterProduct(OutputCache[Depth-1]);

            // Update the bias gradient
            biasGradients[Depth - 1] += delta;

            // Propogate the derivatives back to earlier weights
            for (var l = Depth - 2; l >= 0; l--)
            {
                Matrix<double> transitionWeights = Weights[l + 1];
                // Get the derivates of the cost function WRT the inputs into layer l
                delta = (transitionWeights.Transpose() * delta).PointwiseMultiply(ActivationFunction.Derivative(InputCache[l]));

                weightGradients[l] += delta.OuterProduct(OutputCache[l]);
                biasGradients[l] += delta;
            }
        }
        private Matrix<double> L2Regularization(Matrix<double> weights)
        {
            return weights;
        }
        private Matrix<double> L1Regularization(Matrix<double> weights)
        {
            Matrix<double> wPrime = new DenseMatrix(weights.RowCount, weights.ColumnCount);
            for (var m = 0; m < wPrime.RowCount; m++)
                for (var n = 0; n < wPrime.ColumnCount; n++)
                    wPrime[m, n] = Math.Sign(weights[m, n]);
            return wPrime;
        }
        private Matrix<double> EmptyRegularization(Matrix<double> weights)
        {
            return new DenseMatrix(weights.RowCount, weights.ColumnCount);
        }
        private bool ShouldCut()
        {
            if (Parameters.EpochLookback > ClassificationPercent.Count)
                return false;
            if (ClassificationPercent.Count >= Parameters.MaxEpochCutoff)
                return true;
            double mean = 0;
            double deviation = 0;
            for (var i = ClassificationPercent.Count - 1; i >= ClassificationPercent.Count - Parameters.EpochLookback; i--)
                mean += ClassificationPercent[i];
            mean /= Parameters.EpochLookback;
            for (var i = ClassificationPercent.Count - 1; i >= ClassificationPercent.Count - Parameters.EpochLookback; i--)
                deviation += Math.Abs(ClassificationPercent[i]-mean);
            deviation /= Parameters.EpochLookback;

            return (deviation / mean) <= Parameters.EpochPercentCutoff;
        }
        private void Initialize()
        {
            double sigma = Math.Sqrt(Weights[0].ColumnCount);
            for (var i = 0; i < Weights.Length; i++)
            {
                for (var m = 0; m < Weights[i].RowCount; m++)
                {
                    for (var n = 0; n < Weights[i].ColumnCount; n++)
                        Weights[i][m, n] = KMath.RandGauss(0, sigma);
                    Biases[i][m] = KMath.RandGauss(0, 1);
                }
            }
        }
        private void SetParameters()
        {
            switch (Parameters.Regularization)
            {
                case HyperParameters.RegularizationMode.L1:
                    Regularization = L1Regularization;
                    break;
                case HyperParameters.RegularizationMode.L2:
                    Regularization = L2Regularization;
                    break;
                case HyperParameters.RegularizationMode.None:
                    Regularization = EmptyRegularization;
                    break;
                default:
                    throw new NeuralNetException("Unsupported Regularization Choice: " + Parameters.Regularization);
            }

            switch (Parameters.Cost)
            {
                case HyperParameters.CostFunction.MeanSquare:
                    CostFunction = new MeanSquare();
                    break;
                default:
                    throw new NeuralNetException("Unsupported Cost Function Choice: " + Parameters.Cost);
            }

            switch (Parameters.ActFunction)
            {
                case HyperParameters.ActivationFunction.Sigmoid:
                    ActivationFunction = new Sigmoid();
                    break;
                default:
                    throw new NeuralNetException("Unsupported Activation Function Choice: " + Parameters.ActFunction);
            }

        }
        #endregion
    }
}
