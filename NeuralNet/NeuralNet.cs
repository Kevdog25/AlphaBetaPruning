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
        public Random random = new Random(1);
        [Serializable]
        public class HyperParameters
        {
            public enum RegularizationMode { L1, L2 };
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
                double regularizationWeight = 10,
                int batchSize = 10,
                int maxEpochCutoff = 400,
                int epochLookback = 4,
                double epochPercentCutoff = 0.01)
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
        
        Matrix<double>[] Weights;
        Matrix<double>[] Biases;
        Matrix<double>[][] OutputCache;
        Matrix<double>[][] InputCache;
        int Depth;
        public HyperParameters Parameters;
        public List<double> ClassificationPercent;
        public List<double> Cost;

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
            for (var i = 0; i < Depth; i++)
            {
                Weights[i] = new DenseMatrix(dimensions[i + 1], dimensions[i]);
                Biases[i] = new DenseMatrix(dimensions[i + 1],1);
            }

            Initialize();
        }

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
                    Console.Write(string.Format("Training on batch: {0} : {1}\r",j+1,nBatches));
                    for (var i = 0; i < batchSize; i++)
                    {
                        int index = random.Next(trainingData.Length);
                        inputs[i] = trainingData[index];
                        answers[i] = trainingAnswers[index];
                    }

                    Matrix<double>[] outputs = FeedForward(inputs);
                    BackPropogate(answers, outputs);
                    for (var k = 0; k < inputs.Length; k++)
                        cost += MeanSquare(answers[k], outputs[k]);
                }
                cost /= batchSize * nBatches;
                
                Cost.Add(cost);
                ClassificationPercent.Add(Test(trainingData, testClassifications));
                Console.WriteLine();
                Console.WriteLine("Correctness: " + ClassificationPercent[ClassificationPercent.Count - 1]);
            }

        }

        /// <summary>
        /// Returns the percentage of classifications that the net got correct.
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

        /// <summary>
        /// Takes the array of inputs and computes the resulting array of outputs.
        /// Logs the inputs and outputs at each stage in the cache.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private Matrix<double>[] FeedForward(Matrix<double>[] x)
        {
            InputCache = new Matrix<double>[x.Length][];
            OutputCache = new Matrix<double>[x.Length][];
            Matrix<double>[] result = new Matrix<double>[x.Length];

            for(var t = 0; t < x.Length; t++)
            {
                InputCache[t] = new Matrix<double>[Weights.Length];
                OutputCache[t] = new Matrix<double>[Weights.Length];
                result[t] = x[t];
                for (var i = 0; i < Depth; i++)
                {
                    OutputCache[t][i] = result[t];
                    InputCache[t][i] = Weights[i] * result[t] + Biases[i];
                    result[t] = Sigmoid(InputCache[t][i]);
                }
            }
            return result;
        }

        private void BackPropogate(Matrix<double>[] expectedOutput, Matrix<double>[] output)
        {
            // Set up a place to store the partial derivatives
            // The values will be averaged over all of the expected outputs given
            Matrix<double>[] gradient = new Matrix<double>[Weights.Length];
            Matrix<double>[] biasGradient = new Matrix<double>[Weights.Length];
            for (var i = 0; i < Weights.Length; i++)
            {
                gradient[i] = new DenseMatrix(Weights[i].RowCount, Weights[i].ColumnCount);
                biasGradient[i] = new DenseMatrix(Biases[i].RowCount,1);
            }
            int batchSize = expectedOutput.Length;

            for (var i = 0; i < batchSize; i++)
            {
                // Get the derivate of the cost function WRT the inputs into the final layer
                Matrix<double> delta = MeanSquarePrime(expectedOutput[i], output[i]).PointwiseMultiply(SigmoidPrime(InputCache[i][Depth-1]));
                Matrix<double> layerOutput = OutputCache[i][Depth - 1];

                // Find the derivates WRT the weights transitioning from L-1 -> L
                Matrix<double> partials = delta * layerOutput.Transpose();
                gradient[Depth - 1] += partials;

                // Update the bias gradient
                biasGradient[Depth - 1] += delta;

                // Propogate the derivatives back to earlier weights
                for (var l = Depth - 2; l >= 0; l--)
                {
                    Matrix<double> transitionWeights = Weights[l + 1];
                    // Get the derivates of the cost function WRT the inputs into layer l
                    delta = (transitionWeights.Transpose() * delta).PointwiseMultiply(SigmoidPrime(InputCache[i][l]));

                    layerOutput = OutputCache[i][l];
                    partials = delta * layerOutput.Transpose();

                    gradient[l] += partials;
                    biasGradient[l] += delta;
                }
            }

            // Finally update the weights using the gradient
            for (var i = 0; i < Weights.Length; i++)
            {
                Weights[i] -= (Parameters.LearningRate/batchSize) * gradient[i];
                Biases[i] -= (Parameters.LearningRate / batchSize) * biasGradient[i];
            }
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

        #region Function Definitions
        private double MeanSquare(Matrix<double> expected, Matrix<double> output)
        {
            double v = 0;
            for (var i = 0; i < expected.RowCount; i++)
                v += (expected[i,0] - output[i,0]) * (expected[i,0] - output[i,0]);
            return v / expected.RowCount;
        }

        private Matrix<double> MeanSquarePrime(Matrix<double> e, Matrix<double> o)
        {
            Matrix<double> val = new DenseMatrix(e.RowCount,1);
            for (var i = 0; i < e.RowCount; i++)
                val[i,0] = MeanSquarePrime(e[i,0], o[i,0]);
            return val;
        }

        private double MeanSquarePrime(double e, double o)
        {
            return (o - e);
        }

        private Matrix<double> Sigmoid(Matrix<double> x)
        {
            Matrix<double> val = new DenseMatrix(x.RowCount,1);
            for (var i = 0; i < x.RowCount; i++)
                val[i,0] = Sigmoid(x[i,0]);
            return val;
        }

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
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
            return Sigmoid(x) * (1 - Sigmoid(x));
        }
        #endregion

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
        public static void Save(NeuralNet net, string fileName)
        {
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(f, net);
            }
        }
    }
}
