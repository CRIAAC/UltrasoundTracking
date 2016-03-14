using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace UDPPhotonLibrary
{
    public class PhotonManager : IDisposable
    {
        public List<ConcurrentBag<Photon>> PhotonsGroups;
        public event PhotonDataHandler DataReceived;
        private bool _isSequential;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotonManager"/> class.
        /// </summary>
        public PhotonManager(bool isSequential = false)
        {
            _isSequential = isSequential;
            PhotonsGroups = new List<ConcurrentBag<Photon>>();
            //Photons = new ConcurrentBag<Photon>();
        }

        /// <summary>
        /// Adds the photon.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        public void AddPhoton(string ip, int port, int group = 0, bool debug = false)
        {
            Photon p = new Photon(ip, port, debug);
            p.PhotonDataReceived += PhotonDataReceived;

            while (PhotonsGroups.Count < group + 1)
                PhotonsGroups.Add(new ConcurrentBag<Photon>());

            PhotonsGroups[group].Add(p);
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
            if (_isSequential)
            {
                while (true)
                {
                    for (int i = 0; i < PhotonsGroups.Count; i++)
                    {
                        DateTime lastStartTime = DateTime.Now;
                        foreach (Photon photon in PhotonsGroups[i])
                        {
                            photon.StartThread();
                            lastStartTime = DateTime.Now;
                        }

                        while (DateTime.Now <= lastStartTime.AddMilliseconds(120))
                        {
                        }

                        StopGroup(i);
                    }
                }
            }
            else
                foreach (ConcurrentBag<Photon> concurrentBag in PhotonsGroups)
                {
                    foreach (Photon photon in concurrentBag)
                    {
                        photon.StartThread();
                    }
                }
        }

        /// <summary>
        /// Stops one group of photons
        /// </summary>
        public void StopGroup(int group)
        {
            if (PhotonsGroups.Count >= group + 1)
            {
                foreach (Photon photon in PhotonsGroups[group])
                {
                    photon.StopThread();
                }
            }
        }


        /// <summary>
        /// Stops all the photons
        /// </summary>
        public void StopAll()
        {
            foreach (ConcurrentBag<Photon> photonGroup in PhotonsGroups)
            {
                foreach (Photon photon in photonGroup)
                {
                    photon.StopThread();
                }
            }
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}
