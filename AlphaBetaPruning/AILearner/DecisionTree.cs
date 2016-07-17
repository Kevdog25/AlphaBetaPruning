using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AILearner
{
    abstract class DecisionTree : ILearner
    {
        #region Fields
        protected delegate bool Question(StateResponse s);

        private Node Root;
        private List<IGameState> StateBuffer;
        private List<Action> ActionBuffer;
        private List<StateResponse> Knowledge;
        private List<HashSet<int>> PossibleValues;
        private readonly float log2 = (float)(Math.Log(2));
        #endregion

        #region Classes
        protected class StateResponse
        {
            public int Dimensions { get; protected set; }
            public Action Response { get; set; }
            private int[] values;

            public StateResponse(int d, Action act = null)
            {
                Dimensions = d;
                values = new int[d];
                Response = act;
            }

            public int this[int i]
            {
                get
                {
                    if (i < 0 || i >= Dimensions)
                        throw new IndexOutOfRangeException("Index out of range with value " + i);
                    return values[i];
                }
                set
                {
                    values[i] = value;
                }
            }
        }

        private class Node
        {
            public Question Question { get; private set; }
            public Node TrueNode { get; private set; }
            public Node FalseNode { get; private set; }
            public IActionClass Classification { get; private set; }

            private List<int> TraceData;

            public Node(IActionClass c)
            {
                Classification = c;
                TrueNode = null;
                FalseNode = null;
                Question = null;
            }

            public Node(Question q, Node nTrue, Node nFalse)
            {
                Question = q;
                TrueNode = nTrue;
                FalseNode = nFalse;
            }

            public void SetTraceData(List<int> td)
            {
                TraceData = td;
            }
        }
        #endregion

        #region Constructors
        public DecisionTree()
        {
            StateBuffer = new List<IGameState>();
            ActionBuffer = new List<Action>();
            Knowledge = new List<StateResponse>();
            PossibleValues = new List<HashSet<int>>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Uses the decision tree to map the state to an action class. The action class can then be used
        /// to determine if any of your actions fall into the action class and is considered a suggestion.
        /// </summary>
        /// <param name="state">State to apply the decision tree on.</param>
        /// <returns>Action class for comparison of actions and suggestions.</returns>
        public IActionClass GetSuggestion(IGameState state)
        {
            if (Root == null)
                return null;
            Node n = Root;
            StateResponse sr = Convert(state);
            while (n.Classification == null)
            {
                if (n.Question(sr))
                    n = n.TrueNode;
                else
                    n = n.FalseNode;
            }

            return n.Classification;
        }

        /// <summary>
        /// Adds the state to the knowledge base and processes the addition immediately. This is not gauranteed to be available,
        /// and may defer to BufferLearn if it incremental learning is not appropriate.
        /// </summary>
        /// <param name="state">State to add</param>
        /// <param name="act">Action associated with the corrosponding state</param>
        public void Learn(IGameState state, Action act)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the state to the knowledge base buffer. This can then be processed all at once using ResolveBuffer.
        /// </summary>
        /// <param name="state">State to add</param>
        /// <param name="act">Action associated with the corrosponding state</param>
        public void BufferLearn(IGameState state, Action act)
        {
            StateBuffer.Add(state);
            ActionBuffer.Add(act);
        }

        public virtual void ResolveBuffer()
        {
            for(var i = 0; i < StateBuffer.Count; i++)
                Knowledge.Add(Convert(StateBuffer[i], ActionBuffer[i]));

            Root = null;
            GC.Collect();
            BuildTree();
        }

        public void BuildTree()
        {
            if (Knowledge.Count == 0)
                return;
            int dimensions = Knowledge[0].Dimensions;
            PossibleValues.Clear();
            for(var i = 0; i < dimensions; i++)
            {
                HashSet<int> set = new HashSet<int>();
                for(var j = 0; j < Knowledge.Count; j++)
                    if (!set.Contains(Knowledge[j][i]))
                        set.Add(Knowledge[j][i]);
                PossibleValues.Add(set);
            }
            Root = BuildNode(Knowledge, Entropy(Knowledge));
        }

        /// <summary>
        /// Loads the serialized StateResponse items from the given file relying on the implementation of 
        /// DeserializeItem.
        /// </summary>
        /// <param name="fp">The path to the file.l</param>
        public void LoadFromDatabase(string fp)
        {
            if (!File.Exists(fp))
            {
                File.Create(fp);
            }
            using (StreamReader f = new StreamReader(fp))
            {
                Knowledge.Clear();
                string line;
                while (!f.EndOfStream)
                {
                    line = f.ReadLine();
                    Knowledge.Add(DeserializeItem(line));
                }
            }
        }

        /// <summary>
        /// Saves the current knowledge to the database. Relies on the implementation of SerializeItem.
        /// </summary>
        /// <param name="fp">Path to the file</param>
        /// <param name="append">Whether or not to append to the file if it exists</param>
        public void SaveToDatabase(string fp, bool append = true)
        {
            using (StreamWriter f = new StreamWriter(fp, append))
            {
                for (var i = 0; i < Knowledge.Count; i++)
                {
                    f.WriteLine(SerializeItem(Knowledge[i]));
                }
            }
        }
        #endregion

        #region Overridable Methods
        protected abstract IActionClass GenerateClassification(Dictionary<Action, float> dict);
        protected abstract StateResponse Convert(IGameState state,Action response = null);
        protected abstract StateResponse DeserializeItem(string item);
        protected abstract string SerializeItem(StateResponse item);
        protected virtual List<int> AddTraceData(Action act) { return null; }
        #endregion

        #region Private Methods
        private float Entropy(List<StateResponse> states)
        {
            Dictionary<Action, float> dist = GetClassDist(states);

            float s = 0;
            foreach(int v in dist.Values)
                s -= (float)Math.Log(v) * v;

            return s;
        }

        private Node BuildNode(List<StateResponse> states, float s)
        {
            float bestGain = 0, gain, p;
            Question bestQ = null;
            List<StateResponse> bestF = null;
            List<StateResponse> bestT = null;
            int nStates = states.Count;
            for(var i = 0; i < states[0].Dimensions; i++)
            {
                foreach(int v in PossibleValues[i])
                {
                    List<StateResponse> f = new List<StateResponse>();
                    List<StateResponse> t = new List<StateResponse>();

                    for(var j = 0; j < nStates; j++)
                    {
                        if (states[j][i] == v)
                            t.Add(states[j]);
                        else
                            f.Add(states[j]);
                    }

                    p = (float)(t.Count) / nStates;
                    gain = s - p*Entropy(t) - (1-p)*Entropy(f);

                    if(gain > bestGain && t.Count > 0 && f.Count > 0)
                    {
                        bestGain = gain;
                        bestT = t;
                        bestF = f;
                        int value = v;
                        int d = i;
                        bestQ = x => {
                            return x[d] == v;
                        };
                    }
                }
            }
            s -= bestGain;
            Node n;
            if (bestGain > 0)
                n = new Node(bestQ, BuildNode(bestT, s), BuildNode(bestF, s));
            else
            {
                Dictionary<Action, float> dist = GetClassDist(states);
                n = new Node(GenerateClassification(dist));
            }

            return n;
        }

        private Dictionary<Action, float> GetClassDist(List<StateResponse> states)
        {
            Dictionary<Action, float> count = new Dictionary<Action, float>();

            float c;
            int total = states.Count;
            for (var i = 0; i < total; i++)
            {
                Action act = states[i].Response;
                if (count.TryGetValue(act, out c))
                {
                    count[act] = c + 1;
                }
                else
                {
                    count[act] = 1;
                }
            }

            List<Action> keys = count.Keys.ToList();
            for(var i = 0; i < keys.Count; i++)
            {
                count[keys[i]] /= total;
            }

            return count;
        }
        #endregion
    }
}
