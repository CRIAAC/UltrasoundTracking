using System;
using System.Collections.Specialized;

namespace UltrasoundTracking.Commons
{
    public class MovingEntity : Entity
    {
        public readonly OrderedDictionary _lastKnownPositions;
        private const int PositionsBacklogSize = 20;
        private const double MinSpeedToIdle = 1;

        public bool IsIdle
        {
            get
            {
                if (DeltaX <= MinSpeedToIdle && DeltaY <= MinSpeedToIdle && _lastKnownPositions.Count > 1)
                    return true;
                return false;   // else
            }
        }

        public double DeltaX;
        public double DeltaY;

        public MovingEntity(double x, double y) : base(x, y)
        {
            _lastKnownPositions = new OrderedDictionary {{DateTime.Now, Position}};
            DeltaX = DeltaY = 0;
        }

        public void UpdatePosition(double x, double y)
        {
            DeltaX = x - Position.X;
            DeltaY = y - Position.Y;

            Position.X = x;
            Position.Y = y;

            _lastKnownPositions.Add(DateTime.Now, Position);
            if (_lastKnownPositions.Count > PositionsBacklogSize)
                _lastKnownPositions.RemoveAt(0);
        }
    }
}