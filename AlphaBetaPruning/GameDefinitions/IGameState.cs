
namespace AlphaBetaPruning.GameDefinitions
{
    interface IGameState
    {
        string ToString();

        bool Equals(IGameState other);

        string GetCurrentPlayer();

        void PrettyPrintToConsole();
    }
}
