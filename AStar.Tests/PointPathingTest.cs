﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AStar.Options;
using NUnit.Framework;
using Shouldly;

namespace AStar.Tests
{
    [TestFixture]
    public class PointPathingTests
    {
        private WorldGrid _world;

        [SetUp]
        public void SetUp()
        {
            var level = @"XXXXXXX
                          X11X11X
                          X11111X
                          XXXXXXX";

            _world = Helper.ConvertStringToPathfinderGrid(level);
        }

        [Test]
        public void sdgfdfgfg()
        {
            var list = new List<string>();
            list.Add("allo");
            list.Add("allo");

            var taken = list.Take(4);
            
            Assert.True(taken.Count() == 2);
        }
        
        [Test]
        public void ShouldPathPredictablyByPoint()
        {
            var pathfinder = new PathFinder(_world, new PathFinderOptions { UseDiagonals = false });

            var path = pathfinder.FindPath(new Point(1, 1), new Point(5, 1));

            path.ShouldBe(new[] {
                new Point(1, 1),
                new Point(2, 1),
                new Point(2, 2),
                new Point(3, 2),
                new Point(4, 2),
                new Point(5, 2),
                new Point(5, 1),
            });
        }
    }
}