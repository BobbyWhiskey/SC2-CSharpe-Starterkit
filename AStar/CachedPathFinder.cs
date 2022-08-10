using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using AStar.Options;

namespace AStar;

public class CachedPathFinder : IFindAPath
{
    private Dictionary<PathEntry, Point[]> PathCache = new Dictionary<PathEntry, Point[]>();
    private readonly PathFinder _pathFinder;

    public record PathEntry(Point Start, Point End);

    public CachedPathFinder(WorldGrid worldGrid, PathFinderOptions pathFinderOptions = null)
    {
        this._pathFinder = new PathFinder(worldGrid, pathFinderOptions);
    }

    public Point[] FindPath(Vector3 start, Vector3 end)
    {
        var pathEntry = new PathEntry(new Point((int)start.X, (int)start.Y), new Point((int)end.X, (int)end.Y));
        if (PathCache.TryGetValue(pathEntry, out var cachedPath))
        {
            return cachedPath;
        }
        var path = _pathFinder.FindPath(start, end);
        PathCache.Add(pathEntry, path);
        
        return path;
    }
}