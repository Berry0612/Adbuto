package com.example.adbuto

import android.service.notification.NotificationListenerService
import android.service.notification.StatusBarNotification
import android.app.Notification
import android.util.Log
import org.json.JSONObject
import java.io.*
import java.net.InetAddress      // 修正：加入此行
import java.net.InetSocketAddress // 修正：加入此行
import java.net.Socket            // 修正：加入此行
import android.content.Intent
import androidx.localbroadcastmanager.content.LocalBroadcastManager
/*-------------------------------------------------------------------------*/
class NotificationService : NotificationListenerService() {
    /*-------------------------------------------------------------------------*/
    override fun onListenerConnected() {
        super.onListenerConnected()
        // 當 Service 啟動且連上系統通知服務時
        broadcastStatus(serviceRunning = true, log = "Service Connected")
    }

    override fun onListenerDisconnected() {
        super.onListenerDisconnected()
        broadcastStatus(serviceRunning = false, log = "Service Disconnected")
    }

    // 輔助函式：發送廣播給 MainActivity 更新 UI
    private fun broadcastStatus(serviceRunning: Boolean? = null, pcConnected: Boolean? = null, log: String? = null) {
        val intent = Intent("com.example.adbuto.STATUS_UPDATE")
        serviceRunning?.let { intent.putExtra("service_running", it) }
        pcConnected?.let { intent.putExtra("pc_connected", it) }
        log?.let { intent.putExtra("log_msg", it) }
        // 指定 Package 以確保安全性 (對應 MainActivity 的 RECEIVER_NOT_EXPORTED)
        intent.setPackage(packageName)
        sendBroadcast(intent)
    }
    /*-------------------------------------------------------------------------*/
    override fun onNotificationPosted(sbn: StatusBarNotification) {
        // 1. 取得通知內容
        val extras = sbn.notification.extras
        val title = extras.getString(Notification.EXTRA_TITLE) ?: "No Title"
        val text = extras.getString(Notification.EXTRA_TEXT) ?: ""
        val bigText = extras.getString(Notification.EXTRA_BIG_TEXT) ?: text

        // 取得 App 名稱 (例如 Line, Gmail)
        val pm = packageManager
        val appName = try {
            pm.getApplicationLabel(pm.getApplicationInfo(sbn.packageName, 0)).toString()
        } catch (e: Exception) {
            sbn.packageName
        }

        // 過濾掉不重要的系統通知或自己發出的通知
        if (sbn.packageName == "android" || sbn.packageName == packageName) return

        Log.d("PC-Link", "Notification received: $appName - $title")

        // 2. 開啟執行緒發送資料 (網路動作不能在主執行緒做)
        Thread {
            sendToPC(appName, title, bigText.toString())
        }.start()
    }

    private fun sendToPC(app: String, title: String, content: String) {
        var socket: Socket? = null
        try {
            // 使用 InetAddress 確保解析正確
            val serverAddr = InetAddress.getByName("127.0.0.1")
            socket = Socket()
            // ★★★ 新增：回報連線成功 ★★★
            broadcastStatus(pcConnected = true, log = "Sent: $app")

            // 設定連線逾時，避免阻塞
            socket.connect(InetSocketAddress(serverAddr, 6100), 2000)

            val json = JSONObject()
            json.put("AppName", app)
            json.put("Title", title)
            json.put("Text", content)
            json.put("BigText", content)
            json.put("Timestamp", System.currentTimeMillis())

            val writer = PrintWriter(BufferedWriter(OutputStreamWriter(socket.getOutputStream(), "UTF-8")))
            writer.print(json.toString())
            writer.flush()
            Log.d("PC-Link", "Sent to PC successfully: $app")

        } catch (e: Exception) {
            // 這裡會捕捉到你看到的 EPERM 錯誤
            broadcastStatus(pcConnected = false, log = "Fail: ${e.message}")
            Log.e("PC-Link", "Connection failed: ${e.message}")
        } finally {
            socket?.close()
        }
    }
}
