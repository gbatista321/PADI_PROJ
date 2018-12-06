using RemotingSample;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class PuppetMaster
    {
        private IDictionary<string, Server> servers = new Dictionary<string, Server>();
        private IDictionary<string, Server> serversCrashed = new Dictionary<string, Server>();
        private IDictionary<string, Client> clients = new Dictionary<string, Client>();
        private List<Uri> urls = new List<Uri>();
        private static List<string> names = new List<string>();
        IDictionary<string, Uri> urlsD = new Dictionary<string, Uri>();
        private int numServers = 0;

        public void setServers(IDictionary<string, Server> s)
        {
            servers = s;
        }

        public void setClients(IDictionary<string, Client> c)
        {
            clients = c;
        }

        public void setUrls(List<Uri> l)
        {
            urls = l;
        }

        public IDictionary<string, Uri> getUrlsD()
        {
            return urlsD;
        }

        public void setNames(List<string> l)
        {
            names = l;
        }

        public IDictionary<string, Server> getServers()
        {
            return servers;
        }

        public IDictionary<string, Client> getClients()
        {
            return clients;
        }

        public List<Uri> getUrls()
        {
            return urls;
        }

        public List<string> getNames()
        {
            return names;
        }

        private void setNumS(int n)
        {
            numServers = n;
        }

        private int getNumS()
        {
            return numServers;
        }

        PuppetMaster() { }



        static void Main(string[] args)
        {

            PuppetMaster p = new PuppetMaster();


            Thread ch = new Thread(new ThreadStart(p.runChannel));
            ch.Start();

            if (args.Length == 1)
            {
                string[] lines = File.ReadAllLines(args[0]);
                foreach (string l in lines)
                {
                    try
                    {
                        p = execute(l, p);
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine("try again");
                        Console.WriteLine(l);
                        Console.WriteLine(e.GetType());
                        Console.WriteLine(e.StackTrace);
                        Console.ReadLine();
                    }

                }

            }

            while (true)
            {
                Console.WriteLine("Write a command");
                string l = Console.ReadLine();
                if (l.Length == 0)
                    continue;

                try
                {
                    p = execute(l, p);
                }
                catch (Exception)
                {
                    Console.WriteLine("try again");
                }

            }

        }

        private static PuppetMaster execute(string l, PuppetMaster p)
        {
            string id;
            Uri uri;
            string[] splited = l.Split(new char[] { ' ' });
            string cmd = splited[0];
            int delay;

            switch (cmd)
            {
                case "Server":

                    /*Compare here if the ip is the localhost is the same as requested ip, if it is executes below if not executes sendToPCS(l,ip,port)*/

                    id = splited[1];
                    if (p.getServers().ContainsKey(id))
                    {
                        break;
                    }

                    uri = new Uri(splited[2]);
                    p.getUrls().Add(uri);
                    p.getNames().Add(id);
                    p.getUrlsD().Add(id, uri);
                    int min_delay = Int32.Parse(splited[3]);
                    int max_delay = Int32.Parse(splited[4]);
                    Server s = new Server(id, uri, min_delay, max_delay);
                    p.getServers().Add(id, s);
                    p.setNumS(p.getNumS() + 1);
                    Thread th = new Thread(new ThreadStart(s.executeByPuppet));
                    th.Start();




                    break;

                case "Client":

                    id = splited[1];
                    if (p.getClients().ContainsKey(id))
                    {
                        break;
                    }

                    uri = new Uri(splited[2]);
                    string script = splited[3];

                    Client c = new Client(id, uri, script, p.getUrls(), p.getNames());
                    p.getClients().Add(id, c);
                    Thread th2 = new Thread(new ThreadStart(c.executeByPuppet));
                    th2.Start();

                    break;

                /* in this next commands, check if the ip is the same as localhost, if not need to get the information from PCS*/

                case "Status":
                    foreach (Server s2 in p.serversCrashed.Values)
                        s2.status();
                    foreach (Server s2 in p.getServers().Values)
                        s2.status();
                    break;

                case "Crash":
                    id = splited[1];
                    if (p.getServers().ContainsKey(id))
                    {

                        p.setNumS(p.getNumS() - 1);
                        p.getServers()[id].setCrash(true);
                        p.serversCrashed.Add(id, p.getServers()[id]);
                        p.getServers().Remove(id);
                    }

                    break;

                case "Freeze":
                    id = splited[1];
                    if (p.getServers().ContainsKey(id))
                        p.getServers()[id].setFreeze(true);
                    break;

                case "Unfreeze":
                    id = splited[1];
                    if (p.getServers().ContainsKey(id))
                        p.getServers()[id].setFreeze(false);
                    break;

                case "Wait":
                    Int32.TryParse(splited[1], out delay);
                    System.Threading.Thread.Sleep(delay);
                    break;
            }
            return p;
        }

        public void runChannel()

        {
            IPAddress localAdd = IPAddress.Parse(GetIPAddress());
            TcpListener listener = new TcpListener(localAdd, 10001);
            Console.WriteLine("Listening...");
            listener.Start();
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

                if (dataReceived.Equals("status"))
                    foreach (Server s2 in servers.Values)
                        s2.status();
            }
        }

        public void sendToPCS(string l, string ip, int port)
        {

            //---create a TCPClient object at the IP and port no.---
            TcpClient client = new TcpClient(ip, port);
            NetworkStream nwStream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(l);

            //---send the text---
            Console.WriteLine("Sending : " + l);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            client.Close();
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

    }

}

