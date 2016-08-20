using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace NeuralNet
{
    class Program
    {
        static Random random = new Random();

        static int IndexOfMax(Matrix<double> vector)
        {
            double max = 0;
            int m = 0;
            for (var i = 0; i < vector.RowCount; i++)
            {
                if (vector[i, 0] > max)
                {
                    max = vector[i, 0];
                    m = i;
                }
            }
            return m;
        }

        static NeuralNet TrainNet()
        {

            Matrix<double>[] trainingData = new Matrix<double>[60000];
            Matrix<double>[] trainingAnswers = new Matrix<double>[60000];
            Matrix<double>[] testData = new Matrix<double>[10000];
            Matrix<double>[] testAnswers = new Matrix<double>[10000];

            Console.WriteLine("Loading training data...");
            using (StreamReader trainingIn = new StreamReader("mnist_train.csv"))
            {
                int n = 0;
                while (!trainingIn.EndOfStream && n < trainingData.Length)
                {
                    string[] line = trainingIn.ReadLine().Split(',');
                    trainingAnswers[n] = new DenseMatrix(10,1);
                    trainingAnswers[n][int.Parse(line[0]),0] = 1;
                    trainingData[n] = new DenseMatrix(784,1);
                    for (var i = 0; i < 784; i++)
                        trainingData[n][i,0] = double.Parse(line[i]) / 255;
                    n++;
                }
            }
            using (StreamReader testIn = new StreamReader("mnist_test.csv"))
            {
                int n = 0;
                while (!testIn.EndOfStream && n < testData.Length)
                {
                    string[] line = testIn.ReadLine().Split(',');
                    testAnswers[n] = new DenseMatrix(10,1);
                    testAnswers[n][int.Parse(line[0]),0] = 1;
                    testData[n] = new DenseMatrix(784,1);
                    for (var i = 0; i < 784; i++)
                        testData[n][i,0] = double.Parse(line[i]) / 255;
                    n++;
                }
            }
            Console.WriteLine("Training and testing data loaded");


            NeuralNet.HyperParameters hp = new NeuralNet.HyperParameters(batchSize : 10);
            int[] arch = new int[] { 784, 30, 10 };
            NeuralNet network = new NeuralNet(arch, hp);
            network.Learn(trainingData, trainingAnswers, testData, testAnswers);

            return network;
        }

        static void Main(string[] args)
        {
            NeuralNet network;
            Console.WriteLine("Would you like to retrain the net?");
            if (Console.ReadLine().Equals("yes"))
            {
                network = TrainNet();
                NeuralNet.Save(network, "networkState.txt");
            }
            else
                network = NeuralNet.Load("networkState.txt");

            Matrix<double>[] testData = new Matrix<double>[10000];
            Matrix<double>[] testAnswers = new Matrix<double>[10000];
            using (StreamReader testIn = new StreamReader("mnist_test.csv"))
            {
                int n = 0;
                while (!testIn.EndOfStream && n < testData.Length)
                {
                    string[] line = testIn.ReadLine().Split(',');
                    testAnswers[n] = new DenseMatrix(10,1);
                    testAnswers[n][int.Parse(line[0]),0] = 1;
                    testData[n] = new DenseMatrix(784,1);
                    for (var i = 0; i < 784; i++)
                        testData[n][i,0] = double.Parse(line[i]) / 255;
                    n++;
                }
            }
            string cont = "y";
            while (cont.Equals("y"))
            {
                int number = random.Next(testData.Length);
                Console.WriteLine("Please open up image " + number);
                Console.ReadLine();

                Console.WriteLine("The neural net guesses that this is a ");
                Console.WriteLine(IndexOfMax(network.Process(testData[number])));
                Console.WriteLine("Would you like to try an other one?");
                cont = Console.ReadLine();
            }
        }
    }
}
