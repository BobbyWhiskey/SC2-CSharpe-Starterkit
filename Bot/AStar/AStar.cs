using System.Numerics;

namespace Bot.AStar;

public class Node
{
    // Change this depending on what the desired size is for each element in the grid
    public static int NODE_SIZE = 1;
    public float Cost;
    public float DistanceToTarget;

    public Node Parent;
    public Vector2 Position;
    public bool Walkable;
    public float Weight;

    public Node(Vector2 pos, bool walkable, float weight = 1)
    {
        Parent = null;
        Position = pos;
        DistanceToTarget = -1;
        Cost = 1;
        Weight = weight;
        Walkable = walkable;
    }

    public Vector2 Center
    {
        get
        {
            return new Vector2(Position.X + NODE_SIZE / 2, Position.Y + NODE_SIZE / 2);
        }
    }
    public float F
    {
        get
        {
            if (DistanceToTarget != -1 && Cost != -1)
            {
                return DistanceToTarget + Cost;
            }
            return -1;
        }
    }
}

public class Astar
{
    public List<List<Node>> Grid;

    public Astar(List<List<Node>> grid)
    {
        Grid = grid;
    }

    private int GridRows
    {
        get
        {
            return Grid[0].Count;
        }
    }
    private int GridCols
    {
        get
        {
            return Grid.Count;
        }
    }

    public IEnumerable<Vector2> FindPath(Vector2 Start, Vector2 End)
    {
        var start = new Node(new Vector2((int)(Start.X / Node.NODE_SIZE), (int)(Start.Y / Node.NODE_SIZE)), true);
        var end = new Node(new Vector2((int)(End.X / Node.NODE_SIZE), (int)(End.Y / Node.NODE_SIZE)), true);

        var Path = new Stack<Node>();
        var OpenList = new PriorityQueue<Node, float>();
        var ClosedList = new List<Node>();
        List<Node> adjacencies;
        var current = start;

        // add start node to Open List
        OpenList.Enqueue(start, start.F);

        while (OpenList.Count != 0 && !ClosedList.Exists(x => x.Position == end.Position))
        {
            current = OpenList.Dequeue();
            ClosedList.Add(current);
            adjacencies = GetAdjacentNodes(current);

            foreach (var n in adjacencies)
            {
                if (!ClosedList.Contains(n) && n.Walkable)
                {
                    var isFound = false;
                    foreach (var oLNode in OpenList.UnorderedItems)
                    {
                        if (oLNode.Element == n)
                        {
                            isFound = true;
                        }
                    }
                    if (!isFound)
                    {
                        n.Parent = current;
                        n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) + Math.Abs(n.Position.Y - end.Position.Y);
                        n.Cost = n.Weight + n.Parent.Cost;
                        OpenList.Enqueue(n, n.F);
                    }
                }
            }
        }

        // construct path, if end was not closed return null
        if (!ClosedList.Exists(x => x.Position == end.Position))
        {
            return null;
        }

        // if all good, return path
        var temp = ClosedList[ClosedList.IndexOf(current)];
        if (temp == null)
        {
            return null;
        }
        do
        {
            Path.Push(temp);
            temp = temp.Parent;
        } while (temp != start && temp != null);

        var pathList = Path.Select(n => new Vector2(n.Position.Y, n.Position.X));

        return pathList;
    }

    private List<Node> GetAdjacentNodes(Node n)
    {
        var temp = new List<Node>();

        var row = (int)n.Position.Y;
        var col = (int)n.Position.X;

        if (row + 1 < GridRows)
        {
            temp.Add(Grid[col][row + 1]);
        }
        if (row - 1 >= 0)
        {
            temp.Add(Grid[col][row - 1]);
        }
        if (col - 1 >= 0)
        {
            temp.Add(Grid[col - 1][row]);
        }
        if (col + 1 < GridCols)
        {
            temp.Add(Grid[col + 1][row]);
        }

        return temp;
    }
}



//
//
// public static Stack<MatrixNode> FindPath(bool[][] matrix, int fromX, int fromY, int toX, int toY)
// {
//     var endNode = FindPathInternal(matrix, fromX, fromY, toX, toY);
//     //looping through the Parent nodes until we get to the start node
//     var path = new Stack<MatrixNode>();
//
//     while (endNode.x != fromX || endNode.y != fromY)
//     {
//         path.Push(endNode);
//         endNode = endNode.parent;
//     }
//
//     path.Push(endNode);
//
//     return path;
// }
//
// private static MatrixNode FindPathInternal(bool[][] matrix, int fromX, int fromY, int toX, int toY)
// {
//     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//     // in this version an element in a matrix can move left/up/right/down in one step, two steps for a diagonal move.
//     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//     //the keys for greens and reds are x.ToString() + y.ToString() of the matrixNode 
//     var greens = new Dictionary<string, MatrixNode>(); //open 
//     var reds = new Dictionary<string, MatrixNode>(); //closed 
//
//     var startNode = new MatrixNode { x = fromX, y = fromY };
//     var key = startNode.x + startNode.x.ToString();
//     greens.Add(key, startNode);
//
//     var smallestGreen = () =>
//     {
//         var smallest = greens.ElementAt(0);
//
//         foreach (var item in greens)
//             if (item.Value.sum < smallest.Value.sum)
//                 smallest = item;
//             else if (item.Value.sum == smallest.Value.sum
//                      && item.Value.to < smallest.Value.to)
//                 smallest = item;
//
//         return smallest;
//     };
//
//
//     //add these values to current node's x and y values to get the left/up/right/bottom neighbors
//     var fourNeighbors = new List<KeyValuePair<int, int>>
//     {
//         new(-1, 0),
//         new(0, 1),
//         new(1, 0),
//         new(0, -1)
//     };
//
//     var maxX = matrix.GetLength(0);
//     if (maxX == 0)
//         return null;
//     var maxY = matrix[0].Length;
//
//     while (true)
//     {
//         if (greens.Count == 0)
//             return null;
//
//         var current = smallestGreen();
//         if (current.Value.x == toX && current.Value.y == toY)
//             return current.Value;
//
//         greens.Remove(current.Key);
//         reds.Add(current.Key, current.Value);
//
//         foreach (var plusXY in fourNeighbors)
//         {
//             var nbrX = current.Value.x + plusXY.Key;
//             var nbrY = current.Value.y + plusXY.Value;
//             var nbrKey = nbrX + nbrY.ToString();
//             if (nbrX < 0 || nbrY < 0 || nbrX >= maxX || nbrY >= maxY
//                 || !matrix[nbrX][nbrY]
//                 || reds.ContainsKey(nbrKey))
//                 continue;
//
//             if (greens.ContainsKey(nbrKey))
//             {
//                 var curNbr = greens[nbrKey];
//                 var from = Math.Abs(nbrX - fromX) + Math.Abs(nbrY - fromY);
//                 if (from < curNbr.fr)
//                 {
//                     curNbr.fr = from;
//                     curNbr.sum = curNbr.fr + curNbr.to;
//                     curNbr.parent = current.Value;
//                 }
//             }
//             else
//             {
//                 var curNbr = new MatrixNode { x = nbrX, y = nbrY };
//                 curNbr.fr = Math.Abs(nbrX - fromX) + Math.Abs(nbrY - fromY);
//                 curNbr.to = Math.Abs(nbrX - toX) + Math.Abs(nbrY - toY);
//                 curNbr.sum = curNbr.fr + curNbr.to;
//                 curNbr.parent = current.Value;
//                 greens.Add(nbrKey, curNbr);
//             }
//         }
//     }
// }
//
// public class MatrixNode
// {
//     public int fr, to, sum;
//     public MatrixNode parent;
//     public int x, y;
// }