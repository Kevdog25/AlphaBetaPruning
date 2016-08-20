using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MathNet.Numerics;

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

        Matrix[] Weights;
        Matrix[] Biases;
        Matrix[][] OutputCache;
        Matrix[][] InputCache;
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
            Weights = new Matrix[Depth];
            Biases = new Matrix[Depth];
            for (var i = 0; i < Depth; i++)
            {
                Weights[i] = new Matrix(dimensions[i + 1], dimensions[i]);
                Biases[i] = new Matrix(dimensions[i + 1]);
            }

            Initialize();
        }

        public void Learn(Matrix[] trainingData,Matrix[] trainingAnswers, Matrix[] testingData,Matrix[] testingAnswers)
        {
            int batchSize = Parameters.BatchSize;
            batchSize = Math.Min(trainingData.Length, batchSize);

            int[] testClassifications = new int[trainingAnswers.Length];
            for (var i = 0; i < trainingAnswers.Length; i++)
                testClassifications[i] = IndexOfMax(trainingAnswers[i]);

            while (!ShouldCut())
            {
                Matrix[] inputs = new Matrix[batchSize];
                Matrix[] answers = new Matrix[batchSize];
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

                    Matrix[] outputs = FeedForward(inputs);
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
        public double Test(Matrix[] testData, int[] testAnswers)
        {
            Matrix[] o = new Matrix[testData.Length];
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

        public Matrix Process(Matrix x)
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
        private Matrix[] FeedForward(Matrix[] x)
        {
            InputCache = new Matrix[x.Length][];
            OutputCache = new Matrix[x.Length][];
            Matrix[] result = new Matrix[x.Length];

            for(var t = 0; t < x.Length; t++)
            {
                InputCache[t] = new Matrix[Weights.Length];
                OutputCache[t] = new Matrix[Weights.Length];
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

        private void BackPropogate(Matrix[] expectedOutput, Matrix[] output)
        {
            // Set up a place to store the partial derivatives
            // The values will be averaged over all of the expected outputs given
            Matrix[] gradient = new Matrix[Weights.Length];
            Matrix[] biasGradient = new Matrix[Weights.Length];
            for (var i = 0; i < Weights.Length; i++)
            {
                gradient[i] = new Matrix(Weights[i].Rows, Weights[i].Cols);
                biasGradient[i] = new Matrix(Biases[i].Rows);
            }
            int batchSize = expectedOutput.Length;

            for (var i = 0; i < batchSize; i++)
            {
                // Get the derivate of the cost function WRT the inputs into the final layer
                Matrix delta = Matrix.Hadmard(MeanSquarePrime(expectedOutput[i], output[i]),SigmoidPrime(InputCache[i][Depth-1]));
                Matrix layerOutput = OutputCache[i][Depth - 1];

                // Find the derivates WRT the weights transitioning from L-1 -> L
                layerOutput.Transpose();
                Matrix partials = delta * layerOutput;
                layerOutput.Transpose();
                gradient[Depth - 1] += partials;

                // Update the bias gradient
                biasGradient[Depth - 1] += delta;

                // Propogate the derivatives back to earlier weights
                for (var l = Depth - 2; l >= 0; l--)
                {
                    Matrix transitionWeights = Weights[l + 1];
                    // Get the derivates of the cost function WRT the inputs into layer l
                    transitionWeights.Transpose();
                    delta = Matrix.Hadmard(transitionWeights * delta, SigmoidPrime(InputCache[i][l]));
                    transitionWeights.Transpose();

                    layerOutput = OutputCache[i][l];
                    layerOutput.Transpose();
                    partials = delta * layerOutput;
                    layerOutput.Transpose();

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

            // Check to see how it did
            Matrix diff = Process(OutputCache[0][0]) - output[0];
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
            double sigma = Math.Sqrt(Weights[0].Cols);
            for (var i = 0; i < Weights.Length; i++)
            {
                for (var m = 0; m < Weights[i].Rows; m++)
                {
                    for (var n = 0; n < Weights[i].Cols; n++)
                        Weights[i][m, n] = KMath.RandGauss(0, sigma);
                    Biases[i][m] = KMath.RandGauss(0, 1);
                }
            }
        }

        private int IndexOfMax(Matrix vector)
        {
            double max = 0;
            int m = 0;
            for(var i = 0; i < vector.Rows; i++)
            {
                if(vector[i] > max)
                {
                    max = vector[i];
                    m = i;
                }
            }
            return m;
        }

        #region Function Definitions
        private double MeanSquare(Matrix expected, Matrix output)
        {
            double v = 0;
            for (var i = 0; i < expected.Rows; i++)
                v += (expected[i] - output[i]) * (expected[i] - output[i]);
            return v / expected.Rows;
        }

        private Matrix MeanSquarePrime(Matrix e, Matrix o)
        {
            Matrix val = new Matrix(e.Rows);
            for (var i = 0; i < e.Rows; i++)
                val[i] = MeanSquarePrime(e[i], o[i]);
            return val;
        }

        private double MeanSquarePrime(double e, double o)
        {
            return (o - e);
        }

        private Matrix Sigmoid(Matrix x)
        {
            Matrix val = new Matrix(x.Rows);
            for (var i = 0; i < x.Rows; i++)
                val[i] = Sigmoid(x[i]);
            return val;
        }

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        private Matrix SigmoidPrime(Matrix x)
        {
            Matrix val = new Matrix(x.Rows);
            for (var i = 0; i < x.Rows; i++)
                val[i] = SigmoidPrime(x[i]);
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
