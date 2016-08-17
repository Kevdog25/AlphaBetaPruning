using AlphaBetaPruning.GameDefinitions;
using AlphaBetaPruning.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AIHeurisitc
{
    interface IARTDefinition
    {
        VectorN Convert(IGameState inState, out int player);
        List<int> GetPlayers();
    }
}
