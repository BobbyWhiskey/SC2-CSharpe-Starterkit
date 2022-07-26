using AStar.Collections.PathFinder;

namespace AStar.Heuristics
{
    public interface ICalculateHeuristic
    {
        int Calculate(Position source, Position destination);
    }
}