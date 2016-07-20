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
        private List<StateResponse> Buffer;
        private List<StateResponse> Knowledge;
        private readonly float log2 = (float)(Math.Log(2));
        #endregion

        #region Classes
        protected class StateResponse: IEquatable<StateResponse>
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

            public override bool Equals(object obj)
            {
                return Equals(obj as StateResponse);
            }

            public bool Equals(StateResponse other)
            {
                return (values.Equals(other.values)) && (Response.Equals(other.Response));
            }

            public override int GetHashCode()
            {
                return Response.GetHashCode() + values.GetHashCode();
            }
        }

        private class Node
        {
            public Question Question { get; private set; }
            public Node TrueNode { get; private set; }
            public Node FalseNode { get; private set; }
            public IActionClass Classification { get; private set; }
            
            private const string indent = "\t";
            private int[] TraceData;

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

            public void SetTraceData(int[] td)
            {
                TraceData = td;
            }

            public string ToXML(int level = 0,string prepend = "")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(prepend);
                for (int i = 0; i < level; i++)
                    sb.Append(indent);
                sb.Append("<node");
                if (Classification == null)
                {
                    sb.Append(" value=\"(");
                    for(var i = 0; i < TraceData.Length-1; i++)
                    {
                        sb.Append(TraceData[i] + ",");
                    }
                    sb.Append(TraceData[TraceData.Length - 1] + ")\" >\n");
                    sb.Append(TrueNode.ToXML(level + 1));
                    sb.Append(FalseNode.ToXML(level + 1));
                }
                else
                {
                    sb.Append(">");
                    sb.Append(Classification.ToString());
                }

                for (int i = 0; i < level; i++)
                    sb.Append(indent);
                sb.Append("</node>\n");

                return sb.ToString();
            }
        }
        #endregion

        #region Constructors
        public DecisionTree()
        {
            Buffer = new List<StateResponse>();
            Knowledge = new List<StateResponse>();
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
            StateResponse sr = Convert(state, act);
            if (!Buffer.Contains(sr))
                Buffer.Add(sr);
        }

        /// <summary>
        /// This assimilates the knowledge added with BufferLearn to the decision tree model.
        /// </summary>
        public virtual void ResolveBuffer()
        {
            for (var i = 0; i < Buffer.Count; i++)
            {
                if(!Knowledge.Contains(Buffer[i]))
                    Knowledge.Add(Buffer[i]);
            }
            Root = null;
            Buffer.Clear();
            GC.Collect();
            BuildTree();
        }

        /// <summary>
        /// Initiates the tree building process from scratch. This can be a large cost depending on
        /// the knowledge base.
        /// </summary>
        public void BuildTree()
        {
            if (Knowledge.Count == 0)
                return;
            int dimensions = Knowledge[0].Dimensions;
            List<HashSet<int>> possibleValues = new List<HashSet<int>>();
            for(var i = 0; i < dimensions; i++)
            {
                HashSet<int> set = new HashSet<int>();
                for(var j = 0; j < Knowledge.Count; j++)
                    if (!set.Contains(Knowledge[j][i]))
                        set.Add(Knowledge[j][i]);
                possibleValues.Add(set);
            }
            Root = BuildNode(Knowledge, Entropy(Knowledge),possibleValues);
        }

        /// <summary>
        /// Converts the node structure into a well formatted XML string for human reading.
        /// </summary>
        /// <returns>String of XML</returns>
        public string ToXML()
        {
            if (Root == null)
                return "";
            return Root.ToXML();
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
                File.Create(fp).Close();
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
                f.Close();
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
                    f.Write(SerializeItem(Knowledge[i]));
                }
            }
        }
        #endregion

        #region Overridable Methods
        protected abstract IActionClass GenerateClassification(Dictionary<Action, float> dict);
        protected abstract StateResponse Convert(IGameState state,Action response = null);
        protected abstract StateResponse DeserializeItem(string item);
        protected abstract string SerializeItem(StateResponse item);
        protected virtual int[] AddTraceData(params int[] args) { return args; }
        #endregion

        #region Private Methods
        private float Entropy(List<StateResponse> states)
        {
            Dictionary<Action, float> dist = GetClassDist(states);

            float s = 0;
            foreach(float v in dist.Values)
                s -= (float)Math.Log(v) * v;

            return s;
        }

        private Node BuildNode(List<StateResponse> states, float s, List<HashSet<int>> possibleValues)
        {
            List<HashSet<int>> localPossibles = new List<HashSet<int>>(possibleValues);

            float bestGain = 0, gain, p;
            List<StateResponse> bestF = null;
            List<StateResponse> bestT = null;
            int bestV = 0, bestD = 0;
            int nStates = states.Count;
            for(var i = 0; i < states[0].Dimensions; i++)
            {
                foreach(int v in localPossibles[i])
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
                    // This uses the definition of relative gain that weights 
                    // gain from choosing a particular value by the number of values that there are to split on.
                    gain = (s - p * Entropy(t) - (1 - p) * Entropy(f)) / localPossibles[i].Count;

                    if(gain > bestGain && t.Count > 0 && f.Count > 0)
                    {
                        bestGain = gain;
                        bestT = t;
                        bestF = f;
                        bestV = v;
                        bestD = i;
                    }
                }
            }
            s -= bestGain;
            Node n;
            if (bestGain > 0)
            {
                bestGain *= localPossibles[bestD].Count; // Here it is converted back into absolute gain for passing onto the next stages
                localPossibles[bestD].Remove(bestV);
                n = new Node(x => { return x[bestD] == bestV; },
                    BuildNode(bestT, s - bestGain, localPossibles),
                    BuildNode(bestF, s - bestGain, localPossibles));
                n.SetTraceData(AddTraceData(bestD, bestV));
            }
            else
            {
                Dictionary<Action, float> dist = GetClassDist(states);
                n = new Node(GenerateClassification(dist));
                n.SetTraceData(AddTraceData(0));
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
                float v = count[keys[i]];
                count[keys[i]] = v/total;
            }

            return count;
        }
        #endregion
    }
}
