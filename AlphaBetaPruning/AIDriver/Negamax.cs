using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.AILearner;

namespace AlphaBetaPruning.AIDriver
{
    class Negamax : IDecisionDriver
    {
        #region Private Fields
        private Game game;
        private ILearner Learner;
        private bool LearningMode;
        private int Depth;
        #endregion

        #region Constructors
        public Negamax(Game inGame, int depth = 10)
        {
            game = inGame;
            Depth = depth;
        }
        #endregion

        #region IDecisionDriver
        public Action FindBestMove(IGameState state)
        {
            List<Action> actions = game.AvailableActions(state);
            float a = float.NegativeInfinity;
            float b = float.PositiveInfinity;
            List<Action> bestActions = new List<Action>();
            foreach (Action act in actions)
            {
                float e = -Evaluate(act.Act(state), -b, -a, Depth);
                if (e > a)
                {
                    bestActions.Clear();
                    bestActions.Add(act);
                    a = e;
                }
                else if (e == a)
                {
                    bestActions.Add(act);
                }
            }

            return bestActions[Utils.RandInt(0, bestActions.Count)];
        }
        #endregion

        #region Negamax Public
        public void SetLearner(ILearner learner, bool learningMode = true)
        {
            Learner = learner;
            LearningMode = learningMode;
        }
        #endregion

        #region Negamax Private
        private float Evaluate(IGameState state, float a, float b, int depth)
        {
            if (depth == 0 || game.TerminalStateCheck(state))
                return game.Heuristic(state);
            List<Action> actions = game.AvailableActions(state);
            if (actions.Count == 0)
                throw new GameSpecificationException("Found no available actions, but did not catch the state as terminal");

            IActionClass suggestion = null;
            Action prunedAction = null;

            // Re ordering the actions based on suggestion from the learning AI
            if (Learner != null)
            {
                suggestion = Learner.GetSuggestion(state);
                if (suggestion != null) suggestion.Reorder(actions);
            }

            // Do the actual negamax algorithm
            float e;
            e = float.NegativeInfinity;
            foreach (Action act in actions)
            {
                e = Math.Max(-Evaluate(act.Act(state), -b, -a, depth - 1), e);
                a = Math.Max(a, e);
                if (a > b)
                {
                    prunedAction = act;
                    break;
                }
            }

            // If we are learning, then submit the information for processing
            if (LearningMode && prunedAction != null)
                Learner.BufferLearn(state, prunedAction);

            return e;
        }
        #endregion
    }
}
