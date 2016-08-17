using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARTPrototype
{
    class ART
    {
        private int N;
        private float Vigilance;
        private float ExclusionSim = 1;
        private List<Neuron> Neurons;
        private int MaxWeight;

        private class Neuron
        {
            public VectorN Weights;
            public int NumberOfStates;
            public float Vigilance;
            public static int MaxWeight;

            public Neuron(float vig,int n)
            {
                Weights = new VectorN(n);
                Vigilance = vig;
                NumberOfStates = 0;
            }

            public Neuron(float vig, VectorN source)
            {
                Weights = source * 1; // This makes a local copy of source just in case.
                Vigilance = vig;
                NumberOfStates = 0;
            }

            public void Average(VectorN point)
            {
                Weights = (NumberOfStates * Weights + point) / (NumberOfStates + 1);
                NumberOfStates = Math.Min(MaxWeight,NumberOfStates+1);
            }
            
            public void Average(Neuron other)
            {
                Weights = (NumberOfStates * Weights + other.NumberOfStates * other.Weights) / (NumberOfStates + other.NumberOfStates);
                NumberOfStates = Math.Min(MaxWeight, NumberOfStates + other.NumberOfStates);
            }

            /// <summary>
            /// Compares the point to the weight vector to see how similar they are.
            /// The vlaue returned in between 0 and 1 with 1 being the most similar.
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            public float Similarity(VectorN point)
            {
                // Idk about this metric
                return 1 / (1 + (point - Weights).Length());
            }

            public bool IsInRange(VectorN point, out float similarity, float threshold = -1)
            {
                if (threshold < 0)
                    threshold = Vigilance;
                similarity = Similarity(point);
                return similarity > threshold;
            }
        }

        public ART(int n,float vig,float exclusion = 1,int maxW = 50)
        {
            N = n;
            Neurons = new List<Neuron>();
            Vigilance = vig;
            Neuron.MaxWeight = maxW;
            MaxWeight = maxW;
            ExclusionSim = exclusion;
        }

        public void Process(VectorN point)
        {
            float similarity;
            int bestInd = FindBestMatch(point, out similarity);

            if (bestInd >= 0)
            {
                Neuron bestNeuron = Neurons[bestInd];
                Neurons.RemoveAt(bestInd);
                bestNeuron.Average(point);
                int nearestNeighbor = FindBestMatch(bestNeuron.Weights,out similarity, ExclusionSim);
                if(nearestNeighbor >= 0)
                {
                    bestNeuron.Average(Neurons[nearestNeighbor]);
                    Neurons.RemoveAt(nearestNeighbor);
                }
                Neurons.Add(bestNeuron);
            }
            else
                Neurons.Add(new Neuron(Vigilance, point));
        }

        public int FindBestMatch(VectorN point,out float similarity, float threshold = -1)
        {
            int bestNeuron = -1;
            similarity = 0;
            for(var i = 0; i < Neurons.Count; i++)
            {
                float sim;
                if (Neurons[i].IsInRange(point, out sim, threshold))
                {
                    if (sim > similarity)
                    {
                        bestNeuron = i;
                        similarity = sim;
                    }
                }
            }
            return bestNeuron;
        }

        public List<VectorN> GetPositions()
        {
            List<VectorN> positions = new List<VectorN>();
            for (var i = 0; i < Neurons.Count; i++)
                positions.Add(Neurons[i].Weights);
            return positions;
        }
    }
}
