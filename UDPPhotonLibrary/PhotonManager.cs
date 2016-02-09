using System;
using System.Collections.Concurrent;
using System.Threading;

namespace UDPPhotonLibrary
{
    public class PhotonManager : IDisposable
    {
        public ConcurrentBag<Photon> Photons { get; set; }

        public event PhotonDataHandler DataReceived;

        private Thread _mainThread;

        private bool _stop;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotonManager"/> class.
        /// </summary>
        public PhotonManager()
        {
            Photons = new ConcurrentBag<Photon>();
        }

        /// <summary>
        /// Adds the photon.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        public void AddPhoton(string ip, int port, bool debug = false)
        {
            Photon p = new Photon(ip, port, debug);
            Photons.Add(p);
            p.PhotonDataReceived += PhotonDataReceived;
        }

        /// <summary>
        /// Photons the data received.
        /// </summary>
        /// <param name="data">The data.</param>
        private void PhotonDataReceived(PhotonData data)
        {
            if (DataReceived != null)
            {
                DataReceived(data);
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            if (_mainThread == null)
            {
                _stop = false;
                _mainThread = new Thread(Loop);
                _mainThread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (_mainThread != null)
            {
                _stop = true;
                Thread.Sleep(200);
                if (_mainThread.IsAlive)
                {
                    _mainThread.Abort();
                }
                _mainThread = null;
            }
        }

        /// <summary>
        /// Loops this instance.
        /// </summary>
        private void Loop()
        {
            while (!_stop)
            {
                foreach (var photon in Photons)
                {
                    //(new Thread(photon.Query)).Start();
                    photon.Query();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
