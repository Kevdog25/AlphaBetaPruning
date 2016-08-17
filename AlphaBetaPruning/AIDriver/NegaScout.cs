using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.AILearner;
using AlphaBetaPruning.AIHeurisitc;
using AlphaBetaPruning.Shared;

using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.AIDriver
{
    class NegaScout : IDecisionDriver
    {
        #region Private Fields
        private Game game;
        private float MoveTime;
        private Stopwatch StopWatch;
        private IHeuristicBuilder Heuristic;
        private int MaxDepth;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the driver with the correct game definitions and settings.
        /// </summary>
        /// <param name="inGame">definition of the game to play</param>
        /// <param name="moveTime">Time for each move in seconds.</param>
        public NegaScout(Game inGame, float moveTime, IHeuristicBuilder h, int maxDepth = int.MaxValue)
        {
            game = inGame;
            MoveTime = moveTime * 1000;
            Heuristic = h;
            this.StopWatch = new Stopwatch();
            MaxDepth = maxDepth;
        }
        #endregion

        #region IDecisionDriver
        /// <summary>
        /// This is an IDS search relying on the stopwatch to cut it off.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Action FindBestMove(IGameState state)
        {
            List<Action> actions = SortMoves(game.AvailableActions(state),state);
            List<Action> bestActions = new List<Action>();

            int depth = 0;
            this.StopWatch.Restart();
            List<Action> currentDepthBest = null;
            while ( this.StopWatch.ElapsedMilliseconds < MoveTime && depth <= MaxDepth)
            {
                bestActions = currentDepthBest;
                currentDepthBest = new List<Action>();
                float a = float.NegativeInfinity;
                float b = float.PositiveInfinity;
                foreach (Action act in actions)
                {
                    float e = -Evaluate(act.Act(state), -b, -a, depth);
                    if (e > a)
                    {
                        currentDepthBest.Clear();
                        currentDepthBest.Add(act);
                        a = e;
                    }
                    else if (e == a)
                    {
                        currentDepthBest.Add(act);
                    }
                }
                depth++;
            }
            this.StopWatch.Stop();

            return bestActions[Utils.RandInt(0, bestActions.Count)];
        }
        #endregion

        #region Negamax Public
        #endregion

        #region Negamax Private
        private float Evaluate(IGameState state, float a, float b, int depth)
        {
            if (this.StopWatch.ElapsedMilliseconds > MoveTime)
                return b+1;
            if (depth == 0 || game.TerminalStateCheck(state))
                return Heuristic.Judge(state);

            List<Action> actions = SortMoves(game.AvailableActions(state),state);
            if (actions.Count == 0)
                throw new GameSpecificationException("Found no available actions, but did not catch the state as terminal");

            // Do the actual negamax algorithm
            float e = float.NegativeInfinity;
            for(var i = 0; i < actions.Count; i++)
            {
                Action act = actions[i];

                // Assume the first move is the best.
                if(i == 0)
                {
                    e = Math.Max(-Evaluate(act.Act(state), -b, -a, depth - 1), e);
                    a = Math.Max(a, e);
                    if (a > b)
                    {
                        break;
                    }
                }
                else
                { 
                    float testE = -Evaluate(act.Act(state), -a, -a, depth - 1);
                    if (a <= testE)
                    {
                        e = Math.Max(-Evaluate(act.Act(state), -b, -a, depth - 1), e);
                        a = Math.Max(a, e);
                        if (a > b)
                        {
                            break;
                        }
                    }
                }
            }

            return e;
        }

        private List<Action> SortMoves(List<Action> actions, IGameState state)
        {
            List<ActionValuePair> avp = new List<ActionValuePair>();
            for (var i = 0; i < actions.Count; i++)
                avp.Add(new ActionValuePair(actions[i], Heuristic.Judge(actions[i].Act(state))));
            avp.Sort();
            List<Action> sorted = new List<Action>();
            for (var i = 0; i < avp.Count; i++)
                sorted.Add(avp[i].A);
            return sorted;
        }
        #endregion

        private class ActionValuePair : IComparable<ActionValuePair>
        {
            public Action A;
            public float Value;
            public ActionValuePair(Action a, float value)
            {
                A = a;
                Value = value;
            }

            public int CompareTo(ActionValuePair other)
            {
                return other.Value.CompareTo(Value);
            }
        }
    }
}
