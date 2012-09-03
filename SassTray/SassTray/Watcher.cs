using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SassTray.Properties;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace SassTray
{
    class Watcher : IDisposable
    {
        private LogViewForm _logViewForm;
        private String _path;
        private Process _process;
        private String _lastError;

        public event EventHandler<WatcherEventArgs> Error;
        public event EventHandler<WatcherEventArgs> Started;
        public event EventHandler<WatcherEventArgs> Stopped;

        public Watcher(String path)
        {
            _path = path;
            _logViewForm = new LogViewForm() { Text = "Sass: " + _path, Visible = false };
        }

        public String TargetPath { get { return _path; } }

        public void Start()
        {
            var arguments = new List<String>();

            // load config
            var options = new String[0];
            var outputPath = "";
            if (File.Exists(Path.Combine(_path, "SassTray.config")))
            {
                SassTrayConfig config = SassTrayConfig.Load(Path.Combine(_path, "SassTray.config"));
                options = config.Options ?? options;
                if (!String.IsNullOrWhiteSpace(config.OutputPath))
                {
                    outputPath = Path.GetFullPath(Path.Combine(_path, config.OutputPath));
                }
            }

            // start watch
            var selfDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rubyExePath = Path.GetFullPath(Path.Combine(selfDirectory, Settings.Default.RubyBinDirPath, "ruby.exe"));
            var sassPath = Path.GetFullPath(Path.Combine(selfDirectory, Settings.Default.RubyBinDirPath, "sass"));
            arguments.Add(sassPath);
            arguments.Add("--watch");
            arguments.Add(Settings.Default.SassOptions);
            arguments.AddRange(options);
            arguments.Add(_path.Replace('\\', '/') + (String.IsNullOrWhiteSpace(outputPath) ? "" : ":"+outputPath.Replace('\\', '/'))); // inputpath or inputpath:outputpath

            var argumentsString = String.Join(" ", arguments.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => "\"" + x + "\""));
            var processStartInfo = new ProcessStartInfo(rubyExePath, argumentsString)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = selfDirectory
            };
            _process = Process.Start(processStartInfo);
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.BeginOutputReadLine();

            if (Started != null)
                Started(this, new WatcherEventArgs() { Detail = _path });
        }

        public void Stop()
        {
            if (_process == null) return;

            if (_logViewForm.InvokeRequired)
                _logViewForm.Invoke((Action)(() => _logViewForm.Close()));
            else
                _logViewForm.Close();

            if (!_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(1000);
            }

            _process = null;
            _logViewForm = null;

            if (Stopped != null)
                Stopped(this, new WatcherEventArgs() { Detail = _path });
        }

        public void ShowLog()
        {
            _logViewForm.Show();
        }

        private void OnProcessExited(Object sender, EventArgs args)
        {
            Stop();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Data)) return;

            _logViewForm.AppendLine(e.Data);

            if (e.Data.ToLower().Contains("error"))
            {
                // critical error ( on start )
                if (e.Data.StartsWith("ERROR:"))
                {
                    MessageBox.Show(e.Data, "SassTray: " + _path, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (_lastError != e.Data)
                {
                    _lastError = e.Data;
                    if (Error != null)
                        Error(this, new WatcherEventArgs { Detail = e.Data.Trim() });
                }
            }
            else
            {
                _lastError = null;
            }
        }

        public class WatcherEventArgs : EventArgs
        {
            public String Detail { get; set; }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
