using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.Shared;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AlphaBetaPruning.GameDefinitions;


/// <summary>
/// IN CONSTRUCTION
/// -K
/// </summary>
namespace AlphaBetaPruning.AIHeurisitc
{
    [Serializable]
    class ARTTree<T> : IHeuristicBuilder where T : IARTDefinition, new()
    {
        private Dictionary<int, List<Node>> Nodes;
        private T Definition;
        private float Vigilance;
        private float ExclusionSim;
        private List<float> ChildVigilance;
        private List<float> ChildExclusion;

        #region Classes
        [Serializable]
        private class Node : ART<T>.Neuron
        {
            public ARTTree<T> Children;
            public Node(float vig, VectorN source, float hValue, ARTTree<T> children = null) : base(vig, source, hValue)
            {
                Children = children;
            }
        }
        #endregion

        #region Constructors
        public ARTTree(List<float> vig, List<float> exclusion = null, int maxW = 50)
        {
            Definition = new T();
            InitializeNeurons();
            Node.MaxWeight = maxW;

            Vigilance = vig[0];
            if (vig.Count > 1)
                ChildVigilance = vig.GetRange(1, vig.Count - 1);
            else
                ChildVigilance = null;

            if (exclusion == null)
            {
                ExclusionSim = 1;
                ChildExclusion = null;
            }
            else
            {
                ExclusionSim = exclusion[0];
                if (exclusion.Count > 1)
                    ChildExclusion = exclusion.GetRange(1, exclusion.Count - 1);
                else
                    ChildExclusion = null;
            }
        }
        #endregion

        #region IHeuristicBuilder
        public float Judge(IGameState state)
        {
            int player;
            VectorN point = Definition.Convert(state, out player);
            float h = 0;
            foreach (KeyValuePair<int, List<Node>> playerKnowledge in Nodes)
            {
                float playerEval = 0;
                foreach (Node n in playerKnowledge.Value)
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
            for (var i = 0; i < moves.Count; i++)
                transcript.Add(moves[i].Act(transcript[i]));

            for (var i = transcript.Count - 1; i >= 0; i--)
            {
                int p;
                VectorN point = Definition.Convert(transcript[i], out p);
                float hValue = 1 / (float)(transcript.Count - i);
                foreach (int player in Nodes.Keys)
                    Process(point, hValue, player, player != winner);
            }
        }
        #endregion

        #region Private
        /// <summary>
        /// Processes the new information by inhbiting or encouraging neurons.
        /// This returns the position of the affected neuron.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="hValue"></param>
        /// <param name="player"></param>
        /// <param name="inhibit"></param>
        /// <returns></returns>
        private VectorN Process(VectorN point, float hValue, int player, bool inhibit = false)
        {
            List<Node> playerKnowledge = Nodes[player];
            float similarity;
            int bestInd = FindBestMatch(playerKnowledge, point, out similarity);
            Node match = null;
            if(bestInd >= 0)
                match = playerKnowledge[bestInd];

            // The idea here is to propogate the point down to the lowest level.
            // Then, only positions of neurons are used to influence the higher levels.

            if (inhibit && bestInd >= 0)
            {
                // There is a match and were encouraging it.

                if (match.Children != null)
                    point = match.Children.Process(point, hValue, player, inhibit);

                match.Discourage(point,hValue);
                if (match.NumberOfStates == 0)
                    playerKnowledge.RemoveAt(bestInd);
                point = match.Weights;
            }
            else if (bestInd >= 0)
            {
                // There is a match, and were encouraging it.

                if (match.Children != null)
                    point = match.Children.Process(point, hValue, player, inhibit);
                match.Encourage(point, hValue);

                // Collapse the structure to reduce redundant neurons.
                //playerKnowledge.RemoveAt(bestInd);
                //int nearestNeighbor = FindBestMatch(playerKnowledge, match.Weights, out similarity, ExclusionSim);
                //if (nearestNeighbor >= 0)
                //{
                //    match.Average(playerKnowledge[nearestNeighbor]);
                //    playerKnowledge.RemoveAt(nearestNeighbor);
                //}
                //playerKnowledge.Add(match);
                point = match.Weights;
            }
            else
            {
                // There is not a match so we will make one.

                ARTTree<T> childTree = null;
                // Make a new tree with the next values e
                if (ChildVigilance != null)
                    childTree = new ARTTree<T>(ChildVigilance,ChildExclusion);
                playerKnowledge.Add(new Node(Vigilance, point, hValue,childTree));
            }

            return point;
        }

        private int FindBestMatch(List<Node> neurons, VectorN point, out float similarity, float threshold = -1)
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

        private void InitializeNeurons()
        {
            Nodes = new Dictionary<int, List<Node>>();
            List<int> players = Definition.GetPlayers();
            foreach (int v in players)
                Nodes.Add(v, new List<Node>());
        }
        #endregion
        
        public void Load(string fileName)
        {
            Nodes = null;
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                if (f.Length != 0)
                    Nodes = bf.Deserialize(f) as Dictionary<int, List<Node>>;
            }
            if (Nodes == null)
            {
                InitializeNeurons();
            }
        }
        public void Save(string fileName)
        {
            using (Stream f = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(f, Nodes);
            }
        }
    }
}
