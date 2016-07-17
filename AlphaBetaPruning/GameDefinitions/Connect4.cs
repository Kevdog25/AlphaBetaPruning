using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning
{
    class Connect4 : Game
    {
        public enum Player { Red=1, Black=2}

        public class Connect4State : IGameState
        {
            public int[,] board = new int[7, 7];
            public Player toMove { get; private set; }
            public int NumberOfMoves { get; private set; }

            public Connect4State()
            {
                for (var i = 0; i < 7; i++)
                    for (var j = 0; j < 7; j++)
                        board[i, j] = 0;
                toMove = Player.Red;
            }

            public Connect4State(Connect4State inS)
            {
                for (var i = 0; i < 7; i++)
                    for (var j = 0; j < 7; j++)
                        board[i, j] = inS.board[i,j];
                toMove = inS.toMove;
            }

            public void Move(int col)
            {
                if(board[col,0] < 6)
                {
                    board[col, 0]++;
                    board[col, board[col, 0]] = (int)toMove;
                    if (toMove == Player.Red)
                        toMove = Player.Black;
                    else
                        toMove = Player.Red;
                    NumberOfMoves++;
                }
                else
                {
                    throw new GameSpecificationException("Cannot move to a full column.");
                }
            }

            public bool Equals(IGameState other)
            {
                Connect4State localOther = other as Connect4State;
                if (localOther == null) return false;
                if (localOther.toMove != toMove) return false;
                for (var i = 0; i < 7; i++)
                    for (var j = 0; j < 7; j++)
                        if(board[i, j] != localOther.board[i,j]) return false;
                return true;
            }

            public override string ToString()
            {
                string s = "";
                for (var j = 6; j > 0; j--)
                {
                    s += "|";
                    for (var i = 0; i < 7; i++)
                    {
                        if(board[i,j]==0)
                            s += "-|";
                        else
                            s += board[i, j] + "|";
                    }
                    s += "\n";
                }
                s += " ";
                for (int i = 0; i < 7; i++)
                    s += i + " ";
                return s;            
            }

            public string GetCurrentPlayer()
            {
                throw new NotImplementedException();
            }

            public void PrettyPrintToConsole()
            {
                ConsoleColor oldBckg = Console.BackgroundColor;
                ConsoleColor oldFor = Console.ForegroundColor;
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                
                for (var j = 6; j > 0; j--)
                {
                    Console.Write("|");
                    for (var i = 0; i < 7; i++)
                    {
                        if (board[i, j] == 0)
                            Console.Write("-|");
                        else
                        {
                            if(Enum.GetName(typeof(Player),board[i,j]).Equals("Red"))
                                ConsoleConfig.PrintInColor(board[i, j].ToString(),ConsoleColor.Red);
                            else
                                ConsoleConfig.PrintInColor(board[i, j].ToString(), ConsoleColor.Black);
                            Console.Write("|");
                        }
                    }
                    Console.WriteLine();
                }
                Console.Write(" ");
                for (int i = 0; i < 7; i++)
                    Console.Write(i + " ");

                Console.BackgroundColor = oldBckg;
                Console.ForegroundColor = oldFor;
            }
        }

        public class Connect4Action : Action
        {
            public Player MovingPlayer { get; private set; }
            public int Column { get; private set; }

            public Connect4Action(GameAction act, int col, Player moving) : base(act)
            {
                MovingPlayer = moving;
                Column = col;
            }

            public override int CompareTo(Action obj)
            {
                Connect4Action local = cast(obj);
                int colDiff = Column - local.Column;
                if (colDiff != 0)
                    return colDiff;
                return ((int)MovingPlayer - (int)local.MovingPlayer);
            }

            public override bool Equals(Action other)
            {
                Connect4Action local = cast(other);
                return (local.MovingPlayer == MovingPlayer) && (local.Column == Column);
            }

            private Connect4Action cast(Action other)
            {
                Connect4Action a = other as Connect4Action;
                if (a == null)
                    throw new GameSpecificationException(string.Format("Cannot cast action {0} to Connect4Action", other.GetType()));
                return a;
            }
        }

        List<int[]> directions = new List<int[]>();

        public Connect4()
        {
            directions.Add(new int[] { 0, 1 });
            directions.Add(new int[] { 1, 1 });
            directions.Add(new int[] { 1, 0 });
            directions.Add(new int[] { 1, -1 });
        }

        public override List<Action> AvailableActions(IGameState inState)
        {
            Connect4State state = tryCastGameState(inState);
            List<Action> actions = new List<Action>();

            for (int i = 0; i < 7; i++)
            {
                if (state.board[i, 0] < 6)
                {
                    int col = i;
                    actions.Add(new Connect4Action(inS =>
                    {
                        Connect4State s = new Connect4State(tryCastGameState(inS));
                        s.Move(col);
                        return s;
                    },col,state.toMove));
                }
            }

            return actions;
        }

        public override float Heuristic(IGameState inState)
        {
            Connect4State state = tryCastGameState(inState);
            int points = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = 1; j < 6; j++)
                {
                    int p = state.board[i, j];
                    if (p != 0)
                    {
                        foreach (int[] dir in directions)
                        {
                            int length = 1;
                            int r = 1;
                            while (isInRange(i + r * dir[0], 0, 7) && isInRange(j + r * dir[1], 1, 7)
                                && state.board[i + r * dir[0], j + r * dir[1]] == p)
                            {
                                length++;
                                r++;
                            }
                            r = -1;
                            while (isInRange(i + r * dir[0], 0, 7) && isInRange(j + r * dir[1], 1, 7)
                                && state.board[i + r * dir[0], j + r * dir[1]] == p)
                            {
                                length++;
                                r--;
                            }
                            if (length == 4)
                                length = 1000;
                            if (p == (int)state.toMove)
                                points += length;
                            else
                                points -= length;
                        }
                    }

                }
            }

            return points;
        }

        public override IGameState NewGame()
        {
            return new Connect4State();
        }

        public override IGameState MakeMove(IGameState inS, string col)
        {
            Connect4State s = tryCastGameState(inS);
            s.Move(int.Parse(col));
            return s;
        }

        public override bool TerminalStateCheck(IGameState inState)
        {
            Connect4State state = tryCastGameState(inState);
            for(int i = 0; i < 7; i++)
            {
                for (int j = 1; j < 6; j++)
                {
                    int p = state.board[i, j];
                    if(p != 0)
                    {
                        foreach(int[] dir in directions)
                        {
                            int length = 1;
                            int r = 1;
                            while(isInRange(i+r*dir[0],0,7) && isInRange(j + r * dir[1], 1,7)
                                && state.board[i + r * dir[0], j + r * dir[1]] == p )
                            {
                                length++;
                                r++;
                            }
                            r = -1;
                            while (isInRange(i + r * dir[0], 0, 7) && isInRange(j + r * dir[1], 1, 7)
                                && state.board[i + r * dir[0], j + r * dir[1]] == p)
                            {
                                length++;
                                r--;
                            }
                            if (length >= 4) return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool isInRange(int x, int l, int r)
        {
            return (x >= l) && (x < r);
        }

        private static Connect4State tryCastGameState(IGameState inState)
        {
            Connect4State state = inState as Connect4State;
            if (state == null)
                throw new GameSpecificationException(string.Format("Cannot cast {0} to TicTacToeState", inState.GetType()));

            return state;
        }
    }
}
