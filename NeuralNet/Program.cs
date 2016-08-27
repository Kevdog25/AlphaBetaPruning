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

        static void LoadMnist(out TrainingData[] data, string fp, int nDataPoints)
        {
            data = new TrainingData[nDataPoints];
            using (StreamReader trainingIn = new StreamReader(fp))
            {
                int n = 0;
                while (!trainingIn.EndOfStream && n < data.Length)
                {
                    string[] line = trainingIn.ReadLine().Split(',');
                    data[n] = new TrainingData(784, 10);
                    data[n].IntLabel = int.Parse(line[0]);
                    for (var i = 1; i <= 784; i++)
                        data[n].Data[i-1] = double.Parse(line[i]) / 255;
                    n++;
                }
            }
        }

        static NeuralNet TrainNet()
        {

            TrainingData[] trainingData;
            TrainingData[] testData;

            Console.WriteLine("Loading training data...");
            LoadMnist(out trainingData, "..\\..\\..\\Ignored\\mnist_train.csv", 6000);
            LoadMnist(out testData, "..\\..\\..\\Ignored\\mnist_test.csv", 1000);
            Console.WriteLine("Training and testing data loaded");

            NeuralNet.HyperParameters hp = new NeuralNet.HyperParameters(batchSize : 10, regMode: NeuralNet.HyperParameters.RegularizationMode.None, regularizationWeight : 0.001);
            int[] arch = new int[] { 784, 30, 10 };
            NeuralNet network = new NeuralNet(arch, hp);
            network.Learn(trainingData,testData);

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

            Console.WriteLine("Would you like to test it by hand (Y/N)?");
            string resp = Console.ReadLine();
            if (resp.ToUpper().Equals("Y"))
                HumanTest(network);
        }

        static void HumanTest(NeuralNet network)
        {
            TrainingData[] testData;
            LoadMnist(out testData, "..\\..\\..\\Ignored\\mnist_train.csv", 10000);
            string cont = "y";
            while (cont.Equals("y"))
            {
                int number = random.Next(testData.Length);
                Console.WriteLine("Please open up image " + number);
                Console.ReadLine();

                Console.WriteLine("The neural net guesses that this is a ");
                Console.WriteLine(network.Process(testData[number].Data).AbsoluteMaximumIndex());
                Console.WriteLine("Would you like to try an other one?");
                cont = Console.ReadLine();
            }
        }
    }
}
