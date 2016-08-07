using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.GameDefinitions
{
    abstract class Game
    {
        /// <summary>
        /// Returns a game state that represents the initial state for the game.
        /// </summary>
        /// <returns></returns>
        public abstract IGameState NewGame();

        /// <summary>
        /// Checks to see if the game has reached any of its game over conditions.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public abstract bool TerminalStateCheck(IGameState inState);

        /// <summary>
        /// Returns a list of actions available to transition the state.
        /// The actions are ordered such that the actions that are expected to
        /// be most important to evaluate are first.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public abstract List<Action> AvailableActions(IGameState inState);

        /// <summary>
        /// Returns the estimated score for a given game state.
        /// </summary>
        /// <param name="state">Game state to evaluate.</param>
        /// <returns></returns>
        public abstract float Heuristic(IGameState inState);

        public virtual Action RandomMove(IGameState inState)
        {
            throw new NotImplementedException();
        }

        public virtual IGameState StateFromString(string input)
        {
            throw new NotImplementedException();
        }

        /* Methods used to translate user input into a game move. */
        public virtual IGameState MakeMove(IGameState current, string move)
        {
            throw new NotImplementedException();
        }

        public virtual IGameState MakeMove(IGameState current, int[] move)
        {
            throw new NotImplementedException();
        }

        public virtual IGameState MakeMove(IGameState current, Action move)
        {
            throw new NotImplementedException();
        }
        
    }
}
