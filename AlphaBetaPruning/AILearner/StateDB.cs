using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning
{
    interface IStateDB
    {
        Action GetStateValue(IGameState inState);
        void AddState(IGameState inState, Action act);
    }
}
