using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using AlphaBetaPruning.AILearner;
using AlphaBetaPruning.AIDriver;
using AlphaBetaPruning.AIHeurisitc;

using AlphaBetaPruning.GameDefinitions;
using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning
{
    class Program
    {
        static ARTTree<Connect4ART> ARTHeuristic;
        struct Profile
        {
            public float WinRate1;
            public float WinRate2;

            public override string ToString()
            {
                return string.Format("Win rates:\n  1:{0}\n  2:{1}",WinRate1,WinRate2);
            }
        }

        /// <summary>
        /// Plays out the specified number of games with the given AIs. Alternates which is playing which side.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="nGames"></param>
        /// <param name="AI1"></param>
        /// <param name="AI2"></param>
        /// <returns>Number of games won by AI1</returns>
        static Profile PlayoutGames(Game game, int nGames, IDecisionDriver AI1, IDecisionDriver AI2, bool verbose = false)
        {
            int p1Won = 0;
            Profile prof = new Profile();

            for (int i = 0; i < nGames; i++)
            {
                IGameState state = game.NewGame();
                List<Action> transcript = new List<Action>();
                bool p1ToPlay = true;
                while (!game.TerminalStateCheck(state))
                {
                    Action move;
                    if (p1ToPlay)
                        move = AI1.FindBestMove(state);
                    else
                        move = AI2.FindBestMove(state);

                    transcript.Add(move);

                    state = move.Act(state);
                    if (verbose)
                        Console.WriteLine(state.ToString());
                    p1ToPlay = !p1ToPlay;
                }

                int winner;
                if (p1ToPlay)
                    winner = (int)Connect4.Player.Black;
                else
                    winner = (int)Connect4.Player.Red;
                ARTHeuristic.Learn(game.NewGame(), transcript, winner);

                if (p1ToPlay != (i % 2 == 0))
                    p1Won++;

                IDecisionDriver temp = AI1;
                AI1 = AI2;
                AI2 = temp;
            }

            prof.WinRate1 = (float)p1Won / nGames;
            prof.WinRate2 = 1 - prof.WinRate1;

            return prof;
        }

        static float ProfileMoveTime(Game game, IDecisionDriver ai, int nGames = 20)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int nMoves = 0;
            for(var i = 0; i < nGames; i++)
            {
                IGameState state = game.NewGame();
                while (!game.TerminalStateCheck(state))
                {
                    state = ai.FindBestMove(state).Act(state);
                    nMoves++;
                }
            }
            sw.Stop();

            return (float)sw.ElapsedMilliseconds / (nMoves * 1000);
        }

        static void Main(string[] args)
        {
            ARTHeuristic = new ARTTree<Connect4ART>(new List<float>() {0.2f,0.3f,0.4f,0.5f});
            ARTTree<Connect4ART> randomOne = new ARTTree<Connect4ART>(new List<float>() { 0 });

            // Setup the AI
            Game game = new Connect4();
            NegaScout mmAI = new NegaScout(game, 1f, randomOne, maxDepth: 2);
            NegaScout nsAI = new NegaScout(game, 1f, ARTHeuristic, maxDepth : 2);
            //string connect4FP = "..\\..\\Ignored\\Connect4DB.txt";
            //DecisionTree decisionTree = new Connect4DecisionTree(connect4FP);
            //mmAI.SetLearner(decisionTree);

            // Test the AIs
            int nGames = 200;
            ARTHeuristic.Load("ARTDB.txt");
            Console.WriteLine(PlayoutGames(game, nGames, mmAI, nsAI,true));
            ARTHeuristic.Save("ARTDB.txt");
            //Console.WriteLine(string.Format("Time per move: {0}", ProfileMoveTime(game, mmAI,nGames)));
            //Console.WriteLine(string.Format("Time per move: {0}", ProfileMoveTime(game, nsAI, nGames)));
            //decisionTree.ResolveBuffer();
            //decisionTree.SaveToDatabase(connect4FP);
        }
    }
}
