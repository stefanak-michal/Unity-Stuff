using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace File
{
    /// <summary>
    /// Sending files through network on the background with TCP
    /// Using Unity HLAPI and Threading
    /// </summary>
    /// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
    /// <author>Michal Stefanak</author>
    public sealed class Share : MonoBehaviour
    {
        public int portRangeFrom = 47100;
        public int portRangeTo = 47200;
        public int bufferSize = 1024;
        public int timeout = 10;

        #region network message stuff
        const short fileSharePrepare = MsgType.Highest + 1;
        const short getClientsSendFile = MsgType.Highest + 2;

        class FileSharePrepare : MessageBase
        {
            public string crc;
            public string receiveAction;
            public string extension;
            public int port;
        }

        class NetConn : MessageBase
        {
            public string[] addresses;
            public int port;
            public string file;
        }
        #endregion

        #region variables
        static Share instance;
        static List<int> ports = new List<int>();
        static System.Random r;
        //List of all received files, key is crc
        static Dictionary<string, FileData> files = new Dictionary<string, FileData>();
        //actions after receiving file, key is custom identifier
        static Dictionary<string, Action<string>> onReceiveActions = new Dictionary<string, Action<string>>();
        static string tempDirectory;

        struct FileData
        {
            public string file;
            public string receiveAction;
        }
        #endregion

        private void Start()
        {
            instance = this;
            r = new System.Random();
            StartCoroutine(RegisterHandlers());
        }

        public static void SetTempDirectory(string dir)
        {
            tempDirectory = dir;
        }

        IEnumerator RegisterHandlers()
        {
            yield return new WaitUntil(() => NetworkManager.singleton.isNetworkActive);

            if (NetworkServer.active)
            {
                NetworkServer.RegisterHandler(fileSharePrepare, ServerPrepare);
                NetworkServer.RegisterHandler(getClientsSendFile, ServerConnections);
            }

            if (NetworkClient.active)
            {
                NetworkManager.singleton.client.RegisterHandler(fileSharePrepare, ClientPrepare);
                NetworkManager.singleton.client.RegisterHandler(getClientsSendFile, SendFileToClients);
            }
        }

        private void ServerPrepare(NetworkMessage netMsg)
        {
            FileSharePrepare msg = netMsg.ReadMessage<FileSharePrepare>();
            NetworkServer.SendToAll(netMsg.msgType, msg);
        }

        private void ServerConnections(NetworkMessage netMsg)
        {
            NetConn msg = netMsg.ReadMessage<NetConn>();
            List<string> addresses = new List<string>();
            foreach (var conn in NetworkServer.connections)
            {
                if (conn != null)
                    addresses.Add(conn.address == "localClient" ? GetIP() : conn.address);
            }
            msg.addresses = addresses.ToArray();

            NetworkServer.SendToClient(netMsg.conn.connectionId, getClientsSendFile, msg);
        }

        private void ClientPrepare(NetworkMessage netMsg)
        {
            FileSharePrepare msg = netMsg.ReadMessage<FileSharePrepare>();

            if (files.ContainsKey(msg.crc))
            {
                if (onReceiveActions.ContainsKey(msg.receiveAction))
                    onReceiveActions[msg.receiveAction](files[msg.crc].file);
            }
            else
            {
                StartCoroutine(WaitForReceivedFile(msg.crc));
                new Thread(() => Host(msg.extension, msg.receiveAction, msg.port)).Start();
            }
        }

        private void SendFileToClients(NetworkMessage netMsg)
        {
            NetConn msg = netMsg.ReadMessage<NetConn>();

            foreach (var addr in msg.addresses)
                InvokeThread(addr, msg.port, msg.file);
        }

        IEnumerator WaitForReceivedFile(string crc)
        {
            //the reason why I used while: WaitUntil isn't working with this condition
            while (!files.ContainsKey(crc))
                yield return null;

            if (onReceiveActions.ContainsKey(files[crc].receiveAction))
                onReceiveActions[files[crc].receiveAction](files[crc].file);
        }

        public static void RegisterReceiveAction(string name, Action<string> action)
        {
            onReceiveActions[name] = action;
        }

        public static void UnregisterReceiveAction(string name)
        {
            if (onReceiveActions.ContainsKey(name))
                onReceiveActions.Remove(name);
        }

        /// <summary>
        /// Send file
        /// </summary>
        /// <param name="file">path to file</param>
        /// <param name="receiveAction">Action identifier after receiving a file</param>
        public static void Send(string file, string receiveAction)
        {
            if (file == null || file == string.Empty || !File.Exists(file))
                throw new Exception("Non valid file");

            if (ports.Count >= (instance.portRangeTo - instance.portRangeFrom))
                throw new Exception("Not available port");

            int port = instance.portRangeFrom;
            while (ports.Contains(port) && port < instance.portRangeTo)
                port++;
            ports.Add(port);

            var prepareMsg = new FileSharePrepare()
            {
                crc = GetCRC(file),
                receiveAction = receiveAction,
                extension = Path.GetExtension(file),
                port = port
            };

            //msg to clients, to be prepared
            if (NetworkClient.active)
            {
                NetworkManager.singleton.client.Send(fileSharePrepare, prepareMsg);
                NetworkManager.singleton.client.Send(getClientsSendFile, new NetConn()
                {
                    port = port,
                    file = file
                });
            }
            else if (NetworkServer.active)
            {
                NetworkServer.SendToAll(fileSharePrepare, prepareMsg);
                foreach (var conn in NetworkServer.connections)
                {
                    if (conn != null)
                        InvokeThread(conn.address, port, file);
                }
            }
        }

        static void InvokeThread(string address, int port, string file)
        {
            if (address == null)
                return;

            new Thread(() => Client(address.Replace("::ffff:", ""), port, file)).Start();
        }

        /// <summary>
        /// Receiving file
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="receiveAction"></param>
        static void Host(string extension, string receiveAction, int port)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Server.ReceiveTimeout = instance.timeout;
                listener.Start();
                TcpClient client = listener.AcceptTcpClient();

                var netstream = client.GetStream();
                byte[] recData = new byte[instance.bufferSize];
                int totalrecbytes = 0;
                int recBytes;

                string name = DateTime.Now.ToString("yyyyMMddHHmmss_") + r.Next(1000, 10000);

                //received file are stored in temp
                if (tempDirectory == string.Empty)
                    tempDirectory = Path.GetTempPath();
                tempDirectory.TrimEnd('/', '\\');
                tempDirectory += Path.DirectorySeparatorChar;
                if (!Directory.Exists(tempDirectory))
                    Directory.CreateDirectory(tempDirectory);

                string tmpFile = tempDirectory + name + ".tmp";
                string normalFile = tempDirectory + name + extension;

                //send file through opened stream
                FileStream fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write);
                while ((recBytes = netstream.Read(recData, 0, recData.Length)) > 0)
                {
                    fs.Write(recData, 0, recBytes);
                    totalrecbytes += recBytes;
                }
                fs.Close();

                netstream.Close();
                client.Close();
                listener.Stop();

                if (File.Exists(normalFile))
                    File.Delete(normalFile);
                File.Move(tmpFile, normalFile);

                files.Add(GetCRC(normalFile), new FileData() { file = normalFile, receiveAction = receiveAction });
            }
            catch (Exception e)
            {
                File.AppendAllText("fileshare.log", "[" + DateTime.Now.ToString() + "] " + e.Source + ": " + e.Message + "\n" + e.StackTrace + "\n\n");
            }
        }

        /// <summary>
        /// Sending file over TCP
        /// </summary>
        /// <param name="ip">Where</param>
        /// <param name="port"></param>
        /// <param name="path">What</param>
        static void Client(string ip, int port, string path)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            TcpClient client;
            while (true)
            {
                try
                {
                    client = new TcpClient(ip, port);
                    if (client.Connected)
                        break;
                }
                catch (Exception)
                {
                    if (sw.Elapsed.Seconds > instance.timeout)
                    {
                        sw.Stop();
                        return;
                    }

                    continue;
                };
            }

            sw.Stop();

            try
            {
                NetworkStream netstream = client.GetStream();

                byte[] file = File.ReadAllBytes(path);
                netstream.Write(file, 0, file.Length);
                netstream.Close();

                client.Close();
                ports.Remove(port);
            }
            catch (Exception e)
            {
                File.AppendAllText("fileshare.log", "[" + DateTime.Now.ToString() + "] " + e.Source + ": " + e.Message + "\n" + e.StackTrace + "\n\n");
            }
        }

        /// <summary>
        /// CRC is generated only from first [bufferSize] bytes, because of performance
        /// </summary>
        /// <param name="file"></param>
        /// <returns>CRC</returns>
        static string GetCRC(string file)
        {
            byte[] data = new byte[instance.bufferSize];

            var stream = File.OpenRead(file);
            stream.Read(data, 0, (int)Mathf.Clamp(instance.bufferSize, 0, new FileInfo(file).Length));
            stream.Close();

            MD5 md5 = MD5.Create();
            return System.Text.Encoding.UTF8.GetString(md5.ComputeHash(data));
        }

        string GetIP()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }

            return localIP;
        }

        /// <summary>
        /// Delete all received files from temp
        /// </summary>
        private void OnDestroy()
        {
            foreach (var f in files)
            {
                if (File.Exists(f.Value.file))
                    File.Delete(f.Value.file);
            }
        }
    }
}
