using CommunityToolkit.WinUI.Notifications;
using PCLinkNative.Models;


namespace PCLinkNative.Core
{
    public static class NotificationHelper
    {
        public static void ShowToast(NotificationData data)
        {
            // F-05: 建構 Windows 原生通知
            // 優先使用 BigText，若無則使用 Text
            string content = string.IsNullOrWhiteSpace(data.BigText) ? data.Text : data.BigText;

            new ToastContentBuilder()
                .AddArgument("action", "viewConversation") // 點擊通知本體的回調參數
                .AddArgument("conversationId", data.Timestamp)

                // Header (App Name)
                .AddText(data.AppName) // 第一行通常比較顯眼

                // Title & Body
                .AddText(data.Title)
                .AddText(content)

                // F-05: 設定場景為 Reminder，確保它會進入 Action Center 並且有聲音
                //.SetToastScenario(ToastScenario.Reminder)

                // F-06: 互動按鈕
                //.AddButton(new ToastButton()
                //    .SetContent("Open on Phone")
                //    .AddArgument("action", "openPhone")
                //    .SetBackgroundActivation()) // 背景執行，不彈出視窗
                //.AddButton(new ToastButtonDismiss("Dismiss"))

                .Show(); // 顯示通知
        }

        public static void ClearAll()
        {
            ToastNotificationManagerCompat.History.Clear();
        }
    }
}