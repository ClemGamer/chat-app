using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using UserDict = System.Collections.Generic.Dictionary<int, string>;

/// <summary>
/// 用戶聊天客戶端
/// </summary>
public class ChatClient : MonoBehaviour
{
    /// <summary>
    /// 封裝好的WebSocket連線。
    /// </summary>
    WebSocket ws;
    /// <summary>
    /// 顯示的用戶名稱。
    /// </summary>
    new string name;
    /// <summary>
    /// 用戶列表，用id查詢用戶名稱。
    /// </summary>
    UserDict userDict = new();
    /// <summary>
    /// 連線用戶的id。
    /// </summary>
    [SerializeField] int id;
    /// <summary>
    /// 自己發送的訊息框。
    /// </summary>
    [SerializeField] MessageBlock myBlock;
    /// <summary>
    /// 對方發送的訊息框。
    /// </summary>
    [SerializeField] MessageBlock anotherBlock;
    /// <summary>
    /// 放置訊息框的列表。
    /// </summary>
    [SerializeField] Transform blockParent;
    /// <summary>
    /// 輸入訊息的欄位。
    /// </summary>
    [SerializeField] InputField inputField;
    /// <summary>
    /// 送出訊息的按鈕。
    /// </summary>
    [SerializeField] Button sendButton;
    /// <summary>
    /// 訊息框的捲動物件參考。
    /// </summary>
    [SerializeField] ScrollRect scrollRect;

    private void Awake()
    {
        // 建立封裝好的WebSocket連線。
        ws = new WebSocket($"ws://localhost:8080/ws/chat?id={this.id}");
    }

    void Start()
    {
        // 停用送出訊息按鈕。
        sendButton.interactable = false;
        // 設定送出訊息按鈕的監聽。
        sendButton.onClick.AddListener(Send);
        // 設定開啟連線時的回調。
        ws.OnOpen += () => {
            Debug.Log("OnOpen");
            // 送出取得用戶列表的請求。
            var p = new Protocal<string>
            {
                protocal = "0001",
                id = id,
                data = ""
            };
            var pJson = JsonUtility.ToJson(p);
            ws.Send(pJson, () =>
            {
                // 完成後啟用送出訊息按鈕，可以開始發送訊息。
                sendButton.interactable = true;
            });
        };
        // 設定連線關閉時的回調。
        ws.OnClose += () => { Debug.Log("OnClose"); };
        // 設定接收訊息的回調。
        ws.OnMessage += (msg) =>
        {
            Debug.Log("OnMessage");
            var m = JsonUtility.FromJson<Protocal<string>>(msg);
            switch (m.protocal)
            {
                // protocal "0001" 為接收用戶列表。
                case "0001":
                    Debug.Log(m.data);
                    var userStrings = m.data.Split(",");
                    foreach (var s in userStrings)
                    {
                        var user = s.Split(":");
                        userDict.Add(int.Parse(user[0]), user[1]);
                    }
                    Debug.Log(userDict);
                    this.name = userDict[this.id];
                    break;
                // protocal "0002" 為接收聊天訊息。
                case "0002":
                    if (m.id == this.id)
                    {
                        return;
                    }
                    AddMessageBlock(userDict[m.id], m.data, anotherBlock);
                    StartCoroutine(ScrollToBottom());
                    break;
            }

        };
        // 開啟WebSocket連線。
        ws.Open();
    }

    /// <summary>
    /// 捲動聊天室窗至最底部。
    /// </summary>
    /// <returns></returns>
    IEnumerator ScrollToBottom()
    {
        var b = scrollRect.verticalNormalizedPosition;
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = b;
    }

    /// <summary>
    /// 訊息內容協議，與Server事先協定好的結構。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    class Protocal<T>
    {
        public string protocal;
        public int id;
        public T data;
    }

    /// <summary>
    /// 添加訊息框並送出訊息
    /// </summary>
    public void Send()
    {
        sendButton.interactable = false;

        AddMessageBlock(name, inputField.text, myBlock);
        StartCoroutine(ScrollToBottom());

        var p = new Protocal<string>
        {
            protocal = "0002",
            id = id,
            data = inputField.text
        };
        var pJson = JsonUtility.ToJson(p);

        ws.Send(pJson, () =>
        {
            sendButton.interactable = true;
        });
    }

    /// <summary>
    /// 添加訊息框
    /// </summary>
    /// <param name="name"></param>
    /// <param name="msg"></param>
    /// <param name="block"></param>
    void AddMessageBlock(string name, string msg, MessageBlock block)
    {
        var b = Instantiate(block, blockParent);
        b.Init(name, msg);
        b.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        ws.Close();
    }
}
