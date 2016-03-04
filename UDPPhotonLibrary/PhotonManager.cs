using System;
using System.Collections.Concurrent;
using System.Threading;

namespace UDPPhotonLibrary
{
    public class PhotonManager : IDisposable
    {
        public ConcurrentBag<Photon> Photons { get; set; }

        public event PhotonDataHandler DataReceived;

        //private Thread _mainThread;

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
        /// Starts the photons
        /// </summary>
        public void Start()
        {
            foreach (Photon photon in Photons)
            {
                photon.StartThread();
            }
        }

        /// <summary>
        /// Stops the photons
        /// </summary>
        public void Stop()
        {
            foreach (Photon photon in Photons)
            {
                photon.StopThread();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
