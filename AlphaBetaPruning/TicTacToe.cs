using System;
using System.Collections.Generic;
using System.Linq;

namespace AlphaBetaPruning
{
    class TicTacToe : Game
    {
        private struct TicTacToeState : IGameState
        {
            public enum Players { x = 1, o = -1 };
            public string player;
            public string[,] Board;

            private static string ValidatePlayer(string playerString)
            {
                string p;
                if (Enum.GetNames(typeof(Players)).Contains(playerString))
                {
                    p = playerString;
                }
                else
                {
                    throw new GameSpecificationException("Invalid player specification: " + playerString);
                }
                return p;
            }

            public TicTacToeState(string[] inBoard, string playerString)
            {
                Board = new string[3, 3];
                if (inBoard.Length != 9)
                {
                    throw new GameSpecificationException("Invalid board specification in TicTacToeState.");
                }
                else
                {
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            Board[i, j] = inBoard[i * 3 + j];
                        }
                    }
                }
                player = ValidatePlayer(playerString);
            }

            public TicTacToeState(string[,] inBoard, string playerString)
            {
                if(inBoard.GetLength(0) != 3 || inBoard.GetLength(1) != 3)
                {
                    throw new GameSpecificationException("Invalid board specification in TicTacToeState.");
                }
                else
                {
                    Board = new string[3, 3];
                    for(var i =0; i < 3; i++)
                        for(var j = 0; j < 3; j++)
                        {
                            Board[i, j] = inBoard[i, j];
                        }
                    player = ValidatePlayer(playerString);
                }
            }

            public override string ToString()
            {
                string r = "";
                for(var i = 0; i < 3; i++)
                {
                    for(var j = 0; j < 2; j++)
                    {
                        r += Board[i, j] + "|";
                    }
                    r += Board[i, 2] + "\n";
                    if(i < 2)
                        r += "-----";
                    r += "\n";
                }
                return r;
            }

            public string GetCurrentPlayer()
            {
                return player.ToString();
            }

            public bool Equals(IGameState otherInterface)
            {
                var other = (TicTacToeState)otherInterface;

                if (other.player != player)
                    return false;

                for (var i = 0; i < 3; i++)
                    for (var j = 0; j < 3; j++)
                        if (Board[i, j] != other.Board[i, j])
                            return false;

                return true;
            }
        }

        private class TicTacToeAction : Action
        {
            public TicTacToeAction(GameAction act) : base(act)
            {

            }

            public override bool Equals(Action other)
            {
                throw new NotImplementedException();
            }
        }

        Dictionary<int, int> pointDict;

        List<int[]> GoalCheckCoords;
        List<int[]> PossiblePositions;

        public TicTacToe()
        {
            pointDict = new Dictionary<int, int>()
            {
                {3,200},
                {2,10},
                {1,1},
                {0,0},
                {-1,-1},
                {-2,-10},
                {-3,-200}
            };

            GoalCheckCoords = new List<int[]>();
            GoalCheckCoords.Add(new int[] { 0, 0, 0, 2 });
            GoalCheckCoords.Add(new int[] { 1, 0, 1, 2 });
            GoalCheckCoords.Add(new int[] { 2, 0, 2, 2 });
            GoalCheckCoords.Add(new int[] { 0, 0, 2, 0 });
            GoalCheckCoords.Add(new int[] { 0, 1, 2, 1 });
            GoalCheckCoords.Add(new int[] { 0, 2, 2, 2 });
            GoalCheckCoords.Add(new int[] { 0, 0, 2, 2 });
            GoalCheckCoords.Add(new int[] { 2, 0, 0, 2 });

            PossiblePositions = new List<int[]>();
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    PossiblePositions.Add(new int[] {i,j});
        }

        public override List<Action> AvailableActions(IGameState inState)
        {
            var state = tryCastGameState(inState);
            List<Action> actions = new List<Action>();

            string markerToPlace = "x";
            string nextPlayer = "o";
            if (state.player == "o")
            {
                markerToPlace = "o";
                nextPlayer = "x";
            }

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    if (state.Board[i, j] == " ")
                    {
                        int posRow = i;
                        int posCol = j;
                        Action.GameAction a = (inS) => 
                        {
                            var s = tryCastGameState(inS);
                            TicTacToeState nextS = new TicTacToeState(s.Board,nextPlayer);
                            nextS.Board[posRow, posCol] = markerToPlace;
                            return nextS;
                        };
                        actions.Add(new TicTacToeAction(a));
                    }
                }
            }

            return actions;
        }

        public override bool TerminalStateCheck(IGameState inState)
        {
            var state = tryCastGameState(inState);
            int nMarkers = 9;
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    if (state.Board[i, j] == " ")
                        nMarkers--;
            if (nMarkers == 9)
                return true;

            int vX = 0;
            int vO = 0;
            foreach(int[] coords in GoalCheckCoords)
            {
                vX = Math.Max(vX, getNumberOfInLine(state.Board,"x",coords[0],coords[1],coords[2],coords[3]));
                vO = Math.Max(vO, getNumberOfInLine(state.Board, "o", coords[0], coords[1], coords[2], coords[3]));
            }
            if (vX == 3 || vO == 3)
                return true;

            return false;
        }

        public override float Heuristic(IGameState inState)
        {
            TicTacToeState state;
            if (inState is TicTacToeState)
                state = (TicTacToeState)inState;
            else
                throw new GameSpecificationException("Cannot cast to TicTacToeState");

            float value = 0;
            string marker = state.player;
            foreach (int[] coords in GoalCheckCoords)
            {
                int n = 0;
                n = getNumberOfInLine(state.Board, marker, coords[0], coords[1], coords[2], coords[3]);
                if(marker == "x")
                    n -= getNumberOfInLine(state.Board, "o", coords[0], coords[1], coords[2], coords[3]);
                else
                    n -= getNumberOfInLine(state.Board, "x", coords[0], coords[1], coords[2], coords[3]);
                value += pointDict[n];
            }

            return value;
        }

        int getNumberOfInLine(string[,] board,string marker,int i0, int j0, int i1, int j1)
        {
            int n = 0;
            int sgnj = Math.Sign(j1 - j0);
            int sgni = Math.Sign(i1 - i0);
            for(var dr = 0; dr < 3; dr++)
            {
                string c = board[i0 + sgni * dr, j0 + sgnj * dr];
                if (c == marker)
                    n++;
            }

            return n;
        }

        public override IGameState NewGame()
        {
            string[] empty = new string[9];
            for (var i = 0; i < 9; i++)
                empty[i] = " ";
            return new TicTacToeState(empty, "x");
        }

        static TicTacToeState tryCastGameState(IGameState inState)
        {
            TicTacToeState state;
            if (inState is TicTacToeState)
                state = (TicTacToeState)inState;
            else
                throw new GameSpecificationException(string.Format("Cannot cast {0} to TicTacToeState",inState.GetType()));

            return state;
        }

        public override IGameState MakeMove(IGameState current, string move)
        {
            var currentTTT = tryCastGameState(current);
            string nextPlayer = "x";
            string marker = "o";
            if (currentTTT.player == "x")
            {
                nextPlayer = "o";
                marker = "x";
            }

            TicTacToeState nextState = new TicTacToeState(currentTTT.Board, nextPlayer);
            int placePosRow = int.Parse(move.Substring(0, 1));
            int placePosCol = int.Parse(move.Substring(1, 1));
            if(nextState.Board[placePosRow,placePosCol] == " ")
            {
                nextState.Board[placePosRow, placePosCol] = marker;
            }
            else
            {
                throw new GameSpecificationException(string.Format("Cannot place {0} at {1},{2}",
                    marker,placePosRow,placePosCol));
            }

            return nextState;
        }

        public override Action RandomMove(IGameState inState)
        {
            TicTacToeState state = tryCastGameState(inState);
            string markerToPlace = "x";
            string nextPlayer = "o";
            if (state.player == "o")
            {
                markerToPlace = "o";
                nextPlayer = "x";
            }
            while (true)
            {
                int[] pos = PossiblePositions[Utils.RandInt(0, PossiblePositions.Count)];
                if (state.Board[pos[0], pos[1]] == " ")
                {
                    int posRow = pos[0];
                    int posCol = pos[1];
                    return new TicTacToeAction((inS) =>
                    {
                        var s = tryCastGameState(inS);
                        TicTacToeState nextS = new TicTacToeState(s.Board, nextPlayer);
                        nextS.Board[posRow, posCol] = markerToPlace;
                        return nextS;
                    });
                }
            }
        }

        public override IGameState StateFromString(string input)
        {
            string[] board = new string[9];
            for (int i = 0; i < 9; i++)
                board[i] = input.Substring(i, 1);
            return new TicTacToeState(board, input.Substring(input.Length - 1, 1));
        }
    }
}
