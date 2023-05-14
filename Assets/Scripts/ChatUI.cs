using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ChatUI : MonoBehaviour
{
    public UnityEvent<int, GameData.Message.MessageText, string> onNewMessage;

    public TMP_Text contactName;
    public TMP_InputField input;
    public TMP_Text chatContent;

    public UnityEvent<int, string> onSendMessage;

    private int _contactId;

    /// <summary>
    /// 发送按钮点击处理函数
    /// </summary>
    public void SendMessage()
    {
        chatContent.text += "\n你: " + input.text;
        onNewMessage?.Invoke(_contactId, new GameData.Message.MessageText
        {
            text = input.text
        }, input.text);
        input.text = "";
    }

    /// <summary>
    /// 设置聊天内容，将被事件触发
    /// </summary>
    public void SetMessages(GameData.Message message)
    {
        _contactId = message.contactId;
        // 设置对方名称
        contactName.text = GameSaveManager.instance.definedContacts[message.contactId].displayName.GetLocalizedString();
        chatContent.text = ""; // 重置消息内容

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        // 禁止 Resharper 提示，因为转换之后的语句很难懂，虽然行数变少了
        foreach (var text in message.texts)
        {
            var singleMsgText = text.isReceived ? contactName.text : "你";
            singleMsgText += ": " + text.text;
            // chatContent.text = singleMsgText + "\n" + chatContent.text;
            chatContent.text += singleMsgText + "\n";
        }
    }

    /// <summary>
    /// 当返回按钮点击时触发
    /// </summary>
    public void OnBackClicked()
    {
        input.text = ""; // 移除文本框中的消息
    }
}