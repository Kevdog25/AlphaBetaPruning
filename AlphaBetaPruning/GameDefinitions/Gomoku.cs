using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning
{
    class Gomoku : Game
    {
        List<int[]> Directions;
        public int HeuristicCounter = 0;
        List<int[]> PossiblePositions;
        /* Game board definition */
        private class GomokuState : IGameState
        {
            public enum Player : int {X = 1, Blank = 0, O = -1};
            public Player player;
            public Player[,] Board;
            public static readonly int boardSize = 15;

            private static Player ValidatePlayer(Player playerNumber)
            {
                if (Enum.IsDefined(typeof(Player),playerNumber))
                {
                    return playerNumber;
                }
                else
                {
                    throw new GameSpecificationException("Invalid player specification: " + playerNumber);
                }
            }

            public GomokuState(Player[] inBoard, Player playerNum)
            {
                Board = new Player[boardSize, boardSize];
                if (inBoard.Length != boardSize * boardSize)
                {
                    throw new GameSpecificationException("Invalid board specification in GomokuState.");
                }
                else
                {
                    for (var i = 0; i < boardSize; i++)
                    {
                        for (var j = 0; j < boardSize; j++)
                        {
                            Board[i, j] = inBoard[i * boardSize + j];
                        }
                    }
                }
                player = ValidatePlayer(playerNum);
            }

            public GomokuState(Player[,] inBoard, Player playerNum)
            {
                if (inBoard.GetLength(0) != boardSize || inBoard.GetLength(1) != boardSize)
                {
                    throw new GameSpecificationException("Invalid board specification in TicTacToeState.");
                }
                else
                {
                    Board = new Player[boardSize, boardSize];
                    for (var i = 0; i < boardSize; i++)
                        for (var j = 0; j < boardSize; j++)
                        {
                            Board[i, j] = inBoard[i, j];
                        }
                    player = ValidatePlayer(playerNum);
                }
            }

            public override string ToString()
            {
                string r = "";
                for (var i = 0; i < boardSize; i++)
                {
                    for (var j = 0; j < boardSize-1; j++)
                    {
                        r += pieceToString(Board[i, j]) + "|";
                    }
                    r += pieceToString(Board[i, boardSize-1]) + "\n";
                    if (i < boardSize-1)
                        r += "-----------------------------";
                    r += "\n";
                }
                return r;
            }

            static string pieceToString(Player p)
            {
                switch (p)
                {
                    case Player.Blank:
                        return " ";
                    case Player.O:
                        return "O";
                    case Player.X:
                        return "X";
                    default:
                        return "";
                }
            }

            public string GetCurrentPlayer()
            {
                return player.ToString();
            }

            public Player OtherPlayer(Player current)
            {
                if (current == Player.X)
                    return Player.O;
                return Player.X;
            }

            public bool Equals(IGameState otherInterface)
            {
                var other = (GomokuState)otherInterface;

                if (other.player != player)
                    return false;

                for (var i = 0; i < boardSize; i++)
                    for (var j = 0; j < boardSize; j++)
                        if (Board[i, j] != other.Board[i, j])
                            return false;

                return true;
            }

            public void PrettyPrintToConsole()
            {
                throw new NotImplementedException();
            }
        }
        
        private class GomokuAction : Action
        {
            public GomokuAction(GameAction act) : base(act)
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

        public Gomoku()
        {
            Directions = new List<int[]>();
            Directions.Add(new int[] { 0, -1 });
            Directions.Add(new int[] { -1, -1 });
            Directions.Add(new int[] { -1, 0, });
            Directions.Add(new int[] { -1, 1 });

            PossiblePositions = new List<int[]>();
            for (var i = 0; i < GomokuState.boardSize; i++)
                for (var j = 0; j < GomokuState.boardSize; j++)
                    PossiblePositions.Add(new int[] { i, j });
        }

        public override List<Action> AvailableActions(IGameState inState)
        {
            var state = tryCastGameState(inState);
            List<Action> actions = new List<Action>();

            int boardSize = GomokuState.boardSize;
            GomokuState.Player markerToPlace = state.player;
            GomokuState.Player nextPlayer = state.OtherPlayer(markerToPlace);
            GomokuState.Player blank = GomokuState.Player.Blank;

            for (var i = 0; i < boardSize; i++)
            {
                for (var j = 0; j < boardSize; j++)
                {
                    if (state.Board[i, j] == blank)
                    {
                        int posRow = i;
                        int posCol = j;
                        Action.GameAction a = (inS) =>
                        {
                            var s = tryCastGameState(inS);
                            GomokuState nextS = new GomokuState(s.Board, nextPlayer);
                            nextS.Board[posRow, posCol] = markerToPlace;
                            return nextS;
                        };
                        actions.Add(new GomokuAction(a));
                    }
                }
            }

            return actions;
        }

        public override bool TerminalStateCheck(IGameState inState)
        {
            var state = tryCastGameState(inState);
            int nMarkers = GomokuState.boardSize * GomokuState.boardSize;
            bool isFull = true;
            GomokuState.Player blank = GomokuState.Player.Blank;
            for (var i = 0; i < GomokuState.boardSize; i++)
            {
                for (var j = 0; j < GomokuState.boardSize; j++)
                {

                    // Keep tabs on whether it is full or not.
                    if (state.Board[i, j] == blank)
                        isFull = false;
                    else if(state.Board[i,j] == GomokuState.Player.X)
                    {
                        List<List<GomokuState.Player>> rays = FindContiguousRays(state.Board, i, j, GomokuState.Player.X);

                        // Check to see if there are 5 in a row.
                        for (var k = 0; k < rays.Count; k++)
                        {
                            int nX = 0;
                            for (var n = 0; n < rays[k].Count; n++)
                            {
                                if (rays[k][n] == GomokuState.Player.X)
                                    nX++;
                                else
                                    nX = 0;
                            }
                            if (nX >= 5)
                                return true;
                        }
                    }
                    else if (state.Board[i, j] == GomokuState.Player.O)
                    {
                        List<List<GomokuState.Player>> rays = FindContiguousRays(state.Board, i, j, GomokuState.Player.O);

                        // Check to see if there are 5 in a row.
                        for (var k = 0; k < rays.Count; k++)
                        {
                            int nO = 0;
                            for (var n = 0; n < rays[k].Count; n++)
                            {
                                if (rays[k][n] == GomokuState.Player.O)
                                    nO++;
                                else
                                    nO = 0;
                            }
                            if (nO >= 5)
                                return true;
                        }
                    }
                }
            }

            return isFull;
        }
        
        public override float Heuristic(IGameState inState)
        {
            HeuristicCounter++;
            GomokuState state = tryCastGameState(inState);
            GomokuState.Player blank = GomokuState.Player.Blank;
            float value = 0;
            foreach(GomokuState.Player p in Enum.GetValues(typeof(GomokuState.Player)))
            {
                // Do not evaluate the heuristic for player "blank"
                if (p == blank)
                    continue;
                float playerValue = 0;
                int minToWin = 5;
                int maxMinToStop = 0;
                for (var i = 0; i < GomokuState.boardSize; i++)
                {
                    for (var j = 0; j < GomokuState.boardSize; j++)
                    {
                        List<List<GomokuState.Player>> rays = FindContiguousRays(state.Board, i, j, p);
                        for(var n = 0; n < rays.Count; n++)
                        {
                            List<GomokuState.Player> ray = rays[n];
                            minToWin = Math.Min(NumberOfMovesToWin(ray),minToWin);
                            maxMinToStop += NumberOfMovesToStop(ray);
                        }
                    }
                }

                if (minToWin == 0)
                    playerValue = float.MaxValue;
                else
                    playerValue = (maxMinToStop - minToWin);

                if (p == state.player)
                    value += playerValue;
                else
                    value -= playerValue;
            }

            return value;
        }
        
        List<List<GomokuState.Player>> FindContiguousRays(GomokuState.Player[,] board,
            int i0, int j0, GomokuState.Player desiredPiece)
        {
            GomokuState.Player blank = GomokuState.Player.Blank;

            List<List<GomokuState.Player>> rays = new List<List<GomokuState.Player>>();
            GomokuState.Player piece = board[i0, j0];

            // If the start is the oposing piece, forget about it.
            if (piece != desiredPiece)
                return rays;

            for(var i = 0; i < Directions.Count; i++)
            {
                int[] dir = Directions[i];
                List<GomokuState.Player> ray = new List<GomokuState.Player>();
                int r = 0;

                // While the piece in that direction is the current type
                while (isInRange(i0+r*dir[0],j0+r*dir[1])
                    &&  (board[i0 + r * dir[0], j0 + r * dir[1]] == piece
                        || board[i0 + r * dir[0], j0 + r * dir[1]] == blank)
                    && r < 5)
                {
                    ray.Add(board[i0 + r * dir[0], j0 + r * dir[1]]);
                    r += 1;
                }

                // Look in the opposite direction.
                r = -1;
                while (isInRange(i0 + r * dir[0], j0 + r * dir[1])
                    && (board[i0 + r * dir[0], j0 + r * dir[1]] == piece
                        || board[i0 + r * dir[0], j0 + r * dir[1]] == blank)
                    && r > -5)
                {
                    ray.Insert(0,board[i0 + r * dir[0], j0 + r * dir[1]]);
                    r -= 1;
                }
                rays.Add(ray);
            }

            return rays;
        }
        
        static int NumberOfMovesToWin(List<GomokuState.Player> seq)
        {
            int nMoves = int.MaxValue;
            for(int i = 0; i < seq.Count-4; i++)
            {
                int nBlanks = 0;
                for(int j = i; j <= i+4; j++)
                {
                    if (seq[j] == GomokuState.Player.Blank)
                        nBlanks++;
                }
                nMoves = Math.Min(nBlanks, nMoves);
            }
            return nMoves;
        }

        static int NumberOfMovesToStop(List<GomokuState.Player> seq)
        {
            int nMoves = int.MaxValue;
            if (NumberOfMovesToWin(seq) > 5)
                return 0;

            for(int i = 0; i < seq.Count; i++)
            {
                if (seq[i] == GomokuState.Player.Blank)
                {
                    int movesToStop = 1;
                    if (i > 0)
                    {
                        List<GomokuState.Player> left = seq.GetRange(0, i);
                        movesToStop += NumberOfMovesToStop(left);
                    }
                    if (i < seq.Count - 1)
                    {
                        List<GomokuState.Player> right = seq.GetRange(i + 1, seq.Count - i - 1);
                        movesToStop += NumberOfMovesToStop(right);
                    }
                    nMoves = Math.Min(movesToStop, nMoves);
                }
            }

            return nMoves;
        }

        bool isInRange(params int[] x)
        {
            for(var i = 0; i < x.Length; i++)
                if (x[i] < 0 || x[i] >= GomokuState.boardSize)
                    return false;

            return true;
        }

        public override IGameState NewGame()
        {
            int size = GomokuState.boardSize * GomokuState.boardSize;
            GomokuState.Player[] empty = new GomokuState.Player[size];
            for (var i = 0; i < size; i++)
                empty[i] = GomokuState.Player.Blank;
            return new GomokuState(empty, GomokuState.Player.X);
        }

        static GomokuState tryCastGameState(IGameState inState)
        {
            GomokuState state;
            if (inState is GomokuState)
                state = (GomokuState)inState;
            else
                throw new GameSpecificationException(string.Format("Cannot cast {0} to TicTacToeState", inState.GetType()));

            return state;
        }

        public override IGameState MakeMove(IGameState current, string move)
        {
            var currentGomoku = tryCastGameState(current);
            GomokuState.Player blank = GomokuState.Player.Blank;
            GomokuState.Player marker = currentGomoku.player;
            GomokuState.Player nextPlayer = currentGomoku.OtherPlayer(marker);

            GomokuState nextState = new GomokuState(currentGomoku.Board, nextPlayer);
            int placePosRow = int.Parse(move.Substring(0, 1));
            int placePosCol = int.Parse(move.Substring(1, 1));
            if (nextState.Board[placePosRow, placePosCol] == blank)
            {
                nextState.Board[placePosRow, placePosCol] = marker;
            }
            else
            {
                throw new GameSpecificationException(string.Format("Cannot place {0} at {1},{2}. Piece {3} is in the way.",
                    marker, placePosRow, placePosCol, nextState.Board[placePosRow, placePosCol]));
            }

            return nextState;
        }
        
        public override Action RandomMove(IGameState inState)
        {
            GomokuState state = tryCastGameState(inState);
            GomokuState.Player markerToPlace = state.player;
            GomokuState.Player nextPlayer = state.OtherPlayer(markerToPlace);
            while (true)
            {
                int[] pos = PossiblePositions[Utils.RandInt(0, PossiblePositions.Count)];
                if (state.Board[pos[0], pos[1]] == GomokuState.Player.Blank)
                {
                    int posRow = pos[0];
                    int posCol = pos[1];
                    return new GomokuAction((inS) =>
                    {
                        var s = tryCastGameState(inS);
                        GomokuState nextS = new GomokuState(s.Board, nextPlayer);
                        nextS.Board[posRow, posCol] = markerToPlace;
                        return nextS;
                    });
                }
            }
        }
    }
}
