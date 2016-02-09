using System.Collections.Generic;

namespace UDPPhotonLibrary
{
    public class PhotonData
    {
        public string Photon { get; set; }

        public List<long> ListMaxSonarSensor { get; set; }

        public PhotonData()
        {
            Photon = "";
            ListMaxSonarSensor = new List<long>();
        }

        public PhotonData(int sensorNumber) : this()
        {
            ListMaxSonarSensor = new List<long>(sensorNumber);
        }

        public PhotonData(string photon, int sensorNumber) : this()
        {
            Photon = photon;
            ListMaxSonarSensor = new List<long>(sensorNumber);
        }
    }
}
                                                                                                       