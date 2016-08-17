using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.GameDefinitions;
using AlphaBetaPruning.Shared;

namespace AlphaBetaPruning.AIHeurisitc
{
    [Serializable]
    class Connect4ART : IARTDefinition
    {
        public VectorN Convert(IGameState inState, out int player)
        {
            Connect4.Connect4State state = TryCast(inState);
            float[] values = new float[84];
            player = (int)state.toMove;
            for(var i = 0; i < 42; i ++)
            {
                if (state.board[i / 7, 1 + i % 6] == (int)Connect4.Player.Red)
                    values[2 * i] = 1;
                else if (state.board[i / 7, 1 + i % 6] == (int)Connect4.Player.Black)
                    values[2 * i + 1] = 1;
            }

            return new VectorN(values);
        }

        public List<int> GetPlayers()
        {
            List<int> players = new List<int>();
            foreach(Connect4.Player p in Enum.GetValues(typeof(Connect4.Player)))
            {
                players.Add((int)p);
            }
            return players;
        } 

        private Connect4.Connect4State TryCast(IGameState inS)
        {
            Connect4.Connect4State s = inS as Connect4.Connect4State;
            if (s == null)
                throw new GameSpecificationException("Cant cast this");
            return s;
        }
    }
}
