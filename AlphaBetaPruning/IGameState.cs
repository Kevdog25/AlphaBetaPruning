
namespace AlphaBetaPruning
{
    interface IGameState
    {
        string ToString();

        bool Equals(IGameState other);

        string GetCurrentPlayer();
    }
}
