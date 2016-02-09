using System;
using System.Collections.Generic;
using UltrasoundTracking.Commons;

namespace UltrasoundTracking.Sensors
{
    [Serializable]
    public class Sensor : Entity
    {
        public enum Direction { Up, Down, Left, Right};
        
        public int SensorID { get; set; }
        public Direction SignalDirection { get; set; }
        public bool Enabled { get; set; }

        [NonSerialized] public double SignalLength;
        public double StandByValue;
        public double DetectionThreshold;

        [NonSerialized] public List<double> calibrationValues;  // Populated when calibrating the sensors
        

        public Sensor()
        {
            SensorID = 0;
            SignalDirection = 0;
            Enabled = false;
            SignalLength = 0;
            StandByValue = 0;
            DetectionThreshold = 5;
            calibrationValues = new List<double>();
        }

        public Sensor(int sensorID, double x, double y, double z, Direction signalDirection, bool enabled, double detectionThreshold) : base(x,y,z)
        {
            SensorID = sensorID;
            SignalDirection = signalDirection;
            Enabled = enabled;
            SignalLength = 0;
            StandByValue = 0;
            DetectionThreshold = detectionThreshold;
            calibrationValues = new List<double>();
        }

        // Is the sensor obstructed or not (with Threshold)
        public bool IsObstructed()
        {
            if (SignalLength > 0)
                return SignalLength < StandByValue - DetectionThreshold;

            return false;
        }
    }
}
