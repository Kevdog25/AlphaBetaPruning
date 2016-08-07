using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using AlphaBetaPruning.Shared;
using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

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

            private Distribution<StandardizedState> stateDist;
            private Distribution<Action> actionDist;
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
                if (IsLeaf)
                {
                    initial.Add(actionDist);
                }
                Distribution<Action> acts = new Distribution<Action>();
                
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
            // TODO:
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
