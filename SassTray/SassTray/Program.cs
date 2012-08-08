using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SassTray.Properties;

namespace SassTray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            // 多重起動
            var mutex = new Mutex(false, "SassTray");
            if (!mutex.WaitOne(0))
            {
                var serverChannel = new IpcClientChannel();
                ChannelServices.RegisterChannel(serverChannel, true);
                var server = (Server)Activator.GetObject(typeof(Server), "ipc://SassTray/Server");
                server.StartWatch(args);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApplicationCore(mutex, args));
        }
    }
}
