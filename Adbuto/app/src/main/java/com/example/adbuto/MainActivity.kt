package com.example.adbuto

import android.content.BroadcastReceiver
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.graphics.Color
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import java.text.SimpleDateFormat
import java.util.*

class MainActivity : AppCompatActivity() {

    private lateinit var tvPermStatus: TextView
    private lateinit var tvServiceStatus: TextView
    private lateinit var tvPcStatus: TextView
    private lateinit var tvLogs: TextView

    // 定義廣播接收器，用來接收 Service 的狀態回報
    private val statusReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            intent?.let {
                // 更新 Service 狀態燈
                if (it.hasExtra("service_running")) {
                    val isRunning = it.getBooleanExtra("service_running", false)
                    updateServiceStatus(isRunning)
                }
                // 更新 PC 連線狀態燈
                if (it.hasExtra("pc_connected")) {
                    val isConnected = it.getBooleanExtra("pc_connected", false)
                    updatePcStatus(isConnected)
                }
                // 更新 Log
                if (it.hasExtra("log_msg")) {
                    val msg = it.getStringExtra("log_msg") ?: ""
                    appendLog(msg)
                }
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        tvPermStatus = findViewById(R.id.tvPermStatus)
        tvServiceStatus = findViewById(R.id.tvServiceStatus)
        tvPcStatus = findViewById(R.id.tvPcStatus)
        tvLogs = findViewById(R.id.tvLogs)

        // 點擊權限文字跳轉設定頁
        tvPermStatus.setOnClickListener {
            if (!isNotificationServiceEnabled()) {
                startActivity(Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS))
            }
        }
    }

    override fun onResume() {
        super.onResume()
        checkPermission()
        // 註冊廣播接收器 (只在 App 開著時更新 UI)
        val filter = IntentFilter("com.example.adbuto.STATUS_UPDATE")
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(statusReceiver, filter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            registerReceiver(statusReceiver, filter)
        }

        // 觸發 Service 回報當前狀態 (如果 Service 活著)
        // 這裡做個簡單的檢查：如果權限有開，Service 理論上要活著
        if (isNotificationServiceEnabled()) {
            updateServiceStatus(true) // 暫時假設，Service 稍後會廣播覆蓋
        } else {
            updateServiceStatus(false)
        }
    }

    override fun onPause() {
        super.onPause()
        unregisterReceiver(statusReceiver)
    }

    private fun checkPermission() {
        if (isNotificationServiceEnabled()) {
            tvPermStatus.text = "[✅ 已授權]"
            tvPermStatus.setTextColor(Color.GREEN)
        } else {
            tvPermStatus.text = "[❌ 未授權 (點此開啟)]"
            tvPermStatus.setTextColor(Color.RED)
            appendLog("Warning: Notification permission missing!")
        }
    }

    private fun updateServiceStatus(isRunning: Boolean) {
        if (isRunning) {
            tvServiceStatus.text = "[🟢 執行中]"
            tvServiceStatus.setTextColor(Color.GREEN)
        } else {
            tvServiceStatus.text = "[🔴 已停止]"
            tvServiceStatus.setTextColor(Color.RED)
        }
    }

    private fun updatePcStatus(isConnected: Boolean) {
        if (isConnected) {
            tvPcStatus.text = "[🟢 已連線]"
            tvPcStatus.setTextColor(Color.GREEN)
        } else {
            tvPcStatus.text = "[⚪ 等待中...]"
            tvPcStatus.setTextColor(Color.LTGRAY)
        }
    }

    private fun appendLog(msg: String) {
        val time = SimpleDateFormat("HH:mm:ss", Locale.getDefault()).format(Date())
        val newLog = "> $time $msg\n${tvLogs.text}"
        tvLogs.text = newLog
    }

    private fun isNotificationServiceEnabled(): Boolean {
        val pkgName = packageName
        val flat = Settings.Secure.getString(contentResolver, "enabled_notification_listeners")
        return flat != null && flat.contains(pkgName)
    }
}