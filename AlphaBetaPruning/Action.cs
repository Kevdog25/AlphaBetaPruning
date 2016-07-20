using System;

namespace AlphaBetaPruning
{
    [Serializable]
    abstract class Action: IComparable<Action>, IEquatable<Action>
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

        public abstract int CompareTo(Action obj);

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();

        public abstract bool Equals(Action other);
    }
}
