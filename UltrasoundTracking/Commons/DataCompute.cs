using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using UltrasoundTracking.Commons;
using UltrasoundTracking.Photons;
using UltrasoundTracking.Sensors;

namespace UltrasoundTracking
{
    public static class DataCompute
    {
        private static readonly double MinimumDistanceBetweenDetections = 40;

        // Returns sensors obstructed by someone/something (based on the calibration value)
        private static List<Sensor> GetObstructedSensors()
        {
            List<Sensor> obstructedSensors = new List<Sensor>();

            foreach (PhotonGUI photon in MainWindow.DictPhoton.Values)
            {
                foreach (Sensor sensor in photon.Sensors)
                {
                    if (sensor.IsObstructed())
                        obstructedSensors.Add(sensor);
                }
            }

            return obstructedSensors;
        }

        // returns obstructing object position
        private static Position GetObstructingObjectPosition(Sensor sensor)
        {
            Position pos = new Position();
            switch (sensor.SignalDirection)
            {
                case Sensor.Direction.Up:
                    pos.X = sensor.Position.X;
                    pos.Y = sensor.Position.Y - sensor.SignalLength;
                    break;

                case Sensor.Direction.Down:
                    pos.X = sensor.Position.X;
                    pos.Y = sensor.Position.Y + sensor.SignalLength;
                    break;

                case Sensor.Direction.Left:
                    pos.X = sensor.Position.X - sensor.SignalLength;
                    pos.Y = sensor.Position.Y;
                    break;

                case Sensor.Direction.Right:
                    pos.X = sensor.Position.X + sensor.SignalLength;
                    pos.Y = sensor.Position.Y;
                    break;
            }
            Console.Write(pos.Y);
            return pos;
        }

        // Find area of a polygon.
        // From http://csharphelper.com/blog/2014/07/calculate-the-area-of-a-polygon-in-c/
        private static double PolygonArea(List<Entity> entities)
        {
            int num_points = entities.Count;
            Entity[] pts = new Entity[num_points + 1];
            entities.CopyTo(pts, 0);
            pts[num_points] = entities[0];

            double area = 0;
            for (int i = 0; i < num_points; i++)
                area += (pts[i + 1].Position.X - pts[i].Position.X) * (pts[i + 1].Position.Y + pts[i].Position.Y) / 2;

            return Math.Abs(area);
        }

        // Find centroid of a polygon 
        // From http://csharphelper.com/blog/2014/07/find-the-centroid-of-a-polygon-in-c/
        public static Entity PolygonCentroid(List<Entity> entities)
        {
            int num_points = entities.Count;
            Entity[] pts = new Entity[num_points + 1];
            entities.CopyTo(pts, 0);
            pts[num_points] = entities[0];

            double X = 0;
            double Y = 0;

            for (int i = 0; i < num_points; i++)
            {
                var secondFactor = pts[i].Position.X * pts[i + 1].Position.Y - pts[i + 1].Position.X * pts[i].Position.Y;
                X += (pts[i].Position.X + pts[i + 1].Position.X) * secondFactor;
                Y += (pts[i].Position.Y + pts[i + 1].Position.Y) * secondFactor;
            }

            double polygonArea = PolygonArea(entities);
            X /= 6 * polygonArea;
            Y /= 6 * polygonArea;

            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }

            return new Entity(X, Y);
        }

        // Find the distance between two points (positions)
        public static double DistanceBetweenPoints(Position p1, Position p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        // Find the middle between two points (positions)
        private static Position SegmentMiddle(Position p1, Position p2)
        {
            return new Position((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        // Determine which entities are 
        public static List<MovingEntity> NormalizeMovingEntities()
        {
            List<Position> obstructingPositions = new List<Position>();
            foreach (Sensor sensor in GetObstructedSensors())
                obstructingPositions.Add(GetObstructingObjectPosition(sensor));
            bool allObjectsDifferent = false;
            List<MovingEntity> normalizedEntities = new List<MovingEntity>();

            // While we have more than one obstructing position AND all objects are differents
            while (obstructingPositions.Count > 1 && !allObjectsDifferent)
            {
                allObjectsDifferent = true; // By default, we consider all objects as different
                string[] shortestDistancePoints = new string[2];
                double shortestDistance = double.MaxValue;  // To be sure any value will fall below the max value
                Dictionary<string, double> distances = new Dictionary<string, double>();

                for (int i = 0; i < obstructingPositions.Count / 2; i++)
                {
                    Position p1 = obstructingPositions[i];
                    for (int j = 0; j < obstructingPositions.Count; j++)
                    {
                        // If i and j are 2 different points since we loop through the same list
                        if (j != i)
                        {
                            Position p2 = obstructingPositions[j];
                            double distance = DistanceBetweenPoints(p1, p2);

                            if (distance <= MinimumDistanceBetweenDetections &&
                                !distances.Keys.ToList().Exists(key => key == i + "," + j))
                            {
                                distances.Add(i + "," + j, distance);
                                allObjectsDifferent = false;
                            }
                        }
                    }
                }

                if (!allObjectsDifferent)
                {
                    foreach (string key in distances.Keys)
                    {
                        if (distances[key] <= shortestDistance)
                        {
                            shortestDistance = distances[key];
                            shortestDistancePoints = key.Split(',');
                        }
                    }

                    int objectIndex1 = int.Parse(shortestDistancePoints[0]);
                    int objectIndex2 = int.Parse(shortestDistancePoints[1]);

                    obstructingPositions.Add(SegmentMiddle(obstructingPositions[objectIndex1],
                        obstructingPositions[objectIndex2]));

                    if (objectIndex1 > objectIndex2)
                    {
                        obstructingPositions.RemoveAt(objectIndex1);
                        obstructingPositions.RemoveAt(objectIndex2);
                    }
                    else
                    {
                        obstructingPositions.RemoveAt(objectIndex2);
                        obstructingPositions.RemoveAt(objectIndex1);
                    }
                }
            }

            foreach (Position position in obstructingPositions)
            {
                normalizedEntities.Add(new MovingEntity(position.X, position.Y));
            }
            return normalizedEntities;
        }

        // TODO: Change to less case-specific methods (e.g. have vectors in input)
        // Angle between two vectors: one from a MovingEntity (origin to planned position), one with a new point (origin to a detected point)
        public static double AngleBetweenVectors(MovingEntity entity, MovingEntity newEntity)
        {
            Vector originalVector = new Vector(entity.DeltaX, entity.DeltaY);
            Vector newVector = new Vector(newEntity.Position.X - entity.Position.X, newEntity.Position.Y - entity.Position.Y);

            return Vector.AngleBetween(originalVector, newVector);
        }

        // TODO: Change to less case-specific methods (e.g. have vectors in input)
        // Magnitude between two vectors
        public static double MagnitudeBetweenVectors(MovingEntity entity, MovingEntity newEntity)
        {
            double originalVectorMagnitude = DistanceBetweenPoints(new Position(entity.Position.X, entity.Position.Y),
                new Position(entity.Position.X + entity.DeltaX, entity.Position.Y + entity.DeltaY));
            double newVectorMagnitude = DistanceBetweenPoints(new Position(entity.Position.X, entity.Position.Y),
                new Position(newEntity.Position.X, newEntity.Position.Y));

            return Math.Abs(originalVectorMagnitude - newVectorMagnitude);
        }
    }
}
