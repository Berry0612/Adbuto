import socket
import json

data = {
    "AppName": "TestApp",
    "Title": "測試標題",
    "Text": "這是一則測試內容",
    "BigText": "這是詳細的測試內容...",
    "Timestamp": 12345678
}

try:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect(('127.0.0.1', 6100))
    s.send(json.dumps(data).encode('utf-8'))
    s.close()
    print("發送成功！請查看電腦右下角。")
except Exception as e:
    print(f"失敗: {e}")