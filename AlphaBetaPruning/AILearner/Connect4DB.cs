using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlphaBetaPruning.GameDefinitions;

namespace AlphaBetaPruning
{
    class Connect4DB : IStateDB
    {
        SortedList<Action,List<Connect4.Connect4State>> stateList;

        public Connect4DB()
        {
            stateList = new SortedList<Action, List<Connect4.Connect4State>>();
        }

        public void AddState(IGameState inState, Action act)
        {
            Connect4.Connect4State state = tryCast(inState);
            List<Connect4.Connect4State> l = null;
            if (stateList.TryGetValue(act, out l))
                l.Add(state);
            else
                stateList.Add(act, new List<Connect4.Connect4State>() { state });
        }

        public Action GetStateValue(IGameState inState)
        {
            Connect4.Connect4State state = tryCast(inState);
            foreach(KeyValuePair<Action,List<Connect4.Connect4State>> pair in stateList)
            {
                if (pair.Value.Contains(state))
                    return pair.Key;
            }
            return null;
        }

        private Connect4.Connect4State tryCast(IGameState inS)
        {
            Connect4.Connect4State s = inS as Connect4.Connect4State;
            if (s == null)
                throw new GameSpecificationException("Cannot cast this thing");
            return s;
        }

        private class CompareStates : IComparer<Connect4.Connect4State>
        {
            public int Compare(Connect4.Connect4State x, Connect4.Connect4State y)
            {
                int n = x.NumberOfMoves - y.NumberOfMoves;
                if (n!=0)
                    return n;
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        n = x.board[i, j] - y.board[i, j];
                        if (n != 0)
                            return n;
                    }
                }
                return 0;
            }
        }
    }
}
