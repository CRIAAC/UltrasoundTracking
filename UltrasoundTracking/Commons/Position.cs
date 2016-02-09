using System;

namespace UltrasoundTracking
{
    [Serializable]
    public class Position
    {
        public double X;
        public double Y;
        public double Z;

        public Position()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Position(double x, double y) : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "X=" + X + ";Y=" + Y + ";Z=" + Z;
        }
    }
}