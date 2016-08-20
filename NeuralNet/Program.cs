using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace NeuralNet
{
    class Program
    {
        static Random random = new Random();

        static int IndexOfMax(Matrix vector)
        {
            double max = 0;
            int m = 0;
            for (var i = 0; i < vector.Rows; i++)
            {
                if (vector[i] > max)
                {
                    max = vector[i];
                    m = i;
                }
            }
            return m;
        }

        static NeuralNet TrainNet()
        {

            Matrix[] trainingData = new Matrix[6000];
            Matrix[] trainingAnswers = new Matrix[6000];
            Matrix[] testData = new Matrix[1000];
            Matrix[] testAnswers = new Matrix[1000];

            Console.WriteLine("Loading training data...");
            using (StreamReader trainingIn = new StreamReader("mnist_train.csv"))
            {
                int n = 0;
                while (!trainingIn.EndOfStream && n < trainingData.Length)
                {
                    string[] line = trainingIn.ReadLine().Split(',');
                    trainingAnswers[n] = new Matrix(10);
                    trainingAnswers[n][int.Parse(line[0])] = 1;
                    trainingData[n] = new Matrix(784);
                    for (var i = 0; i < 784; i++)
                        trainingData[n][i] = double.Parse(line[i]) / 255;
                    n++;
                }
            }
            using (StreamReader testIn = new StreamReader("mnist_test.csv"))
            {
                int n = 0;
                while (!testIn.EndOfStream && n < testData.Length)
                {
                    string[] line = testIn.ReadLine().Split(',');
                    testAnswers[n] = new Matrix(10);
                    testAnswers[n][int.Parse(line[0])] = 1;
                    testData[n] = new Matrix(784);
                    for (var i = 0; i < 784; i++)
                        testData[n][i] = double.Parse(line[i]) / 255;
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

        static void TestMatrices()
        {
            Matrix m1 = new Matrix(4);
            m1.SetData(1,0,0,1);
            Matrix m2 = new Matrix(4);
            m2.SetData(1, 1, 1, 1);
            m2.Transpose();
            Console.WriteLine(m1*m2);
        }

        static void TestNetSimple()
        {
            Matrix[] trainingData = new Matrix[60000];
            Matrix[] trainingAnswers = new Matrix[60000];
            Matrix[] testData = new Matrix[1000];
            Matrix[] testAnswers = new Matrix[1000];

            for(var i = 0; i < trainingData.Length; i++)
            {
                Matrix data = new Matrix(4);
                for (var m = 0; m < data.Rows; m++)
                    data[m] = random.NextDouble();
                Matrix answer = new Matrix(4);
                answer[IndexOfMax(data)] = 1;
                trainingData[i] = data;
                trainingAnswers[i] = answer;
            }

            for (var i = 0; i < testData.Length; i++)
            {
                Matrix data = new Matrix(4);
                for (var m = 0; m < data.Rows; m++)
                    data[m] = random.NextDouble();
                Matrix answer = new Matrix(4);
                answer[IndexOfMax(data)] = 1;
                testData[i] = data;
                testAnswers[i] = answer;
            }

            Console.WriteLine("Training and testing data loaded");
            int[] arch = new int[] { 4, 4 };
            NeuralNet network = new NeuralNet(arch);
            network.Learn(trainingData, trainingAnswers, testData, testAnswers);
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

            Matrix[] testData = new Matrix[10000];
            Matrix[] testAnswers = new Matrix[10000];
            using (StreamReader testIn = new StreamReader("mnist_test.csv"))
            {
                int n = 0;
                while (!testIn.EndOfStream && n < testData.Length)
                {
                    string[] line = testIn.ReadLine().Split(',');
                    testAnswers[n] = new Matrix(10);
                    testAnswers[n][int.Parse(line[0])] = 1;
                    testData[n] = new Matrix(784);
                    for (var i = 0; i < 784; i++)
                        testData[n][i] = double.Parse(line[i]) / 255;
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
