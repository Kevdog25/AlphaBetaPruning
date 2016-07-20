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
            Minimax mmAI2 = new Minimax(game);
            //Game.GameAction act = mcAI.FindBestMove(state, maxIterations: 10000);
            //Console.WriteLine(act(state).ToString());
            //StreamWriter fout = new StreamWriter("C:\\Users\\Kevin\\Desktop\\MCUTCTree.xml");
            //mcAI.DumpTree(fout);
            //fout.Close();
            string connect4FP = "Connect4DB.txt";
            DecisionTree decisionTree = new Connect4DecisionTree(connect4FP);
            mmAI.SetLearner(decisionTree,true);
            bool prettyPrint = false;

            string x = "y";

            while (x.Equals("y") || x.Equals("r"))
            {
                if(x.Equals("r"))
                    decisionTree.ResolveBuffer();
                IGameState state = game.NewGame();
                Console.WriteLine(state.ToString());
                bool player = true;
                while (!game.TerminalStateCheck(state))
                {
                    Action act;
                    if (player)
                        act = mmAI.FindBestMove(state, 7);
                    else
                        act = mmAI2.FindBestMove(state, 7);
                    state = act.Act(state);
                    player = !player;
                    if (prettyPrint)
                        state.PrettyPrintToConsole();
                    else
                        Console.WriteLine(state.ToString());
                }
                if (!player)
                    Console.WriteLine("Player 1 won!");
                else
                    Console.WriteLine("Player 2 won!");
                Console.WriteLine("Play again?");
                string xml = decisionTree.ToXML();
                x = Console.ReadLine();
            }

            decisionTree.SaveToDatabase(connect4FP);
        }
    }
}
