using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Threading;

namespace RemotingSample {

	public class Server {

        private static int mainS = 0;
        private static int replica = 0;
        private int max_delay;
        private int min_delay;
        private string id;
        private Uri url;
        bool crash = false;
        bool freeze = false;
        private TcpChannel channel;
        private MyRemoteObject mo;

        public Server(string id2, Uri url2, int min, int max)
        {
            id = id2;
            url = url2;
            min_delay = min;
            max_delay = max;
        }

        public Server()
        {
        }

        public void setCrash(bool c)
        {
            crash = c;
            mo.setCrash(c);
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

        
        public static int Replica
        {
            get
            {
                return replica;
            }
            set
            {
                replica = value;
            }
        }

        public void method()
        {

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();

            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            string name = "s";
            int port = 8086;
            if (url != null)
            {
                props["port"] = url.Port;
                port = url.Port;
            }
            else
                props["port"] = 8086;
            if (id != null)
            {
                props["name"] = id;
                name = id;
            }
            channel = new TcpChannel(props, null, provider);
            //String reference = "tcp://" + GetIPAddress() + ":" + port + "/MyRemoteObjectName";    
            mo = new MyRemoteObject(min_delay, max_delay);
            RemotingServices.Marshal(mo,
                 "MyRemoteObjectName/" + name,
                 typeof(MyRemoteObject));
        }

        static void Main(string[] args) {

            if(mainS == 0 && replica == 0)
            {
                Server s1 = new Server();
                Thread th = new Thread(new ThreadStart(s1.method));
                th.Start();
            }
			
      
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}