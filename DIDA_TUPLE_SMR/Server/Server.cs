using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemotingSample {

	public class Server {

        private static int mainS = 0;
        private int max_delay;
        private int min_delay;
        private string id;
        private Uri uri;
        bool crash = false;
        bool freeze = false;
        private MyRemoteObject mo;
        private bool receivedAlive = false;
        private int invokeCount;
        private int changing=0;
        private int maxCount = 10;
        private int replica;
        TcpChannel channel;
        UdpClient server;
        IDictionary<string, Uri> urls = new Dictionary<string, Uri>();

        public Server(string id2, Uri uri2, int min, int max, int v, int pup)
        {
            id = id2;
            uri = uri2;
            min_delay = min;
            max_delay = max;
            replica = v;
            if(pup!=2)
                server =  new UdpClient(uri.Port);
        }

        public Server()
        {
        }

        public Uri getUri()
        {
            return uri;
        }

        public void setUrls(IDictionary<string, Uri> urls2)
        {
            urls = urls2;
            while (mo == null)
                continue;
            mo.setUrls(urls2);
        }

        public void setCrash(bool c)
        {
            crash = c;
            RemotingServices.Disconnect(mo);
            channel.StopListening(mo);
            //Environment.Exit(1);
        }

        public void setFreeze(bool c)
        {
            freeze = c;
            mo.setFreeze(c);
        }

        public void executeByPuppet()
        {
            Console.WriteLine("Server " + id + " is crashed: " + crash + " and is freeze: " + freeze);
            Thread th = new Thread(new ThreadStart(this.method));
            th.Start();
        }

        public void status()
        {
            Console.WriteLine("Server " + id + " is crashed: " + crash + " and is freeze: "+freeze);
        }

        public static int MainS
        {
            get
            {
                return mainS;
            }
            set
            {
                mainS = value;
            }
        }

        public bool ImPrimary()
        {
            bool newP = false;
            foreach(string s in urls.Keys)
            {

                if (s.Equals(id))
                {
                    Console.WriteLine("I AM NEW PRIMARY   " +id);
                    foreach (string s2 in urls.Keys)
                    {
                        
                        var Client = new UdpClient();
                        var RequestData = Encoding.ASCII.GetBytes("alive");

                        Client.EnableBroadcast = true;
                        Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, urls[s2].Port));
                        Client.Close();
                    }
                    
                    return true;
                }
                else
                {

                    var task = Task.Run(() =>
                    {
                        newP = waitNewP();
                    });

                    bool isCompletedSuccessfully = task.Wait(TimeSpan.FromSeconds(8));

                    if (newP)
                    {
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("DIDNT RECEIVED FROM  " + s);
                        continue;
                    }
                    
                }

                
            }
            return false;
        }

        public bool waitNewP()
        {
            var ClientEp = new IPEndPoint(IPAddress.Any, 8888);
            var ClientRequestData = server.Receive(ref ClientEp);
            var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
            return true;
        }


        public void receiveAlive()
        {      
            var autoEvent = new AutoResetEvent(false);
            var stateTimer = new Timer(CheckStatus, autoEvent, 1000, 500);

            Thread th = new Thread(new ThreadStart(checking));
            th.Start();
            autoEvent.WaitOne();
            th.Interrupt();
            stateTimer.Dispose();
            autoEvent.Close();

            if (!ImPrimary())
            {
                changing = 0;
                receivedAlive = true;
                receiveAlive();
            }
            else
            {
                mo.setPrimary(0);
                replica = 0;
                sendAlive();
            }
        }

        public void checking()
        {
            try
            {

                while (true)
                {
                    if (changing == 1)
                        break;
                    var ClientEp = new IPEndPoint(IPAddress.Any, 8888);
                    var ClientRequestData = server.Receive(ref ClientEp);
                    var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);

                    Console.WriteLine("Im server {0} and Recived {1} from {2}", id, ClientRequest, ClientEp.Address.ToString());
                    if (ClientRequest.Equals("alive"))
                        receivedAlive = true;
                }
            }
            catch (Exception)
            {
                return;
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

        public void sendAlive()
        {
            var autoEvent = new AutoResetEvent(false);
            var stateTimer = new Timer(sendStatus, autoEvent, 1000, 500);
            while (true)
            {
                if (crash)
                {
                    stateTimer.Dispose();
                    break;
                }
                autoEvent.WaitOne();
                Console.WriteLine("Alive sent");
            }
            
        }





        // This method is called by the timer delegate.
        public void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
            invokeCount++;

            if (invokeCount == maxCount) {

                if (!receivedAlive)
                {
                    // Reset the counter and signal the waiting thread.

                    invokeCount = 0;
                    changing = 1;
                    autoEvent.Set();
                }
                else
                {
                    Console.WriteLine("Checking status {0}.", DateTime.Now.ToString("h:mm:ss.fff"));
                    invokeCount = 0;
                    receivedAlive = false;
                }
            }    

        }

        public void sendStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
            
            invokeCount++;

            if (invokeCount == maxCount)
            {
                foreach(string s in urls.Keys) {
                    if (urls[s].Port.Equals(uri.Port))
                        continue;
                    Console.WriteLine("Im server {0} and im am sending alive broadcast {1} to " + s + " " + urls[s].Port, id, DateTime.Now.ToString("h:mm:ss.fff"));
                    var Client = new UdpClient();
                    var RequestData = Encoding.ASCII.GetBytes("alive");

                    Client.EnableBroadcast = true;
                    Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, urls[s].Port));
                    Client.Close();
                }
                // Reset the counter and signal the waiting thread.
                invokeCount = 0;
                autoEvent.Set();
            }
        }
        


            public void method()
        {
           
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            string name = "s";
            int port = 8086;
            if (uri != null)
            {
                props["port"] = uri.Port;
                port = uri.Port;
            }
            else
                props["port"] = 8086;
            if (id != null) {
                props["name"] = id;
                name = id;
            }
            channel = new TcpChannel(props, null, provider);
            ChannelServices.RegisterChannel(channel, false);
            //String reference = "tcp://" + GetIPAddress() + ":" + port + "/MyRemoteObjectName";    

            if (replica==0) {
                mo = new MyRemoteObject(min_delay, max_delay, uri,0);
                RemotingServices.Marshal(mo,
                     "MyRemoteObjectName/" + name,
                     typeof(MyRemoteObject));
                maxCount = 8;
                sendAlive();
            }
            else
            {
                mo = new MyRemoteObject(min_delay, max_delay, uri,1);
                Console.WriteLine(uri.ToString());
                RemotingServices.Marshal(mo,
                     "MyRemoteObjectName/" + name,
                     typeof(MyRemoteObject));

                receiveAlive();     
            }
            


        }

        static void Main(string[] args) {

                Server s1 = new Server("s", new Uri("tcp://1.2.3.4:8086/S"),0,0,0,0);
                Thread th = new Thread(new ThreadStart(s1.method));
                th.Start();

			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}