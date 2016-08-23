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
        #region Private Fields
        private delegate Matrix<double> RegularizationScheme(Matrix<double> w);
        Matrix<double>[] Weights;
        Matrix<double>[] Biases;
        Matrix<double>[] weightGradients;
        Matrix<double>[] biasGradients;
        Matrix<double>[] OutputCache;
        Matrix<double>[] InputCache;
        RegularizationScheme Regularization;
        ICostFunction CostFunction;
        #endregion
        #region Classes
        [Serializable]
        public class HyperParameters
        {
            public enum RegularizationMode { L1, L2 , None};
            public enum CostFunction { MeanSquare, CrossEntropy };

            public RegularizationMode Regularization;
            public CostFunction Cost;
            public double LearningRate;
            public double RegularizationWeight;
            public int MaxEpochCutoff;
            public double EpochPercentCutoff;
            public int EpochLookback;
            public int BatchSize;

            public HyperParameters(RegularizationMode regMode = RegularizationMode.L2,
                CostFunction costFunc = CostFunction.MeanSquare,
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
            }
        }
        [Serializable]
        public class MeanSquare : ICostFunction
        {
            public double Derivative(double expected, double actual)
            {
                return actual - expected;
            }

            public Matrix<double> Derivative(Matrix<double> expected, Matrix<double> actual)
            {
                return actual - expected;
            }

            public double Of(double expected, double actual)
            {
                return (expected - actual) * (expected - actual);
            }

            public Matrix<double> Of(Matrix<double> expected, Matrix<double> actual)
            {
                Matrix<double> diff = expected - actual;
                return (diff.Transpose() * diff);
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
            Biases = new Matrix<double>[Depth];
            weightGradients = new Matrix<double>[Depth];
            biasGradients = new Matrix<double>[Depth];
            OutputCache = new Matrix<double>[Depth];
            InputCache = new Matrix<double>[Depth];
            for (var i = 0; i < Depth; i++)
            {
                Weights[i] = new DenseMatrix(dimensions[i + 1], dimensions[i]);
                Biases[i] = new DenseMatrix(dimensions[i + 1],1);
                weightGradients[i] = new DenseMatrix(dimensions[i+1], dimensions[i]);
                biasGradients[i] = new DenseMatrix(dimensions[i + 1], 1);
                OutputCache[i] = new DenseMatrix(dimensions[i]);
                InputCache[i] = new DenseMatrix(dimensions[i + 1]);
            }

            SetDelegates();
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
        public void Learn(Matrix<double>[] trainingData,Matrix<double>[] trainingAnswers, Matrix<double>[] testingData,Matrix<double>[] testingAnswers)
        {
            int batchSize = Parameters.BatchSize;
            batchSize = Math.Min(trainingData.Length, batchSize);

            int[] testClassifications = new int[trainingAnswers.Length];
            for (var i = 0; i < trainingAnswers.Length; i++)
                testClassifications[i] = IndexOfMax(trainingAnswers[i]);

            while (!ShouldCut())
            {
                Matrix<double>[] inputs = new Matrix<double>[batchSize];
                Matrix<double>[] answers = new Matrix<double>[batchSize];
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
                        inputs[i] = trainingData[index];
                        answers[i] = trainingAnswers[index];

                        Matrix<double> output = FeedForward(inputs[i]);
                        BackPropogate(answers[i], output);

                        cost += CostFunction.Of(answers[i], output)[0, 0];
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
                ClassificationPercent.Add(Test(trainingData, testClassifications));
                Console.WriteLine();
                Console.WriteLine("Correctness: " + ClassificationPercent[ClassificationPercent.Count - 1]);
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
        public double Test(Matrix<double>[] testData, int[] testAnswers)
        {
            Matrix<double>[] o = new Matrix<double>[testData.Length];
            for (var i = 0; i < testData.Length; i++)
                o[i] = Process(testData[i]);

            int correct = 0;
            for(var i = 0; i < o.Length; i++)
            {
                if (testAnswers[i] == IndexOfMax(o[i]))
                    correct++;
            }
            
            return (double)correct / testAnswers.Length;
        }
        public Matrix<double> Process(Matrix<double> x)
        {
            for (var l = 0; l < Depth; l++)
                x = Sigmoid(Weights[l] * x);
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
        private Matrix<double> FeedForward(Matrix<double> x)
        {
            for (var i = 0; i < Depth; i++)
            {
                OutputCache[i] = x;
                InputCache[i] = Weights[i] * x + Biases[i];
                x = Sigmoid(InputCache[i]);
            }
            return x;
        }
        private void BackPropogate(Matrix<double> expectedOutput, Matrix<double> output)
        {
            // Get the derivate of the cost function WRT the inputs into the final layer
            Matrix<double> delta = CostFunction.Derivative(expectedOutput, output).PointwiseMultiply(SigmoidPrime(InputCache[Depth-1]));

            // Find the derivates WRT the weights transitioning from L-1 -> L
            Matrix<double> partials = delta * OutputCache[Depth-1].Transpose();
            weightGradients[Depth - 1] += partials;

            // Update the bias gradient
            biasGradients[Depth - 1] += delta;

            // Propogate the derivatives back to earlier weights
            for (var l = Depth - 2; l >= 0; l--)
            {
                Matrix<double> transitionWeights = Weights[l + 1];
                // Get the derivates of the cost function WRT the inputs into layer l
                delta = (transitionWeights.Transpose() * delta).PointwiseMultiply(SigmoidPrime(InputCache[l]));
                
                partials = delta * OutputCache[l].Transpose();

                weightGradients[l] += partials;
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
                    Biases[i][m,0] = KMath.RandGauss(0, 1);
                }
            }
        }
        private void SetDelegates()
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

        }
        private int IndexOfMax(Matrix<double> vector)
        {
            double max = 0;
            int m = 0;
            for(var i = 0; i < vector.RowCount; i++)
            {
                if(vector[i,0] > max)
                {
                    max = vector[i,0];
                    m = i;
                }
            }
            return m;
        }
        #endregion
        #region Function Definitions
        private Matrix<double> Sigmoid(Matrix<double> x)
        {
            Matrix<double> val = new DenseMatrix(x.RowCount,1);
            for (var i = 0; i < x.RowCount; i++)
                val[i,0] = Sigmoid(x[i,0]);
            return val;
        }

        private double Sigmoid(double x)
        {
            double v = 1.0 / (1.0 + Math.Exp(-x));
            return v;
        }

        private Matrix<double> SigmoidPrime(Matrix<double> x)
        {
            Matrix<double> val = new DenseMatrix(x.RowCount,1);
            for (var i = 0; i < x.RowCount; i++)
                val[i,0] = SigmoidPrime(x[i,0]);
            return val;
        }

        private double SigmoidPrime(double x)
        {
            double s = 1.0 / (1.0 + Math.Exp(-x));
            return s * (1 - s);
        }
        #endregion
    }
}
