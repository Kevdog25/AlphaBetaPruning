using System;
using System.Collections.Generic;
using AlphaBetaPruning.AILearner;

namespace AlphaBetaPruning
{
    class Minimax
    {
        private Game game;
        private ILearner Learner;
        private bool LearningMode;
        public Minimax(Game inGame)
        {
            game = inGame;
        }

        public void SetLearner(ILearner learner, bool learningMode = true)
        {
            Learner = learner;
            LearningMode = learningMode;
        }

        /// <summary>
        /// Given a player to play as, finds the best move for them to pick.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="player"></param>
        public Action FindBestMove(IGameState state,int depth = 10)
        {
            List<Action> actions = game.AvailableActions(state);
            float a = float.NegativeInfinity;
            float b = float.PositiveInfinity;
            List<Action> bestActions = new List<Action>();
            foreach(Action act in actions)
            {
                IGameState s = act.Act(state);
                float e = Evaluate(s, a, b, false,depth);
                if (e > a)
                {
                    bestActions.Clear();
                    bestActions.Add(act);
                    a = e;
                }
                else if(e == a)
                {
                    bestActions.Add(act);
                }
            }

            //if(learner != null)
            //    learner.ResolveBuffer();

            return bestActions[Utils.RandInt(0,bestActions.Count)];
        }

        /// <summary>
        /// Runs through an alpha-beta pruning routine to find the expected value of 
        /// the state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="maxToPlay"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        float Evaluate(IGameState state,float a, float b, bool maxToPlay,int depth)
        {
            if (depth == 0 || game.TerminalStateCheck(state))
            {
                return game.Heuristic(state)*(maxToPlay ? 1 : -1);
            }
            List<Action> actions = game.AvailableActions(state);
            if(actions.Count == 0)
            {
                throw new GameSpecificationException("State has no available actions, but was not caught as terminal.");
            }
            
            Action prunedAction = null;
            IActionClass suggestion = null;
            if (Learner != null)
            { 
                suggestion = Learner.GetSuggestion(state);
                if (suggestion != null)
                    suggestion.Reorder(actions);
            }

            float e;
            if (maxToPlay)
            {
                e = float.NegativeInfinity;
                foreach(Action act in actions)
                {
                    e = Math.Max(Evaluate(act.Act(state), a, b, !maxToPlay, depth - 1), e);
                    a = Math.Max(a, e);

                    // Node Pruning
                    if (a > b)
                    {
                        prunedAction = act;
                        break;
                    }
                }
            }
            else
            {
                e = float.PositiveInfinity;
                foreach (Action act in actions)
                {
                    e = Math.Min(Evaluate(act.Act(state), a, b, !maxToPlay, depth - 1), e);
                    b = Math.Min(b, e);

                    // Node Pruning
                    if (a > b)
                    {
                        prunedAction = act;
                        break;
                    }
                }
            }

            if (LearningMode && prunedAction != null)
            {
                if (suggestion == null || !suggestion.IsMember(prunedAction))
                    Learner.BufferLearn(state, prunedAction);
            }

            return e;
        }
    }
}
