namespace PCLinkNative.Models
{
    // F-04: 對應 Android 端傳來的 JSON 格式
    public class NotificationData
    {
        public string PackageName { get; set; } = string.Empty;
        public string AppName { get; set; } = "App";
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string BigText { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }
}