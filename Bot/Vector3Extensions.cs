using System.Numerics;
using SC2APIProtocol;

namespace Bot;

public static class Vector3Extensions
{
    public static Vector3 MidWay(this Vector3 start, Vector3 end)
    {
        return start + (end - start) / 2;
    }

    public static Vector3 Normalize(this Vector3 vector)
    {
        var length = vector.Length();
        return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
    }

    public static Vector3 ToVector3(this System.Drawing.Point point)
    {
        return new Vector3(point.X, point.Y, 0);
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