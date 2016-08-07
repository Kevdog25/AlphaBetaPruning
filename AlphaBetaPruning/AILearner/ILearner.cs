using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.AILearner
{
    interface ILearner
    {
        IActionClass GetSuggestion(IGameState state);
        void Learn(IGameState state, Action act);
        void BufferLearn(IGameState state, Action act);
        void ResolveBuffer();
    }
}
