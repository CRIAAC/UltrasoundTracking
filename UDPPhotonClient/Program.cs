using System;
using System.IO;
using System.Linq;
using System.Text;
using UDPPhotonLibrary;

namespace UDPPhotonClient
{
    public class Program
    {
        private static StreamWriter _fileWriter;

        static void Main(string[] args)
        {
            if (!File.Exists("./dataset.csv"))
                File.Create("./dataset.csv").Close();

            _fileWriter = new StreamWriter(new FileStream("./dataset.csv", FileMode.Truncate));
            _fileWriter.WriteLine("ULTRASOUND_1;ULTRASOUND_2;ULTRASOUND_3;ULTRASOUND_4;ULTRASOUND_5;sensor");
            using (PhotonManager manager = new PhotonManager(true))
            {
                manager.DataReceived += ManagerDataReceived;
                manager.AddPhoton("192.168.1.31", 8888, 0, true);
                manager.AddPhoton("192.168.1.32", 8888, 1, true);

                manager.Start();
                Console.WriteLine("Press enter to quit");
                Console.ReadLine();
                manager.StopAll();
            }
        }

        private static void ManagerDataReceived(PhotonData data)
        {
            StringBuilder message = new StringBuilder();
            data.ListMaxSonarSensor.ToList().ForEach(d =>
            {
                message.Append(((d/6.4) * 25.4).ToString("F")).Append(";");
            });
            message.Append(data.Photon);
            _fileWriter.WriteLine(message);
        }
    }
}
