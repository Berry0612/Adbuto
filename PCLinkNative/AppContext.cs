using Microsoft.Win32; // 1. 新增這個引用，用於存取註冊表
using PCLinkNative.Core;
using CommunityToolkit.WinUI.Notifications;

namespace PCLinkNative
{
    public class MyApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private AdbManager _adbManager;
        private SocketServer _server;

        // 定義應用程式名稱，用於註冊表識別
        private const string APP_NAME = "PCLinkNative";

        public MyApplicationContext()
        {
            InitializeTrayIcon();

            _adbManager = new AdbManager();
            _server = new SocketServer();

            _adbManager.OnConnectionStatusChanged += OnDeviceStatusChanged;
            _server.OnNotificationReceived += (data) =>
            {
                NotificationHelper.ShowToast(data);
            };

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                System.Diagnostics.Debug.WriteLine($"Toast Action: {toastArgs.Argument}");
            };

            _adbManager.StartMonitoring();
            _server.Start();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "PC-Link Native (Waiting...)"
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            // 狀態顯示 (Index 0)
            //menu.Items.Add("Status: Disconnected", null).Enabled = false;

            //menu.Items.Add("-");

            // ★★★ 新增功能：USB 網路分享開關 ★★★
            ToolStripMenuItem tetherItem = new ToolStripMenuItem("Enable USB Tethering");
            tetherItem.CheckOnClick = true; // 設定為點擊會自動打勾/取消打勾
            tetherItem.Click += (s, e) =>
            {
                // 取得點擊後的狀態 (已打勾 or 未打勾)
                bool enable = tetherItem.Checked;

                // 呼叫 AdbManager 執行指令
                _adbManager.SetUsbTethering(enable);

                // 顯示一個小氣泡提示使用者正在切換
                string statusMsg = enable ? "Enabling RNDIS..." : "Disabling RNDIS...";
                _trayIcon.ShowBalloonTip(1000, "PC-Link", statusMsg, ToolTipIcon.Info);
            };
            menu.Items.Add(tetherItem);
            menu.Items.Add("-");

            // 2. 新增「開機自動啟動」選項
            ToolStripMenuItem startupItem = new ToolStripMenuItem("Run on Startup");
            startupItem.Checked = IsStartupEnabled(); // 檢查目前狀態
            startupItem.Click += (s, e) =>
            {
                // 切換狀態
                bool newState = !startupItem.Checked;
                SetStartup(newState);
                startupItem.Checked = newState;
            };
            menu.Items.Add(startupItem);

            menu.Items.Add("-");
            menu.Items.Add("Clear History", null, (s, e) => NotificationHelper.ClearAll());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon.ContextMenuStrip = menu;
        }

        // 3. 檢查註冊表，判斷是否已設定開機啟動
        private bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key.GetValue(APP_NAME) != null;
            }
        }

        // 4. 設定或移除開機啟動
        private void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        // 寫入當前執行檔的路徑
                        key.SetValue(APP_NAME, Application.ExecutablePath);
                    }
                    else
                    {
                        // 移除設定
                        key.DeleteValue(APP_NAME, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定失敗: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceStatusChanged(bool isConnected)
        {
            _trayIcon.Invoke((MethodInvoker)delegate
            {
                _trayIcon.Text = isConnected ? "PC-Link: Connected" : "PC-Link: Searching...";
                _trayIcon.ContextMenuStrip.Items[0].Text = isConnected ? "Status: Connected" : "Status: Disconnected";
            });

            if (isConnected)
            {
                // 手機連上時，顯示一個安靜的通知告訴使用者服務已就緒
                NotificationHelper.ShowToast(new Models.NotificationData
                {
                    AppName = "System",
                    Title = "PC-Link Connected",
                    Text = "Service is ready via USB."
                });
            }
        }

        private void ExitApp()
        {
            _adbManager.Stop();
            _server.Stop();
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }

    public static class NotifyIconExtensions
    {
        public static void Invoke(this NotifyIcon icon, Delegate d)
        {
            if (Application.OpenForms.Count > 0)
                Application.OpenForms[0]?.Invoke(d);
        }
    }
}