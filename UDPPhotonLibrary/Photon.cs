using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UDPPhotonLibrary
{
    public delegate void PhotonDataHandler(PhotonData data);

    [Serializable()]
    public class Photon
    {
        public string Ip { get; set; }

        public int Port { get; set; }

        public event PhotonDataHandler PhotonDataReceived;

        private IPEndPoint _photon;
        private readonly UdpClient _client;

        private Stopwatch _sw = new Stopwatch();
        private bool _timeOut = false;
        private bool _debug;

        private List<PhotonData> _lastFilteredValues;
        private long _threshold = 2; // Threshold allowed for data filtering
        private int filterBaseValues = 10; // Number of values to keep to filter the data

        private Thread _photonThread;
        private bool _isThreadOn;

        /// <summary>
        /// Initializes a new instance of the <see cref="Photon"/> class.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        public Photon(string ip, int port, bool debug = false)
        {
            Ip = ip;
            Port = port;
            _client = new UdpClient(Ip, Port);
            _client.Client.ReceiveTimeout = 120;
            _photon = new IPEndPoint(IPAddress.Parse(Ip), Port);
            _lastFilteredValues = new List<PhotonData>();
            _debug = debug;


        }

        public void StartThread()
        {
            _photonThread = new Thread(Query);
            _isThreadOn = true;
            _photonThread.Start();
        }

        public void StopThread()
        {
            if (_photonThread != null && _photonThread.IsAlive)
            {
                _photonThread.Interrupt();
                _isThreadOn = false;
            }
        }

        /// <summary>
        /// Queries this instance.
        /// </summary>
        public void Query()
        {
            while (_isThreadOn)
            {
                if (PhotonDataReceived != null)
                {
                    try
                    {
                        _client.AllowNatTraversal(true);
                        byte[] data = Encoding.ASCII.GetBytes("OK");
                        _client.Send(data, data.Length);
                        var receivedData = _client.Receive(ref _photon);

                        if (!_timeOut)
                        {
                            _sw.Restart();
                            _timeOut = true;
                        }

                        var json = Encoding.UTF8.GetString(receivedData);

                        //Console.WriteLine(json);
                        var photonData = fastJSON.JSON.ToObject<PhotonData>(json);
                        PhotonDataReceived(photonData);

                        if (_debug)
                        {
                            Console.WriteLine("==============================\n" +
                                              "Photon: " + Ip + ":" + Port);
                            Console.Write(photonData.ListMaxSonarSensor[0] + " ");
                            Console.Write(photonData.ListMaxSonarSensor[1] + " ");
                            Console.Write(photonData.ListMaxSonarSensor[2] + " ");
                            Console.Write(photonData.ListMaxSonarSensor[3] + " ");
                            Console.Write(photonData.ListMaxSonarSensor[4] + " ");
                            Console.Write(photonData.ListMaxSonarSensor[5]);
                            Console.WriteLine("\n==============================");

                            Console.WriteLine("\n");
                        }
                    }
                    catch (Exception e)
                    {
                        if (_timeOut)
                        {
                            _sw.Stop();
                            _timeOut = false;
                            if (_debug)
                            {
                                Console.Write("[" + DateTime.Now.ToLongTimeString() + "] Timeout error; Photon " + Ip + ":" + Port + "; ");
                                Console.WriteLine("Timeout delay : {" + _sw.Elapsed.Minutes + "}:{" + _sw.Elapsed.Seconds +
                                              "}:{" + _sw.Elapsed.Milliseconds + "}");
                            }
                        }
                        else
                        {
                            if (_debug)
                            {
                                Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] Timeout error; Photon " + Ip + ":" + Port + "; Exception: " + e.Message + "\n");
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
