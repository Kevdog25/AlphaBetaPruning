using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AIDriver
{
    class MCUTC : IDecisionDriver
    {
        private Game game;
        private Node root;
        private float MaxTime;
        private int MaxInterations;

        #region Classes
        private class MCUTCException : Exception
        {
            public MCUTCException() : base()
            {

            }

            public MCUTCException(string message) : base(message)
            {

            }

            public MCUTCException(string message, Exception ex) : base(message,ex)
            {

            }
        }

        private class Node
        {
            public float Value;
            public int NVisits;
            public List<Node> Children;
            List<Action> UntriedMoves;
            public IGameState State;
            public Action Move;
            public Node Parent;

            readonly float ExplorationFactor = 0f;

            public Node(IGameState inState, List<Action> actions, Action move = null, Node parent = null)
            {
                Parent = parent;
                NVisits = 0;
                Value = 0;
                State = inState;
                Children = new List<Node>();
                UntriedMoves = actions;
                Move = move;
            }

            public Action RemoveRandomAction()
            {
                if (UntriedMoves.Count == 0)
                    throw new MCUTCException("Cannot add children to a fully expanded node.");
                int index = Utils.RandInt(0, UntriedMoves.Count);
                Action act = UntriedMoves[index];
                UntriedMoves.RemoveAt(index);
                return act;
            }

            public bool IsExpanded()
            {
                return UntriedMoves.Count == 0;
            }

            public bool isExpandable()
            {
                return UntriedMoves.Count > 0;
            }

            public Node SelectChild()
            {
                Node c = null;
                double best = double.MinValue;
                for(var i = 0; i < Children.Count; i++)
                {
                    Node ci = Children[i];
                    double next = ci.Value / ci.NVisits/200.0f + ExplorationFactor*Math.Sqrt(2*Math.Log(NVisits) / ci.NVisits);
                    if (next > best)
                    {
                        c = ci;
                        best = next;
                    }
                }
                return c;
            }

            public void AppendToStream(StreamWriter fout)
            {
                fout.WriteLine(string.Format("<node value=\"{0}\">",Value));
                if (Children.Count == 0)
                    fout.WriteLine("leaf");
                foreach(Node c in Children)
                {
                    c.AppendToStream(fout);
                }
                fout.WriteLine("</node>");
            }
        }
        #endregion

        #region Constructors
        public MCUTC(Game inGame, float maxTime = float.PositiveInfinity,
            int maxIterations = int.MaxValue)
        {
            game = inGame;
            MaxTime = maxTime;
            MaxInterations = maxIterations;
        }
        #endregion

        #region IDecisionDriver
        public Action FindBestMove(IGameState state)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            root = new Node(state, game.AvailableActions(state));

            int iter = 0;
            while (sw.ElapsedMilliseconds / 1000 < MaxTime && iter < MaxInterations)
            {
                iter++;
                Node node = root;

                bool isTerminal = game.TerminalStateCheck(node.State);
                while (node.IsExpanded() && !isTerminal)
                {
                    node = node.SelectChild();
                    isTerminal = game.TerminalStateCheck(node.State);
                }

                if (!isTerminal)
                {
                    Action act = node.RemoveRandomAction();
                    IGameState nextState = act.Act(node.State);
                    Node c = new Node(nextState, game.AvailableActions(nextState), move : act, parent : node);
                    node.Children.Add(c);
                    node = c;
                }

                IGameState currentState = node.State;
                while (!game.TerminalStateCheck(currentState))
                {
                    currentState = game.RandomMove(currentState).Act(currentState);
                }

                float h = game.Heuristic(currentState);
                while (node != null)
                {
                    float nextVal = 0;
                    if (Math.Abs(h) > 100)
                    {
                        if (currentState.GetCurrentPlayer() == node.State.GetCurrentPlayer())
                            nextVal = 1;
                        else
                            nextVal = 0.5f;
                    }
                    node.NVisits++;
                    node.Value = (((float)(node.NVisits - 1) / node.NVisits) * node.Value) + (nextVal / node.NVisits);
                    node = node.Parent;
                }
            }
            sw.Stop();

            float bestV = float.MinValue;
            Action bestAct = null;
            for(var i = 0; i < root.Children.Count; i++)
            {
                Node ci = root.Children[i];
                float value = ci.Value;
                if (value > bestV)
                {
                    bestAct = ci.Move;
                    bestV = value;
                }
                Console.WriteLine(string.Format("Value: {0}. NVisit: {1}|| {2}", bestV, ci.NVisits,ci.State.ToString().Replace('\n',' ')));
            }
            Console.WriteLine("Chosen State Value: " + bestV.ToString());
            return bestAct;
        }
        #endregion

        #region MCUTC Public
        public void DumpTree(StreamWriter fout)
        {
            fout.WriteLine("<xml version = \"1.0\" encoding = \"UTF-8\">");
            fout.WriteLine("<MCUTCTree>");
            root.AppendToStream(fout);
            fout.WriteLine("</MCUTCTree>");
            fout.WriteLine("</xml>");
        }
        #endregion
    }
}
