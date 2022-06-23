﻿using System.Numerics;
using SC2APIProtocol;

namespace Bot;

public static class Vector3Extensions
{
    public static Vector3 MidWay(this Vector3 start, Vector3 end)
    {
        return start + (end - start) / 2;
    }

    public static Point ToPoint(this Vector3 vector)
    {
        return new Point
        {
            X = vector.X,
            Y = vector.Y,
            Z = vector.Z
        };
    }
}