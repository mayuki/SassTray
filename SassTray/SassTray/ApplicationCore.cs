using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SassTray.Properties;

namespace SassTray
{
    public class ApplicationCore : ApplicationContext
    {
        private NotifyIcon _notifyIcon = new NotifyIcon();
        private ContextMenu _contextMenu = new ContextMenu();
        private Mutex _mutex;
        private Server _server;
        private Control _control;

        private IDictionary<String, Watcher> _watchTargets = new Dictionary<String, Watcher>(StringComparer.OrdinalIgnoreCase);

        public ApplicationCore(Mutex mutex, String[] args)
        {
            _mutex = mutex;
            _server = Server.Start(this);
            _control = new Control();
            var handle = _control.Handle;

            // Notify Icon
            _notifyIcon.Text = "Sass";
            _notifyIcon.ContextMenu = _contextMenu;
            _notifyIcon.Visible = true;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SassTray.SassTray.ico"))
            {
                _notifyIcon.Icon = new Icon(stream);
            }

            // Events
            Application.ApplicationExit += OnApplicationExit;

            StartWatch(args);
        }

        public void StartWatch(String[] watchPaths)
        {
            foreach (var watchPath in watchPaths.Where(x => Directory.Exists(x) || File.Exists(x)))
            {
                if (!String.IsNullOrWhiteSpace(watchPath))
                {
                    StartWatch(watchPath);
                }
            }
        }
        public void StartWatch(String watchPath)
        {
            InvokeInUIThread(() =>
            {
                // start watch
                if (_watchTargets.ContainsKey(watchPath)) return;

                var watcher = new Watcher(watchPath);
                watcher.Started += WatcherStarted;
                watcher.Stopped += WatcherStopped;
                watcher.Error += WatcherError;
                _watchTargets[watchPath] = watcher;

                watcher.Start();
            });
        }

        private void WatcherError(object sender, Watcher.WatcherEventArgs e)
        {
            var targetPath = (sender as Watcher).TargetPath;
            _notifyIcon.ShowBalloonTip(3000, "Sass", e.Detail, ToolTipIcon.Error);
        }

        private void WatcherStarted(object sender, Watcher.WatcherEventArgs e)
        {
            _notifyIcon.ShowBalloonTip(3000, "Sass", "Start watching for changes.\r\n" + e.Detail, ToolTipIcon.Info);
            UpdateMenu();
        }

        private void WatcherStopped(object sender, Watcher.WatcherEventArgs e)
        {
            var targetPath = (sender as Watcher).TargetPath;
            var watcher = _watchTargets[targetPath];
            watcher.Started -= WatcherStarted;
            watcher.Stopped -= WatcherStopped;
            watcher.Error -= WatcherError;
            _watchTargets.Remove(e.Detail);

            _notifyIcon.ShowBalloonTip(3000, "Sass", "Stopped watching for changes.\r\n" + targetPath, ToolTipIcon.Info);

            UpdateMenu();
        }

        private void UpdateMenu()
        {
            _contextMenu.MenuItems.Clear();

            _contextMenu.MenuItems.AddRange(
                _watchTargets.Values.Select(x =>
                {
                    var menuItem = new MenuItem(x.TargetPath);
                    menuItem.MenuItems.Add(new MenuItem("&View Output Log", MenuItemOnViewLogClick) { Tag = x.TargetPath });
                    menuItem.MenuItems.Add(new MenuItem("&Stop", MenuItemOnStopClick) { Tag = x.TargetPath });
                    return menuItem;
                }).ToArray()
            );

            _contextMenu.MenuItems.Add(new MenuItem("-"));
            _contextMenu.MenuItems.Add(new MenuItem("&Watch Folder...", MenuItemOnWatchFolderClick));
            _contextMenu.MenuItems.Add(new MenuItem("-"));
            _contextMenu.MenuItems.Add(new MenuItem("E&xit", MenuItemOnExitClick));
        }

        private void MenuItemOnWatchFolderClick(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog() { Description = "Select a target folder" })
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    StartWatch(folderBrowserDialog.SelectedPath);
                }
            }
        }
        private void MenuItemOnStopClick(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null || !_watchTargets.ContainsKey(menuItem.Tag as String))
                return;

            var watcher = _watchTargets[menuItem.Tag as String];
            watcher.Stop();
        }
        private void MenuItemOnViewLogClick(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null || !_watchTargets.ContainsKey(menuItem.Tag as String))
                return;

            var watcher = _watchTargets[menuItem.Tag as String];
            watcher.ShowLog();
        }
        private void MenuItemOnExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Close()
        {
            foreach (var watcher in _watchTargets.Values)
            {
                watcher.Stopped -= WatcherStopped;
                watcher.Stop();
            }
            _notifyIcon.Visible = false;

            _mutex.ReleaseMutex();
        }

        private void OnApplicationExit(Object sender, EventArgs args)
        {
            Close();
        }

        public void InvokeInUIThread(Action action)
        {
            if (_control.InvokeRequired)
            {
                _control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
