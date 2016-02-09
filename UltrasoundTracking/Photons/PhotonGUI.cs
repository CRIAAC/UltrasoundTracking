using System.Collections.Generic;
using UltrasoundTracking.Sensors;
using System;
using System.Windows.Media;


//using System.Windows.Media;

namespace UltrasoundTracking.Photons
{
    [Serializable()]
    public class PhotonGUI
    {
        public string FriendlyName { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public List<Sensor> Sensors { get; set; }
        public Color Col { get; set; }
        public Brush Col2 => new SolidColorBrush(Col);

        public PhotonGUI()
        {
            IP = "";
            Port = 0;
            Sensors = new List<Sensor>();
            Col = Colors.Black;
        }

        public PhotonGUI(string ip, int port) : this()
        {
            IP = ip;
            Port = port;
        }

        public PhotonGUI(string ip, int port, Color col) : this(ip, port)
        {
            Col = col;
        }

        public PhotonGUI(string friendlyName, string ip, int port, List<Sensor> sensors, Color col) : this(ip, port, col)
        {
            FriendlyName = friendlyName;
            Sensors = sensors;
        }
    }
}
