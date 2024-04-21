using UnityEngine;
using UnityEngine.UI;

public class MessageBlock : MonoBehaviour
{
    /// <summary>
    /// 用戶名稱
    /// </summary>
    [SerializeField] new Text name;
    /// <summary>
    /// 訊息
    /// </summary>
    [SerializeField] Text msg;

    /// <summary>
    /// 設定用戶名稱及訊息
    /// </summary>
    /// <param name="name"></param>
    /// <param name="msg"></param>
    public void Init(string name, string msg)
    {
        this.name.text = name;
        this.msg.text = msg;
    }
}
