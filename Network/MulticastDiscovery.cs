using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Network
{
    /// <summary>
    /// UDP Multicast service
    /// Developed and tested on Windows and Android platform
    /// </summary>
    public class MulticastDiscovery : MonoBehaviour
    {
        [Serializable]
        public class ReceiveEvent : UnityEvent<IPAddress> { }

        /// <summary>
        /// I choosed this IP based on wiki, where is marked as "Simple Service Discovery Protocol address"
        /// <see cref="https://en.wikipedia.org/wiki/Multicast_address"/>
        /// </summary>
        public string ip = "239.255.255.250";
        public int port;
        public bool log;
        public ReceiveEvent OnReceive;

        AndroidJavaObject multicastLock;
        Queue<IPAddress> received = new Queue<IPAddress>();
        List<UdpClient> clients = new List<UdpClient>();

        void Start()
        {
            InitializeClients();
            StartCoroutine(ProcessReceived());

            if (Debug.isDebugBuild && log)
                StartCoroutine(ProcessErrors());
        }

        Queue<string> errors = new Queue<string>();

        IEnumerator ProcessErrors()
        {
            while (true)
            {
                yield return new WaitUntil(() => errors.Count > 0);
                Debug.Log(errors.Dequeue());
            }
        }

        /// <summary>
        /// Handle received IP addresses
        /// </summary>
        IEnumerator ProcessReceived()
        {
            while (true)
            {
                yield return new WaitUntil(() => received.Count > 0);
                OnReceive?.Invoke(received.Dequeue());
            }
        }

        /// <summary>
        /// Create UDP clients
        /// </summary>
        void InitializeClients()
        {
            if (!IPAddress.TryParse(ip, out IPAddress destination))
            {
                Debug.LogError("Wrong IP address format");
                return;
            }

            foreach (IPAddress local in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (local.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var client = new UdpClient(AddressFamily.InterNetwork);
                client.ExclusiveAddressUse = false;
                client.MulticastLoopback = false;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(local, port));
                client.JoinMulticastGroup(destination, local);
                clients.Add(client);

                new Thread(() =>
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, port);

                    while (true)
                    {
                        try
                        {
                            client.Receive(ref from);
                            if (!received.Contains(from.Address))
                            {
                                received.Enqueue(from.Address);
                                errors.Enqueue("Received " + from.Address);
                            }
                        }
                        catch (Exception e)
                        {
                            errors.Enqueue("Error receiving " + e.Message);
                            //background thread, you can't use Debug.Log
                        }

                        Thread.Sleep(1000);
                    }
                })
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                }.Start();

                new Thread(() =>
                {
                    var data = System.Text.Encoding.UTF8.GetBytes("HELLO");
                    while (true)
                    {
                        //You can add some condition here to broadcast only if it's needed, like app is running as server
                        //{
                            try
                            {
                                client.Send(data, data.Length, ip, port);
                                errors.Enqueue("Sended");
                            }
                            catch (Exception e)
                            {
                                errors.Enqueue("Error sending " + e.Message);
                                //background thread, you can't use Debug.Log
                            }
                        //}
                        Thread.Sleep(1000);
                    }
                })
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                }.Start();
            }
        }

        /// <summary>
        /// Clean up at end
        /// </summary>
        private void OnDestroy()
        {
            foreach (UdpClient client in clients)
                client.Close();

            multicastLock?.Call("release");
        }

        /// <summary>
        /// If you have problems with multicast lock on android, this method can help you
        /// </summary>
        void MulticastLock()
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "lock");
                    multicastLock.Call("acquire");
                }
            }
        }
    }
}
