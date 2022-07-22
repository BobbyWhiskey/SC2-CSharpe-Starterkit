using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace Bot.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
    }

    [Fact]
    public static void unitTest_AStar()
    {
        bool[][] matrix =
        {
            new[] { true, true, true, true, false },
            new[] { true, false, false, true, true },
            new[] { true, true, true, false, true },
            new[] { false, true, false, true, true },
            new[] { true, true, true, true, false }
        };

        //looking for shortest path from 'S' at (0,1) to 'E' at (3,3)
        //obstacles marked by 'X'
        int fromX = 0, fromY = 1, toX = 3, toY = 3;

        var grid = new List<List<Node>>();
        var x = 0;

        foreach (var lineX in matrix)
        {
            var y = 0;
            var gridLine = new List<Node>();
            foreach (var value in lineX)
            {
                gridLine.Add(new Node(new Vector2(x, y), value));
                y++;
            }

            grid.Add(gridLine);
            x++;
        }

        var astar = new Astar(grid);
        var path = astar.FindPath(new Vector2(fromX, fromY), new Vector2(toX, toY));

        Console.WriteLine("The shortest path from  " +
                          "(" + fromX + "," + fromY + ")  to " +
                          "(" + toX + "," + toY + ")  is:  \n");
        //
        // while (path.Count() > 0)
        // {
        //     var node = path.Pop();
        //     Console.WriteLine("(" + node.Position.X + "," + node.Position.Y + ")");
        // }
    }
}