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

        public Program() { }

        static void Main(string[] args)
        {
            Program p = new Program();
            Console.WriteLine(GetIPAddress());

            //---incoming client connected---

            var server = new UdpClient(10000);
            while (true)
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 10000);
                var ClientRequestData = server.Receive(ref ClientEp);
                var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);

                p = execute(ClientRequest, p);

            }

        }

        private static Program execute(string l, Program p)
        {
            string id;
            Uri uri;
            string[] splited = l.Split(new char[] { ' ' });
            string cmd = splited[0];

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
                    Server s;

                    if (p.getNumS() > 0)
                    {
                        s = new Server(id, uri, min_delay, max_delay, 1,0);
                    }
                    else
                        s = new Server(id, uri, min_delay, max_delay, 0,0);
                    p.getServers().Add(id, s);
                    p.setNumS(p.getNumS() + 1);
                    Thread th = new Thread(new ThreadStart(s.executeByPuppet));
                    th.Start();
                    foreach (Server s2 in p.getServers().Values)
                    {
                        s2.setUrls(p.getUrlsD());
                    }
                    foreach (Client c2 in p.getClients().Values)
                    {
                        c2.setUrls(p.getUrls());
                        c2.setNames(p.getNames());
                    }




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

            }
            return p;
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
