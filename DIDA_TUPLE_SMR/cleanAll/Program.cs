using RemotingSample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace cleanAll
{
    class CleanAll
    {
        public CleanAll() { }

        public void clean()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            String reference = "tcp://localhost:50001/MyRemoteObjectName";
            MyRemoteInterface obj = (MyRemoteInterface)Activator.GetObject(
            typeof(MyRemoteInterface), reference);

            // The GetManuallyMarshaledObject() method uses RemotingServices.Marshal()
            // to create an ObjRef object for a SampleTwo object.
            ObjRef objRefSampleTwo = obj.GetManuallyMarshaledObject();

            SampleTwo objectSampleTwo = (SampleTwo)RemotingServices.Unmarshal(objRefSampleTwo);
        }
    }
}
