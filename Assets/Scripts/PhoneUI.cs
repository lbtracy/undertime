using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PhoneUI : MonoBehaviour, INeedSave, INeedReset
{
    public GameObject messageList; // 消息列表整体
    public GameObject messageListContainer; // 消息列表项容器
    public ChatUI chatUI; // 聊天界面
    public GameObject messageItemPrefab; // 消息项目预制体

    public UnityEvent<GameData.Message> setChatUIContent; // 进入聊天界面

    private List<GameData.Message> _msg = new(); // 消息
    private readonly List<MessageItem> _msgItems = new(); // 消息列表项

    private void Start()
    {
        chatUI.gameObject.SetActive(false);
    }

    public void SaveData(ref GameData gd)
    {
        Debug.Log("Saving messages: " + _msg);
        gd.currentCycle.messages = new();
        gd.currentCycle.messages.AddRange(_msg);
    }

    public void LoadData(GameData gd)
    {
        _msg.AddRange(gd.currentCycle.messages);

        // 创建消息列表项对象
        foreach (var msg in _msg)
        {
            Debug.Log("Load messages!");
            if (msg.texts.Count <= 0)
            {
                AddMessageItem(msg.contactId, "");
                Debug.Log("Messages loaded! " + msg.contactId);
                continue;
            }
            var lastMsg = msg.texts.Last();
            AddMessageItem(msg.contactId, lastMsg.text);
            Debug.Log("Messages loaded! " + msg.contactId);
        }
    }

    /// <summary>
    /// 添加消息列表项
    /// </summary>
    /// <param name="id">联系人 ID</param>
    /// <param name="preview">预览消息文本，如果是新联系人，或者已重置，则为空</param>
    /// <param name="isNew">是否是新添加的联系人</param>
    public void AddMessageItem(int id, string preview, bool isNew = false)
    {
        Debug.Log("正在添加消息项：" + id + preview);
        if (isNew) _msg.Add(new GameData.Message{ contactId = id});
        var item = Instantiate(messageItemPrefab, messageListContainer.transform);
        var itemInUI = item.GetComponent<MessageItem>();
        itemInUI.SetData(id, preview);
        // 设置点击事件
        item.GetComponent<Button>().onClick.AddListener(() =>
        {
            // 设置聊天界面内容
            setChatUIContent?.Invoke(_msg.Find(it => it.contactId == id));
            Debug.Log("正在进入对话界面：" + _msg);
            // 打开聊天界面
            chatUI.gameObject.SetActive(true);
            messageList.SetActive(false);
        });
        _msgItems.Add(itemInUI);
    }

    public void Reset()
    {
        // 清空所有消息列表项
        _msgItems.ForEach(it => Destroy(it.gameObject));
        _msgItems.Clear();
        _msg.Clear();
        // 重新添加进去
        GameSaveManager.instance.gameData.collectedContacts.ForEach(it => AddMessageItem(it.id, "", true));
        // 关闭聊天界面
        chatUI.gameObject.SetActive(false);
        messageList.SetActive(true);
    }

    /// <summary>
    /// 在聊天界面点击返回时触发
    /// </summary>
    public void OnChatUIBack()
    {
        messageList.SetActive(true);
        chatUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// 在聊天界面发送消息时触发
    /// </summary>
    /// <param name="id"></param>
    /// <param name="texts"></param>
    /// <param name="lastMsg"></param>
    public void OnNewChatMessage(int id, GameData.Message.MessageText texts, string lastMsg)
    {
        var msg = _msg.Find(it => it.contactId == id);
        msg.texts.Add(texts);
        // 设置消息预览
        _msgItems.Find(it => it.id == id).SetData(id, texts.text);
        // 设置聊天界面内容
        setChatUIContent?.Invoke(msg);
        // 等待返回数据
        StartCoroutine(GetResponse(lastMsg, s =>
        {
            msg.texts.Add(new GameData.Message.MessageText
            {
                isReceived = true,
                text = s,
            });
            // 设置消息预览
            _msgItems.Find(it => it.id == id).SetData(id, s);
            setChatUIContent?.Invoke(msg);
        }));
    }
    
    /// <summary>
    /// 获取返回数据
    /// TODO: 向 AI 后端发送请求
    /// </summary>
    /// <param name="text">发送出去的文本</param>
    /// <returns></returns>
    private IEnumerator GetResponse(string text, Action<string> result)
    {
        yield return new WaitForSeconds(2f);
        result("回复啦！");
        yield return null;
    }
}