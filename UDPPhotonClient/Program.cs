using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UDPPhotonLibrary;

namespace UDPPhotonClient
{
    public class Program
    {
        private static StreamWriter _fileWriter;

        static void Main(string[] args)
        {
            if (!File.Exists("photons.txt"))
            {
                File.Create("photons.txt").Close();
                Console.WriteLine("photons.txt didn't exist. Please add photons to the configuration before launching the program.");
            }

            List<string> config = File.ReadLines("photons.txt").ToList();
            config.RemoveAll(s => s.StartsWith("#"));

            if (!config.Any())
            {
                Console.WriteLine("Config file is empty. No Photon to monitor. Exiting.");
                Console.ReadLine();
            }
            else
            {
                _fileWriter = new StreamWriter(new FileStream("./dataset.csv", FileMode.Truncate));
                _fileWriter.WriteLine("ULTRASOUND_1;ULTRASOUND_2;ULTRASOUND_3;ULTRASOUND_4;ULTRASOUND_5;sensor");
                using (PhotonManager manager = new PhotonManager(true))
                {
                    manager.DataReceived += ManagerDataReceived;

                    foreach (string line in config)
                    {
                        if (Regex.IsMatch(line, @"\d{1,3}\.\d{1,3}\.\d{1,3}\:\d{4}\:\d?\:(true|false)?$"))
                        {
                            string[] confLine = line.Split(':'); // ip:port:group:debug
                            string ip = confLine[0];
                            int port = int.Parse(confLine[1]);
                            int group = confLine[2] == "" ? 0 : int.Parse(confLine[2]);
                            bool debug = confLine[3] != "" && bool.Parse(confLine[3]);

                            manager.AddPhoton(ip, port, group, debug);
                        }
                        
                        else
                        {
                            Console.WriteLine("The following line syntax is incorrect: " + line);
                        }
                    }

                    manager.Start();
                    Console.WriteLine("Press enter to quit");
                    Console.ReadLine();
                    manager.Dispose();
                }
            }
        }

        private static void ManagerDataReceived(PhotonData data)
        {
            StringBuilder message = new StringBuilder();
            data.ListMaxSonarSensor.ToList().ForEach(d =>
            {
                message.Append(((d / 6.4) * 25.4).ToString("F")).Append(";");
            });
            message.Append(data.Photon);
            _fileWriter.WriteLine(message);
        }
    }
}
