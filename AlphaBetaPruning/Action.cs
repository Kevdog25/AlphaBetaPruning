using System;

namespace AlphaBetaPruning
{
    [Serializable]
    abstract class Action
    {
        [Serializable]
        public delegate IGameState GameAction(IGameState state);

        GameAction act;

        public Action(GameAction act)
        {
            this.act = act;
        }

        public virtual IGameState Act(IGameState state)
        {
            try
            {
                return act(state);
            }
            catch(Exception ex)
            {
                throw new GameSpecificationException("Cannot act on " + state.GetType().ToString(),ex);
            }
        }

        public abstract bool Equals(Action other);
    }
}
