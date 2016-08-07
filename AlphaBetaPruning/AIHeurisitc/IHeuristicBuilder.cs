using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.AIHeurisitc
{
    interface IHeuristicBuilder
    {
        float Judge(IGameState state);
        void Learn(IGameState initialState, List<Action> moves, int winner);
    }
}
