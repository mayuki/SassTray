using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            if (args.Length == 0 || String.IsNullOrWhiteSpace(args[0]) || !Directory.Exists(args[0]))
            {
                MessageBox.Show("フォルダが指定されていません。", "Sass Tray");
                return;
            }

            // 多重起動
            var mutex = new Mutex(false, "SassTray." + Convert.ToBase64String(Encoding.UTF8.GetBytes(args[0])));
            if (!mutex.WaitOne(0))
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApplicationLogic(args[0], mutex));
        }
    }

    class ApplicationLogic : ApplicationContext
    {
        private NotifyIcon _notifyIcon = new NotifyIcon();
        private ContextMenu _contextMenu = new ContextMenu(new[]
                                                               {
                                                                   new MenuItem("-Path-") { Enabled = false },
                                                                   new MenuItem("-"), 
                                                                   new MenuItem("E&xit", MenuItemOnExitClick),
                                                               });
        private LogViewForm _logViewForm = new LogViewForm();
        private Process _process;
        private String _watchPath;
        private Mutex _mutex;

        public ApplicationLogic(String watchPath, Mutex mutex)
        {
            _mutex = mutex;

            // Notify Icon
            _notifyIcon.Text = "Sass: " + (watchPath.Length > 50 ? watchPath.Substring(0, 47) + "..." : watchPath);
            _notifyIcon.Icon = new Form().Icon;
            _notifyIcon.Visible = true;
            _notifyIcon.MouseClick += NotifyIconOnMouseClick;
            _notifyIcon.ContextMenu = _contextMenu;

            _contextMenu.MenuItems[0].Text = watchPath;

            // LogView
            _logViewForm.Text = "Sass: " + watchPath;
            _logViewForm.Visible = false;

            // Events
            Application.ApplicationExit += OnApplicationExit;

            StartWatch(watchPath);
        }


        private static void MenuItemOnExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void NotifyIconOnMouseClick(object sender, MouseEventArgs mouseEventArgs)
        {
            _logViewForm.Show();
        }

        private void StartWatch(String watchPath)
        {
            _watchPath = watchPath;

            // start watch
            var rubyExePath = Path.Combine(Settings.Default.RubyBinDirPath, "ruby.exe");
            var sassPath = Path.Combine(Settings.Default.RubyBinDirPath, "sass");
            var arguments = String.Join(" ", new[]
                                                 {
                                                     sassPath,
                                                     "--watch",
                                                     Settings.Default.SassOptions,
                                                     watchPath.Replace('\\', '/')
                                                 }.Where(x => !String.IsNullOrWhiteSpace(x))
                                                 .Select(x => "\"" + x + "\""));

            var processStartInfo = new ProcessStartInfo(rubyExePath, arguments)
                                       {
                                           CreateNoWindow = true,
                                           WindowStyle = ProcessWindowStyle.Hidden,
                                           UseShellExecute = false,
                                           RedirectStandardOutput = true,
                                           WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                       };
            _process = Process.Start(processStartInfo);
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.BeginOutputReadLine();

            _notifyIcon.ShowBalloonTip(3000, "Sass", "Start watching for changes.\r\n" + watchPath, ToolTipIcon.Info);
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Data)) return;
            _logViewForm.AppendLine(e.Data);
        }

        private void Close()
        {
            _mutex.ReleaseMutex();

            _logViewForm.Close();
            _notifyIcon.Visible = false;
            _process.Kill();
            _process.WaitForExit(1000);
        }

        private void OnProcessExited(Object sender, EventArgs args)
        {
            Application.Exit();
        }
        private void OnApplicationExit(Object sender, EventArgs args)
        {
            Close();
        }
    }
}
