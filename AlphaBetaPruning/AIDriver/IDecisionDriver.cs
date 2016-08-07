using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = AlphaBetaPruning.Shared.Action;
using AlphaBetaPruning.GameDefinitions;

namespace AlphaBetaPruning.AIDriver
{
    interface IDecisionDriver
    {
        Action FindBestMove(IGameState state);
    }
}
