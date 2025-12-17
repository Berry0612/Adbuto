using System.Diagnostics;

namespace PCLinkNative.Core
{
    public class AdbManager
    {
        private const string ADB_PATH = "adb";
        private bool _isConnected = false;
        private CancellationTokenSource _cts;

        public event Action<bool>? OnConnectionStatusChanged;

        public void StartMonitoring()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool currentlyConnected = CheckDevice();

                // 情況 A: 裝置剛上線
                if (currentlyConnected && !_isConnected)
                {
                    _isConnected = true;
                    OnConnectionStatusChanged?.Invoke(true);

                    // 剛連線時，嘗試開啟 RNDIS (網路)
                    // 注意：有些手機開啟 RNDIS 會導致 USB 重置，所以通道建立放在下面
                    TryEnableRndis();
                }
                // 情況 B: 裝置剛斷線
                else if (!currentlyConnected && _isConnected)
                {
                    _isConnected = false;
                    OnConnectionStatusChanged?.Invoke(false);
                }

                // 情況 C (關鍵修正): 只要裝置是連線狀態，我們就持續維護通道
                // 無論使用者是否切換了網路分享，這行能確保通道永遠活著
                if (currentlyConnected)
                {
                    EnsurePortForwarding();
                }

                await Task.Delay(3000, token); // 每 3 秒檢查一次
            }
        }

        private bool CheckDevice()
        {
            try
            {
                var output = RunAdbCommand("devices");
                return output.Contains("\tdevice");
            }
            catch { return false; }
        }

        private void TryEnableRndis()
        {
            Task.Run(() =>
            {
                try
                {
                    // 嘗試切換為 RNDIS 模式 (這可能會導致 USB 短暫重置)
                    RunAdbCommand("shell svc usb setFunctions rndis");
                }
                catch { }
            });
        }

        // 這個函式會每 3 秒被呼叫一次，確保通道暢通
        private void EnsurePortForwarding()
        {
            try
            {
                // 為了效能，我們可以先檢查規則是否已經存在 (可選)，
                // 但直接執行 reverse 指令其實開銷很小，且能保證最強健壯性。
                // 這裡我們直接強制覆寫規則。
                RunAdbCommand("reverse tcp:6100 tcp:6100");

                // 如果您後來改用 LocalSocket，請保留這行：
                // RunAdbCommand("reverse localabstract:pclink tcp:6100");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADB Maintain Error] {ex.Message}");
            }
        }

        private string RunAdbCommand(string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = ADB_PATH,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                using var reader = process.StandardOutput;
                string result = reader.ReadToEnd();
                process.WaitForExit();
                return result;
            }
            catch
            {
                return "";
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { RunAdbCommand("reverse --remove-all"); } catch { }
        }

        // ★★★ 新增這個功能：控制 USB 網路分享 ★★★
        public void SetUsbTethering(bool enable)
        {
            Task.Run(() =>
            {
                try
                {
                    // "rndis" = 開啟網路分享
                    // "mtp" = 關閉 (切換回檔案傳輸模式，相當於關閉熱點)
                    string mode = enable ? "rndis" : "mtp";
                    Debug.WriteLine($"[ADB] Switching USB mode to: {mode}");

                    RunAdbCommand($"shell svc usb setFunctions {mode}");

                    // 如果是開啟，順便確保通道暢通
                    if (enable)
                    {
                        Thread.Sleep(2000); // 等待一下讓網卡生效
                        RunAdbCommand("reverse tcp:6100 tcp:6100");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tethering Error] {ex.Message}");
                }
            });
        }
    }
}