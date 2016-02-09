
using System;

namespace UltrasoundTracking.Commons
{
    [Serializable]
    public class Entity
    {
        public readonly Position Position;

        public Entity()
        {
            Position = new Position();
        }

        public Entity(double x, double y) : this()
        {
            Position.X = Math.Round(x, 1);
            Position.Y = Math.Round(y, 1);
        }

        public Entity(double x, double y, double z) : this(x, y)
        {
            Position.Z = Math.Round(z, 1);
        }
    }
}
    