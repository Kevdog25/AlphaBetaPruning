using System;
using System.Collections.Generic;

namespace AlphaBetaPruning
{
    class Checkers : Game
    {
        private struct CheckersState : IGameState
        {
            public static readonly List<string> Players = new List<string>(){"red","black"};
            public string[,] Board;
            public string player;
            public int NRed;
            public int NBlack;
            public List<Checker> Pieces;

            /* Defining the Checker class to use board pieces. */
            public struct Checker
            {
                public int row;
                public int col;
                public int direction;
                public string type;
                public string color;

                public Checker(int r, int c,string v)
                {
                    row = r;
                    col = c;
                    if (v == "r" || v == "K")
                    {
                        direction = 1;
                        color = "r";
                    }
                    else if (v == "b" || v == "k")
                    {
                        direction = -1;
                        color = "b";
                    }
                    else
                        throw new GameSpecificationException("Cannot set checker value to " + v);
                    type = v;
                }

                public int[] ForwardLeft(int d)
                {
                    return new int[] {row - d * direction,col - d * direction };
                }

                public int[] ForwardRight(int d)
                {
                    return new int[] { row - d * direction, col + d * direction };
                }

                public int[] BackLeft(int d)
                {
                    return new int[] { row + d * direction, col + d * direction };
                }

                public int[] BackRight(int d)
                {
                    return new int[] { row + d * direction, col - d * direction };
                }
                
            }

            /* Constructors */
            public CheckersState(string[,] inBoard, string inPlayer)
            {
                if (inBoard.GetLength(0) != 8 || inBoard.GetLength(1) != 8)
                    throw new GameSpecificationException("Improper length for board to initialize CheckersBoard.");
                Pieces = new List<Checker>();
                Board = new string[8, 8];
                player = validatePlayer(inPlayer);
                NRed = 0;
                NBlack = 0;
                for (var i = 0; i < 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        string p = inBoard[i, j];
                        Board[i, j] = p;
                        if (p == "r" || p == "K")
                            NRed++;
                        else if (p == "b" || p == "k")
                            NBlack++;
                        if (p != " ")
                            Pieces.Add(new Checker(i,j,p));
                    }
                }
            }

            public CheckersState(string[] inBoard, string inPlayer)
            {
                if (inBoard.Length != 64)
                    throw new GameSpecificationException("Improper length for board to initialize CheckersBoard.");
                Pieces = new List<Checker>();
                Board = new string[8, 8];
                player = validatePlayer(inPlayer);
                NRed = 0;
                NBlack = 0;
                for (var i = 0; i < 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        string p = inBoard[i*8 + j];
                        Board[i, j] = p;
                        if (p == "r" || p == "K")
                            NRed++;
                        else if (p == "b" || p == "k")
                            NBlack++;
                        if (p != " ")
                            Pieces.Add(new Checker(i, j, p));
                    }
                }
            }

            public CheckersState(List<Checker> pieces, string inPlayer)
            {
                Pieces = new List<Checker>();
                player = validatePlayer(inPlayer);
                Board = new string[8, 8];
                NRed = 0;
                NBlack = 0;
                for (var i = 0; i < 8; i++)
                    for (var j = 0; j < 8; j++)
                        Board[i, j] = " ";
                foreach (Checker c in pieces)
                {
                    Board[c.row, c.col] = c.type;
                    if (c.type == "r" || c.type == "K")
                        NRed++;
                    if (c.type == "b" || c.type == "k")
                        NBlack++;
                    Pieces.Add(c);
                }
            }

            /* Other Methods */
            static string validatePlayer(string inPlayer)
            {
                string lower = inPlayer.ToLower();
                if (Players.Contains(lower))
                    return lower;
                else
                    throw new GameSpecificationException("Cannot set player to " + inPlayer);
            }

            public bool Equals(IGameState otherInterface)
            {
                var other = (CheckersState)otherInterface;

                if (other.player != player)
                    return false;

                for (var i = 0; i < 8; i++)
                    for (var j = 0; j < 8; j++)
                        if (Board[i, j] != other.Board[i, j])
                            return false;

                return true;
            }

            public void MovePiece(int row1,int col1,int row2,int col2)
            {
                Checker c = Pieces[0];
                int index = 1;
                while(c.row != row1 || c.col != col1)
                {
                    c = Pieces[index];
                    index++;
                }

                Board[row1, col1] = " ";
                Board[row2, col2] = c.type;
                c.row = row2;
                c.col = col2;
            }

            public void RemovePiece(int row, int col)
            {
                Checker c = Pieces[0];
                int index = 1;
                while (c.row != row || c.col != col)
                {
                    c = Pieces[index];
                    index++;
                }
                Pieces.Remove(c);
                Board[row, col] = " ";
            }

            public override string ToString()
            {
                string r = "";
                for(var i = 0; i < 8; i++)
                {
                    r += "|";
                    for(var j = 0; j < 8; j++)
                    {
                        r += Board[i, j] + "|";
                    }
                    r += "\n";
                }
                return r;
            }

            public string GetCurrentPlayer()
            {
                return player.ToString();
            }

            public void PrettyPrintToConsole()
            {
                throw new NotImplementedException();
            }
        }

        private class CheckersAction : Action
        {
            public CheckersAction(GameAction act) : base(act)
            {

            }

            public override int CompareTo(Action obj)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(Action other)
            {
                throw new NotImplementedException();
            }
        }

        public Checkers()
        {
        }

        public override IGameState NewGame()
        {
            string[,] board = new string[8, 8];
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 8; j++)
                    board[i, j] = " ";
            for (var i = 0; i < 3; i++)
                for (var j = i % 2; j < 8; j += 2)
                    board[i, j] = "b";
            for (var i = 5; i < 8; i++)
                for (var j = i % 2; j < 8; j += 2)
                    board[i, j] = "r";

            return new CheckersState(board, "red");
        }

        public override bool TerminalStateCheck(IGameState inState)
        {
            CheckersState state = tryCastGameState(inState);

            return Math.Abs(state.NBlack - state.NRed) <= 1;

            //int nPieces = 0;
            //for (var i = 0; i < 8; i++)
            //{
            //    for (var j = 0; j < 8; j++)
            //    {
            //        if (state.Board[i, j] != " ")
            //        {
            //            nPieces++;
            //            if (nPieces > 1)
            //                return false;
            //        }
            //    }
            //}
            //return true;
        }
        
        public override List<Action> AvailableActions(IGameState inState)
        {
            CheckersState state = tryCastGameState(inState);
            List<Action> actions = new List<Action>();

            foreach(CheckersState.Checker c in state.Pieces)
            {
                actions.AddRange(FindMoves(state,c));
            }

            return actions;
        }

        List<Action> FindMoves(IGameState inState, CheckersState.Checker c, bool fromCapture = false)
        {
            CheckersState state = tryCastGameState(inState);
            List<Action> actions = new List<Action>();
            if (fromCapture)
            {
                int[] moveDir = c.ForwardRight(2);
            }
            else
            {

            }

            return actions;
        }

        List<Action> TryCapture(IGameState inState, CheckersState.Checker c, int[] moveDir)
        {
            CheckersState state = tryCastGameState(inState);
            int spotCheck = tryMove(state, c.row + moveDir[0], c.col + moveDir[1], c.type);
            List<Action> actions = new List<Action>();
            if (spotCheck == 1)
            {
                List<Action> nextActions = new List<Action>();
                Action.GameAction aCap = (inS) =>
                {
                    var s = tryCastGameState(inS);
                    string nextPlayer = s.player;
                    CheckersState nextS = new CheckersState(s.Board, nextPlayer);
                    nextS.MovePiece(c.row, c.col, c.row + moveDir[0], c.col + moveDir[1]);
                    return nextS;
                };

                nextActions = FindMoves(aCap(state),
                    new CheckersState.Checker(c.row + moveDir[0], c.col + moveDir[1], c.type), true);

                actions.Add(new CheckersAction(aCap));
                for (var i = 0; i < nextActions.Count; i++)
                {
                    actions.Add(new CheckersAction(
                        (inS) =>
                        {
                            return nextActions[i].Act((aCap(inS)));
                        }));
                }
            }

            return actions;
        }

        public override float Heuristic(IGameState inState)
        {
            CheckersState state = tryCastGameState(inState);
            // This is a placeholder heuristic. Please correct for real use.
            if (state.player == "red")
                return state.NRed - state.NBlack;
            else
                return state.NBlack - state.NRed;
        }

        static CheckersState tryCastGameState(IGameState inState)
        {
            if (inState is CheckersState)
                return (CheckersState)inState;
            else
                throw new GameSpecificationException(string.Format("Cannot cast {0} to CheckersState",inState.GetType()));
        }

        static int tryMove(CheckersState state, int row, int col, string movingPiece)
        {
            if (row < 0 || col < 0 || row > 7 || col > 7)
                return -1;

            if (state.Board[row, col] == " ")
                return 1;

            if (movingPiece == "r" || movingPiece == "K")
                if (state.Board[row, col] == "r" || state.Board[row, col] == "K")
                    return -1;

            if (movingPiece == "b" || movingPiece == "k")
                if (state.Board[row, col] == "b" || state.Board[row, col] == "k")
                    return -1;

            if (movingPiece == "r" || movingPiece == "K")
                if (state.Board[row, col] == "b" || state.Board[row, col] == "k")
                    return 0;

            if (movingPiece == "b" || movingPiece == "k")
                if (state.Board[row, col] == "r" || state.Board[row, col] == "K")
                    return 0;

            throw new GameSpecificationException("Piece moving was not caught by available conditions.");
        }

    }
}
