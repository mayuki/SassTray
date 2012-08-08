using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;

namespace SassTray
{
    public class Server : MarshalByRefObject
    {
        private ApplicationCore _application;
        private Server _server;

        public Server(ApplicationCore applicationCore)
        {
            _application = applicationCore;
        }

        public void StartWatch(String[] paths)
        {
            _application.StartWatch(paths);
        }

        public void StartWatch(String path)
        {
            _application.StartWatch(path);
        }

        public static Server Start(ApplicationCore applicationCore)
        {
            var serverChannel = new IpcServerChannel("SassTray");
            ChannelServices.RegisterChannel(serverChannel, true);
            var server = new Server(applicationCore);
            RemotingServices.Marshal(server, "Server", typeof(Server));

            return server;
        }
    }
}
