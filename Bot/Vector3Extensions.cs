using System.Numerics;

namespace Bot
{
    public static class Vector3Extensions
    {
        public static Vector3 MidWay(this Vector3 start, Vector3 end)
        {
            return start + (end - start) / 2;
        }
    }
}