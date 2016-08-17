using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using AlphaBetaPruning.GameDefinitions;
using AlphaBetaPruning.Shared;
using System.IO;

/// <summary>
/// IN CONSTRUCTION
/// -K
/// </summary>
namespace AlphaBetaPruning.AIHeurisitc
{
    [Serializable]
    class ART<T> : IHeuristicBuilder where T : IARTDefinition, new()
    {
        private float Vigilance;
        private float ExclusionSim = 1;
        private Dictionary<int,List<Neuron>> Neurons;
        private int MaxWeight;
        private T Definition;

        [Serializable]
        public class Neuron
        {
            public VectorN Weights;
            public int NumberOfStates;
            public float Vigilance;
            public static int MaxWeight;
            public float HValue;

            public Neuron(float vig,int n)
            {
                Weights = new VectorN(n);
                Vigilance = vig;
                NumberOfStates = 1;
            }

            public Neuron(float vig, VectorN source,float hValue)
            {
                Weights = source * 1; // This makes a local copy of source just in case.
                Vigilance = vig;
                NumberOfStates = 1;
                HValue = hValue;
            }

            public void Encourage(VectorN point, float hValue)
            {
                Weights = (NumberOfStates * Weights + point) / (NumberOfStates + 1);
                HValue = (NumberOfStates * HValue + hValue) / (NumberOfStates + 1);
                NumberOfStates = Math.Min(MaxWeight,NumberOfStates+1);
            }

            public void Discourage(VectorN point, float hValue)
            {
                //VectorN dr = (point - Weights) / (NumberOfStates);
                //Weights -= dr;
                HValue = (NumberOfStates * HValue - hValue) / (NumberOfStates);
                NumberOfStates = Math.Max(0, NumberOfStates);
            }
            
            public void Average(Neuron other)
            {
                Weights = (NumberOfStates * Weights + other.NumberOfStates * other.Weights) / (NumberOfStates + other.NumberOfStates);
                HValue = (NumberOfStates * HValue + other.NumberOfStates * other.HValue) / (NumberOfStates + other.NumberOfStates);
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

        public ART(float vig,float exclusion = 1,int maxW = 50)
        {
            Definition = new T();
            InitializeNeurons();
            Vigilance = vig;
            Neuron.MaxWeight = maxW;
            MaxWeight = maxW;
            ExclusionSim = exclusion;
        }

        private void InitializeNeurons()
        {
            Neurons = new Dictionary<int, List<Neuron>>();
            List<int> players = Definition.GetPlayers();
            foreach (int v in players)
                Neurons.Add(v, new List<Neuron>());
        }

        public Dictionary<int, List<VectorN>> GetPositions()
        {
            Dictionary<int, List<VectorN>> knowledgeDict = new Dictionary<int, List<VectorN>>();
            foreach(KeyValuePair<int,List<Neuron>> pair in Neurons)
            {
                List<VectorN> positions = new List<VectorN>();
                for (var i = 0; i < pair.Value.Count; i++)
                    positions.Add(pair.Value[i].Weights);
                knowledgeDict.Add(pair.Key, positions);
            }
            return knowledgeDict;
        }

        public void Load(string fileName)
        {
            Neurons = null;
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                if(f.Length != 0)
                    Neurons = bf.Deserialize(f) as Dictionary<int,List<Neuron>>;
            }
            if(Neurons == null)
            {
                InitializeNeurons();
            }
        }

        public void Save(string fileName)
        {
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(f,Neurons);
            }
        }

        #region IHeuristicBuilder
        public float Judge(IGameState state)
        {
            int player;
            VectorN point = Definition.Convert(state, out player);
            float h = 0;
            foreach (KeyValuePair<int, List<Neuron>> playerKnowledge in Neurons)
            {
                float playerEval = 0;
                foreach (Neuron n in playerKnowledge.Value)
                {
                    float sim;
                    if (n.IsInRange(point, out sim))
                        playerEval += sim * n.HValue;
                }
                playerEval *= player == playerKnowledge.Key ? 1 : -1;
                h += playerEval;
            }
            return h;
        }

        public void Learn(IGameState initialState, List<Shared.Action> moves, int winner)
        {
            List<IGameState> transcript = new List<IGameState>();
            transcript.Add(initialState);
            for(var i = 0; i < moves.Count; i++)
                transcript.Add(moves[i].Act(transcript[i]));

            for(var i = transcript.Count-1; i >= 0; i--)
            {
                int p;
                VectorN point = Definition.Convert(transcript[i], out p);
                float hValue = 1 / (float)(transcript.Count - i);
                foreach(int player in Neurons.Keys)
                    Process(point, hValue, player, player != winner);
            }
        }
        #endregion

        #region Private
        private void Process(VectorN point, float hValue, int player, bool inhibit = false)
        {
            List<Neuron> playerKnowledge = Neurons[player];
            float similarity;
            int bestInd = FindBestMatch(playerKnowledge, point, out similarity);

            // If we are inhibiting. Then we treat it a bit differently.
            if (inhibit && bestInd >= 0)
            {
                playerKnowledge[bestInd].Discourage(point,hValue);
                if (playerKnowledge[bestInd].NumberOfStates == 0)
                    playerKnowledge.RemoveAt(bestInd);
                return;
            }

            if (bestInd >= 0)
            {
                Neuron bestNeuron = playerKnowledge[bestInd];
                bestNeuron.Encourage(point, hValue);


                // Collapse the structure to reduce redundant neurons.
                playerKnowledge.RemoveAt(bestInd);
                int nearestNeighbor = FindBestMatch(playerKnowledge, bestNeuron.Weights, out similarity, ExclusionSim);
                if (nearestNeighbor >= 0)
                {
                    bestNeuron.Average(playerKnowledge[nearestNeighbor]);
                    playerKnowledge.RemoveAt(nearestNeighbor);
                }
                playerKnowledge.Add(bestNeuron);
            }
            else if (!inhibit)
                playerKnowledge.Add(new Neuron(Vigilance, point, hValue));
        }

        private int FindBestMatch(List<Neuron> neurons, VectorN point, out float similarity, float threshold = -1)
        {
            int bestNeuron = -1;
            similarity = 0;
            for (var i = 0; i < neurons.Count; i++)
            {
                float sim;
                if (neurons[i].IsInRange(point, out sim, threshold))
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
        #endregion
    }
}
