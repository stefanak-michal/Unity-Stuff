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
        public ReceiveEvent OnReceive;

        Thread broadcaster, listener;
        UdpClient client;
        IPEndPoint from = new IPEndPoint(0, 0);
        AndroidJavaObject multicastLock;
        Queue<IPAddress> received = new Queue<IPAddress>();

        void Start()
        {
            InitializeClient();
            StartListener();
            StartBroadcaster();
            StartCoroutine(ProcessReceived());
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
        /// Listen to messages
        /// </summary>
        void StartListener()
        {
            (listener = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        client?.Receive(ref from);
                        if (!received.Contains(from.Address))
                            received.Enqueue(from.Address);
                    }
                    catch (Exception e)
                    {
                        //background thread, you can't use Debug.Log
                    }

                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            }).Start();
        }

        /// <summary>
        /// Broadcast message over network
        /// </summary>
        void StartBroadcaster()
        {
            (broadcaster = new Thread(() =>
            {
                var data = System.Text.Encoding.UTF8.GetBytes("HELLO");
                while (true)
                {
                    //You can add some condition here to broadcast only if it's needed, like app is running as server
                    try
                    {
                        client?.Send(data, data.Length, ip, port);
                    }
                    catch (Exception e)
                    {
                        //background thread, you can't use Debug.Log
                    }
                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            }).Start();
        }

        /// <summary>
        /// Create UDP client
        /// </summary>
        void InitializeClient()
        {
            if (!IPAddress.TryParse(ip, out IPAddress address))
            {
                Debug.LogError("Wrong IP address format");
                return;
            }

            client = new UdpClient();
            client.ExclusiveAddressUse = false;
            client.MulticastLoopback = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            client.JoinMulticastGroup(address);
        }

        /// <summary>
        /// Clean up at end
        /// </summary>
        private void OnDestroy()
        {
            client?.Close();
            broadcaster?.Abort();
            listener?.Abort();
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
