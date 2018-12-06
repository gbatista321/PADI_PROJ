using RemotingSample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCS
{
    class Program
    {
        static void Main(string[] args)
        {
            IDictionary<string, Server> servers = new Dictionary<string, Server>();
            IDictionary<string, Client> clients = new Dictionary<string, Client>();
            int bad = 0; 

            IPAddress localAdd = IPAddress.Parse(GetIPAddress());
            TcpListener listener = new TcpListener(localAdd, 10000);
            Console.WriteLine("Listening...");
            listener.Start();

            //---incoming client connected---
            

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                //---get the incoming data through a network stream---
                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                //---read incoming stream---
                int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                //---convert the data received into a string---
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                bad = execute(dataReceived, servers, clients);
                if (bad == 1)
                    Console.WriteLine("bad format");

            }

        }

        public static string GetIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private static int execute(string l, IDictionary<string, Server> servers, IDictionary<string, Client> clients)
        {
            Console.ReadLine();
            int badIn = 0;
            string id, url;
            Uri uri;
            string[] splited = l.Split(new char[] { ' ' });
            string cmd = splited[0];
            int delay;

            switch (cmd)
            {
                case "Server":

                    id = splited[1];
                    if (servers.ContainsKey(id))
                    {
                        badIn = 1;
                        break;
                    }

                    uri = new Uri(splited[2]);
                    url = uri.AbsolutePath;
                    int min_delay = Int32.Parse(splited[3]);
                    int max_delay = Int32.Parse(splited[4]);

                    Server s = new Server(id, url, min_delay, max_delay);
                    servers.Add(id, s);
                    Thread th = new Thread(new ThreadStart(s.executeByPuppet));
                    th.Start();

                    break;

                case "Client":

                    id = splited[1];
                    if (clients.ContainsKey(id))
                    {
                        badIn = 1;
                        break;
                    }

                    uri = new Uri(splited[2]);
                    url = uri.AbsolutePath;
                    string script = splited[3];

                    Client c = new Client(id, url, script);
                    clients.Add(id, c);
                    Thread th2 = new Thread(new ThreadStart(c.executeByPuppet));
                    th2.Start();

                    break;

                case "Status":

                    foreach (Server s2 in servers.Values)
                        s2.status();
                    break;

                case "Crash":
                    id = splited[1];
                    if (servers.ContainsKey(id))
                        servers[id].setCrash(true);
                    break;

                case "Freeze":
                    id = splited[1];
                    if (servers.ContainsKey(id))
                        servers[id].setFreeze(true);
                    break;

                case "Unfrezze":
                    id = splited[1];
                    if (servers.ContainsKey(id))
                        servers[id].setFreeze(false);
                    break;

                case "Wait":
                    Int32.TryParse(splited[1], out delay);
                    System.Threading.Thread.Sleep(delay);
                    break;
            }
            return badIn;
        }
    }


}
