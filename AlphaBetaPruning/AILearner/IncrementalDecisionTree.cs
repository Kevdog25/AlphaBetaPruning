using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using AlphaBetaPruning.Shared;
using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

/// <summary>
/// IN CONSTRUCTION
/// -K
/// </summary>
namespace AlphaBetaPruning.AILearner
{
    abstract class IncrementalDecisionTree : DecisionTree, ILearner
    {
        private Node Root;

        #region Classes
        private class Node
        {
            public bool IsLeaf { get; private set; }
            public Node False;
            public Node True;
            public bool Dirty;

            public Distribution<StandardizedState> stateDist;
            public Distribution<Action> actionDist;
            private int KnowledgeSize;
            public int TestDimension;
            public int TestValue;

            #region Constructors
            public Node()
            {
                IsLeaf = true;
                stateDist = null;
                actionDist = null;
                False = null;
                True = null;
                Dirty = true;
            }

            public Node(int dimension, int value,Node f, Node t) : this()
            {
                IsLeaf = false;
                False = f;
                True = t;
                TestDimension = dimension;
                TestValue = value;
            }

            public Node(List<StateResponse> knowledge = null) : this()
            {
                if (knowledge != null)
                    KnowledgeSize = knowledge.Count;
            }

            public Node(StateResponse knowledge) : this()
            {
                KnowledgeSize = 1;
            }

            public Node(Distribution<StandardizedState> sDist, Distribution<Action> aDist) : this()
            {
                KnowledgeSize = sDist.NElements;
                stateDist = sDist;
                actionDist = aDist;
            }
            #endregion
            
            /// <summary>
            /// Adds a state and response pair to the knowledge stored locally on the node.
            /// </summary>
            /// <param name="sr"></param>
            public void AddKnowledge(StateResponse sr)
            {
                stateDist.Add(sr.State);

                actionDist.Add(sr.Response);
                KnowledgeSize++;
            }

            /// <summary>
            /// Decides whether or not the state is catagorized as false or true based on the 
            /// test parameters on this internal node.
            /// </summary>
            /// <param name="state"></param>
            /// <returns></returns>
            public bool Test(StandardizedState state)
            {
                if (IsLeaf)
                    throw new Exception("Cannot test on a non internal node.");
                return state[TestDimension] == TestValue;
            }

            /// <summary>
            /// Gets the distribution of states at this. Recursively looks through the children to 
            /// retrieve the data from the leaves.
            /// </summary>
            /// <param name="initial"></param>
            public void GetStateDist(Distribution<StandardizedState> initial)
            {
                if (IsLeaf)
                    initial.Add(stateDist);
                else
                {
                    if (False != null) False.GetStateDist(initial);
                    if (True != null) True.GetStateDist(initial);
                }
            }

            /// <summary>
            /// Gets the distribution of actions at this node given the proposed test value and dimension.
            /// Recusively looks through the children to retrieve the information stored at the leaves.
            /// </summary>
            /// <param name="testDimension"></param>
            /// <param name="testValue"></param>
            /// <returns>The dsitribution of actions at this node.</returns>
            public void GetActionDist(Distribution<Action> initial, int testDimension = -1, int testValue = -1)
            {
                // TODO: This ignored the actual states at the leaf if the value has not been tested on yet.
                if (IsLeaf)
                {
                    initial.Add(actionDist);
                }
                
                // If we are branching on the given test, then add only the true branch.
                // Otherwise, the false has potentially valid nodes.
                if(!(testDimension == TestDimension && testValue == TestValue))
                    False.GetActionDist(initial,testDimension, testValue);
                True.GetActionDist(initial,testDimension, testValue);
            }
            
            /// <summary>
            /// Turns a leaf into an internal branch node using the given test values.
            /// </summary>
            /// <param name="testDimension"></param>
            /// <param name="testValue"></param>
            public void Split(int testDimension, int testValue)
            {
                Debug.Assert(IsLeaf,"Cannot split an internal node");
                int nTrue = 0;
                int nFalse = KnowledgeSize - nTrue;

                // TODO: Finish the split and convert from Distribution<int> to Distribution<StandardizedState>
            }

            public bool KnowsAbout(StandardizedState state)
            {
                return stateDist.Contains(state);
            }

            public bool KnowsAbout(Action response)
            {
                return actionDist.Contains(response);
            }

            public void SetData(Distribution<StandardizedState> sDist, Distribution<Action> aDist)
            {
                KnowledgeSize = sDist.NElements;
                stateDist = sDist;
                actionDist = aDist;
                Dirty = true;
                IsLeaf = true;
                False = null;
                True = null;
            }

            public void SetData(int dimension, int value, Node f, Node t)
            {
                IsLeaf = false;
                False = f;
                True = t;
                TestDimension = dimension;
                TestValue = value;
            }
        }
        #endregion

        #region ILearner
        public void BufferLearn(IGameState state, Action act)
        {
            throw new NotImplementedException();
        }

        public IActionClass GetSuggestion(IGameState state)
        {
            StateResponse sr = Convert(state);
            Node n = Root;
            while (!n.IsLeaf)
            {
                if (n.Test(sr.State))
                    n = n.True;
                else
                    n = n.False;
            }
            Distribution<Action> dist = new Distribution<Action>();
            n.GetActionDist(dist);
            return new DTActionClass(dist);
        }

        public void Learn(IGameState state, Action act)
        {
            throw new NotImplementedException();
        }

        public void ResolveBuffer()
        {
            throw new NotImplementedException();
        }
        #endregion
        
        /// <summary>
        /// Transposes the node n and its children from n -> y,y to y -> n,n. Requires that both the children of 
        /// n have the same dimension and value test.
        /// </summary>
        /// <param name="n"></param>
        private void Transpose(ref Node n)
        {
            Node nn = n.False.False;
            Node ny = n.False.True;
            Node yn = n.True.False;
            Node yy = n.True.True;

            Node root = n.True;
            root.False = new Node(n.TestDimension, n.TestValue, nn, yn);
            root.True = new Node(n.TestDimension, n.TestValue, ny, yy);

            n = root;
        }

        /// <summary>
        /// Pulls up the desired test node from the leaves of the sub tree.
        /// Ensures that the desired test is placed at the root without
        /// disrupting the validity of the tree.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="testD"></param>
        /// <param name="testV"></param>
        private void PullUp(ref Node root, int testD, int testV)
        {
            if (root.TestValue == testV && root.TestDimension == testD)
                return;
            if (root.IsLeaf)
                root.Split(testD, testV);
            else
            {
                PullUp(ref root.True, testD, testV);
                PullUp(ref root.False, testD, testV);
                Transpose(ref root);
            }
        }

        /// <summary>
        /// Follows the correct branch of the tree to add the state and response to a leaf node for storage.
        /// Marks all touched branches as dirty.
        /// </summary>
        /// <param name="sr"></param>
        private void AddToTree(StateResponse sr)
        {
            if (Root == null)
                Root = new Node(sr);
            else
            {
                Node n = Root;
                n.Dirty = true;
                while (!n.IsLeaf)
                {
                    if (n.Test(sr.State))
                        n = n.True;
                    else
                        n = n.False;
                    n.Dirty = true;
                }

                // Check to see if the state can be safely added to the existing node
                if (n.stateDist.NUnique == 0 && n.stateDist.Contains(sr.State)
                    || n.actionDist.NUnique == 0 && n.actionDist.Contains(sr.Response))
                {
                    n.AddKnowledge(sr);
                }
                else
                {
                    // It cannot, so we must reinflate the knowledge base and grow the subtree
                    List<StateResponse> knowledge = ReconstructKnowledge(n.stateDist, n.actionDist);
                    List<StandardizedState> states = new List<StandardizedState>();
                    List<Action> responses = new List<Action>();
                    foreach(var s in knowledge)
                    {
                        states.Add(s.State);
                        responses.Add(s.Response);
                    }

                    GrowTree(n,states, responses);
                }
            }
        }

        private List<StateResponse> ReconstructKnowledge(Distribution<StandardizedState> states, Distribution<Action> responses)
        {
            Debug.Assert(states.NElements == responses.NElements,"Cannot recreate the knowledge base from un matching distributions");
            Debug.Assert(states.NUnique == 1 && responses.NUnique == 1, "Cannot figure out how to construct the correct StateResponse items if there is a non-trivial distribution of both");
            
            List<StateResponse> knowledge = new List<StateResponse>();
            while (states.NElements > 0)
                knowledge.Add(new StateResponse(states.RemoveAny(), responses.RemoveAny()));

            return knowledge;
        }

        private void GrowTree(Node n, List<StandardizedState> states, List<Action> responses, HashSet<int>[] possibleValues = null)
        {
            // Construct a set of possible values for each node
            // A local reference to the possible values must be created so that the possibles can be passed to the child
            // branches safely.
            HashSet<int>[] localPossibles = new HashSet<int>[NDimensions];
            if (possibleValues == null)
            {
                for (var i = 0; i < NDimensions; i++)
                {
                    localPossibles[i] = new HashSet<int>();
                    for(var j = 0; j < states.Count; j++)
                        localPossibles[i].Add(states[j][i]);
                }
            }
            else
            {
                for (var i = 0; i < NDimensions; i++)
                    localPossibles[i] = new HashSet<int>(possibleValues[i]);
            }

            // Check to see if there are different actions in the responses list.
            bool differentActions = responses.Count > 0;
            if (responses.Count > 0)
            {
                Action a = responses[0];
                for (var i = 1; i < responses.Count; i++)
                {
                    if (!responses[i].Equals(a))
                    {
                        differentActions = true;
                        break;
                    }
                }
            }

            // All of the actions are the same, so a leaf should be made
            if (!differentActions)
            {
                n.SetData(new Distribution<StandardizedState>(states), new Distribution<Action>(responses));
                return;
            }

            int bestV = -1;
            int bestD = -1;

            List<StandardizedState> fStates = null;
            List<Action> fResponses = null;
            List<StandardizedState> tStates = null;
            List<Action> tResponses = null;
            List<StandardizedState> bestFStates = null;
            List<Action> bestFResponses = null;
            List<StandardizedState> bestTStates = null;
            List<Action> bestTResponses = null;

            float bestS = float.PositiveInfinity;
            float s;
            for(var i = 0; i < NDimensions; i++)
            {
                // Skip trying to branch on values where we 
                // know there will be no false.
                if (localPossibles[i].Count == 1)
                    continue;
                foreach(int v in localPossibles[i])
                {
                    fStates = new List<StandardizedState>();
                    fResponses = new List<Action>();
                    tStates = new List<StandardizedState>();
                    tResponses = new List<Action>();

                    // Build the distributions of states and responses after the branching test.
                    for(var j = 0; j < states.Count; j++)
                    {
                        if(states[j][i] == v)
                        {
                            tStates.Add(states[i]);
                            tResponses.Add(responses[i]);
                        }
                        else
                        {
                            fStates.Add(states[i]);
                            fResponses.Add(responses[i]);
                        }
                    }

                    Distribution<Action> fDist = new Distribution<Action>(fResponses);
                    Distribution<Action> tDist = new Distribution<Action>(tResponses);
                    float p = (float)tResponses.Count / states.Count;

                    s = p * tDist.Entropy + (1 - p) * fDist.Entropy;

                    if(s < bestS)
                    {
                        bestS = s;
                        bestFStates = fStates;
                        bestFResponses = fResponses;
                        bestTStates = tStates;
                        bestTResponses = tResponses;
                        bestV = v;
                        bestD = i;
                    }
                }
            }

            // If bestS was not updated, then all of the states are the same.
            if (bestS == float.PositiveInfinity)
                n.SetData(new Distribution<StandardizedState>(states), new Distribution<Action>(responses));
            else
            {
                Node f = new Node();
                Node t = new Node();
                GrowTree(f, bestFStates, bestFResponses, localPossibles);
                GrowTree(t, bestTStates, bestTResponses, localPossibles);
                n.SetData(bestD, bestV, f, t);
            }
        }

        /// <summary>
        /// Restructures the tree from Root to ensure that the best branching is 
        /// done at every point. Should be done after adding knowledge to the tree.
        /// Is resiliant to calling at inappropriate times.
        /// </summary>
        private void Reorder()
        {
            // TODO:
        }

        /// <summary>
        /// Prunes the leaves of the tree to remove trivially unneccessary branchings.
        /// </summary>
        /// <param name="n"></param>
        private void PruneTree(Node n)
        {
            // TODO:
        }
    }
}
