using UnityEngine;
using System.Net.WebSockets;
using System;
using System.Threading;
using System.Text;

public class WebSocket
{
    /// <summary>
    /// 底層ClientWebSocket。
    /// </summary>
    ClientWebSocket ws;
    /// <summary>
    /// 收到訊息事件通道。
    /// </summary>
    public event Action<string> OnMessage;
    /// <summary>
    /// 開啟連線事件通道。
    /// </summary>
    public event Action OnOpen;
    /// <summary>
    /// 關閉連線事件通道。
    /// </summary>
    public event Action OnClose;
    /// <summary>
    /// 連線WebSocket的URI
    /// </summary>
    private Uri uri;
    /// <summary>
    /// 接收訊息的緩衝區
    /// </summary>
    private byte[] buffer;

    public WebSocket(string uri)
    {
        this.uri = new Uri(uri);
        this.ws = new ClientWebSocket();
        buffer = new byte[256];
    }

    /// <summary>
    /// 開啟連線
    /// </summary>
    public async void Open()
    {
        var task = ws.ConnectAsync(this.uri, CancellationToken.None);
        await task;
        if (task.IsFaulted)
        {
            Debug.LogError(task.Exception);
        }
        else
        {
            OnOpen?.Invoke();
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        OnClose?.Invoke();
                        break;
                    case WebSocketMessageType.Text:
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessage?.Invoke(message);
                        break;
                }
            }

        }
        Debug.Log("Am I stucked??");
    }

    /// <summary>
    /// 送出訊息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="finish"></param>
    public async void Send(string message, Action finish)
    {
        if (ws.State == WebSocketState.Open)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            finish?.Invoke();
        }
    }

    /// <summary>
    /// 關閉連線
    /// </summary>
    public void Close()
    {
        ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }

}
