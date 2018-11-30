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
        bool debug;

        void Start()
        {
            debug = Debug.isDebugBuild;

            InitializeClient();
            StartListener();
            StartBroadcaster();
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
                        OnReceive.Invoke(from.Address);
                        Log("broadcast received " + from.Address);
                    }
                    catch (Exception e)
                    {
                        Log(e.Message);
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
                        Log("broadcast sended");
                    }
                    catch (Exception e)
                    {
                        Log(e.Message);
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
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address))
            {
                Log("Wrong IP address format");
                return;
            }

            client = new UdpClient();
            client.ExclusiveAddressUse = false;
            client.MulticastLoopback = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            client.JoinMulticastGroup(address);

            Log("UDP client initialized");
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
        /// Platform dependant log
        /// </summary>
        /// <param name="msg"></param>
        void Log(string msg)
        {
            if (debug)
            {
#if UNITY_ANDROID
                Console.WriteLine(msg);
#elif UNITY_EDITOR
                Debug.Log(msg);
#endif
            }
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
                    Log("multicast lock acquired");
                }

            }
        }
    }
}
