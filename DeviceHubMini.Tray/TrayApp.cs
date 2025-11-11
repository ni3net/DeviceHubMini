using System;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;
using DeviceHubMini.Tray.Services;
using DeviceHubMini.Tray.Views;
using Timer = System.Windows.Forms.Timer;

namespace DeviceHubMini.Tray
{
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ServiceHelper _serviceHelper;
        private readonly Timer _statusTimer;

        private readonly Icon _iconRunning;
        private readonly Icon _iconStopped;
        private readonly Icon _iconError;

        public TrayApp()
        {
            string serviceName = "DeviceHubMiniService"; // Your actual Windows Service name
            _serviceHelper = new ServiceHelper(serviceName);

            _iconRunning = new Icon("Assets/icon-running.ico");
            _iconStopped = new Icon("Assets/icon-stopped.ico");
            _iconError = new Icon("Assets/icon-error.ico");

            // Build context menu
            var menu = new ContextMenuStrip();
            menu.Items.Add("Start Service", null, (_, _) => StartService());
            menu.Items.Add("Stop Service", null, (_, _) => StopService());
            menu.Items.Add("Restart Service", null, (_, _) => RestartService());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
            menu.Items.Add("Open Logs Folder", null, (_, _) => OpenLogsFolder());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitApp());

            _trayIcon = new NotifyIcon
            {
                Icon = _iconStopped,
                Visible = true,
                ContextMenuStrip = menu,
                Text = "DeviceHubMini Controller"
            };

            // Timer for auto-refresh
            _statusTimer = new Timer { Interval = 5000 };
            _statusTimer.Tick += (_, _) => UpdateStatus();
            _statusTimer.Start();

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            try
            {
                var status = _serviceHelper.GetStatus();
                _trayIcon.Text = $"DeviceHubMini Service: {status}";

                _trayIcon.Icon = status switch
                {
                    "Running" => _iconRunning,
                    "Stopped" => _iconStopped,
                    _ => _iconError
                };
            }
            catch
            {
                _trayIcon.Icon = _iconError;
                _trayIcon.Text = "DeviceHubMini: Service not found";
            }
        }

        private void StartService()
        {
            try
            {
                _serviceHelper.StartService();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start service:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopService()
        {
            try
            {
                _serviceHelper.StopService();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not stop service:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartService()
        {
            try
            {
                _serviceHelper.RestartService();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not restart service:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSettings()
        {
            using var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }

        private void OpenLogsFolder()
        {
            try
            {
                string logsPath = @"C:\ProgramData\DeviceHubMini\Logs";
                System.Diagnostics.Process.Start("explorer.exe", logsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open logs folder:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExitApp()
        {
            _statusTimer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
