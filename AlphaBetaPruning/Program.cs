using System;
using System.IO;
using AlphaBetaPruning.AILearner;
namespace AlphaBetaPruning
{
    class Program
    {
        static void PlayTTT(Game game, bool aiGoesFirst = false)
        {
            Minimax ai = new Minimax(game);
            IGameState state = game.NewGame();
            bool humanMove = !aiGoesFirst;
            Console.WriteLine(state.ToString());
            do
            {
                if (humanMove)
                {
                    string input = Console.ReadLine();
                    if (input == "q")
                        break;
                    state = game.MakeMove(state, input);
                }
                else
                {
                    state = ai.FindBestMove(state,2).Act(state);
                }
                Console.WriteLine(state.ToString());
                humanMove = !humanMove;
            } while (!game.TerminalStateCheck(state));
        }

        static void PlayGomoku(Game game, bool aiGoesFirst = false)
        {
            Minimax ai = new Minimax(game);
            IGameState state = game.NewGame();
            bool humanMove = !aiGoesFirst;
            Console.WriteLine(state.ToString());
            do
            {
                if (humanMove)
                {
                    string input = Console.ReadLine();
                    if (input == "q")
                        break;
                    state = game.MakeMove(state, input);
                }
                else
                {
                    state = ai.FindBestMove(state,1).Act(state);
                    Console.WriteLine(((Gomoku)game).HeuristicCounter);
                }
                Console.WriteLine(state.ToString());
                Console.WriteLine(string.Format("{0}: For {1}",game.Heuristic(state),state.GetCurrentPlayer()));

                humanMove = !humanMove;
            } while (!game.TerminalStateCheck(state));
        }

        static void Main(string[] args)
        {
            //PlayTTT(new TicTacToe(),true);
            //PlayGomoku(new Gomoku(),false);

            Game game = new Connect4();
            MCUTC mcAI = new MCUTC(game);
            Minimax mmAI = new Minimax(game);
            IGameState state = game.NewGame();
            Console.WriteLine(state.ToString());
            //Game.GameAction act = mcAI.FindBestMove(state, maxIterations: 10000);
            //Console.WriteLine(act(state).ToString());
            //StreamWriter fout = new StreamWriter("C:\\Users\\Kevin\\Desktop\\MCUTCTree.xml");
            //mcAI.DumpTree(fout);
            //fout.Close();
            string connect4FP = "Connect4DB.txt";
            mmAI.SetLearner(new Connect4DecisionTree());
            bool prettyPrint = false;
            while (!game.TerminalStateCheck(state))
            {
                Action act = mmAI.FindBestMove(state, 6);
                state = act.Act(state);
                Console.WriteLine(state.ToString());

                state = game.MakeMove(state, Console.ReadLine());

                if (prettyPrint)
                    state.PrettyPrintToConsole();
                else
                    Console.WriteLine(state.ToString());
            }

        }
    }
}
