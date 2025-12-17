using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PCLinkNative.Models;

namespace PCLinkNative.Core
{
    public class SocketServer
    {
        private TcpListener _listener;
        private bool _isRunning;
        private const int PORT = 6100;

        // 當收到有效通知時觸發事件
        public event Action<NotificationData>? OnNotificationReceived;

        public async void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new TcpListener(IPAddress.Any, PORT);
                _listener.Start();
                _isRunning = true;

                // 異步監聽迴圈
                await Task.Run(async () =>
                {
                    while (_isRunning)
                    {
                        try
                        {
                            var client = await _listener.AcceptTcpClientAsync();
                            _ = HandleClientAsync(client); // 每個連線獨立處理
                        }
                        catch { /* Listener stopped */ }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Server Error] {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                // 讀取傳來的 JSON 字串
                string json = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    // F-04: 解析 JSON
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<NotificationData>(json, options);

                    if (data != null)
                    {
                        OnNotificationReceived?.Invoke(data);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Client Error] {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}