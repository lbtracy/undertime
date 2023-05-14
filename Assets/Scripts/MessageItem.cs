using TMPro;
using UnityEngine;

public class MessageItem : MonoBehaviour, INeedReset
{
    public TMP_Text title;
    public TMP_Text preview;
    public int id; // 联系人 ID

    // 设置消息列表项内容
    public void SetData(int contactId, string p)
    {
        title.text = GameSaveManager.instance.definedContacts[contactId].displayName.GetLocalizedString();
        preview.text = p;
        // 保持当前消息列表项的联系人 ID 不变
        if (id == 0) id = contactId;
    }

    public void Reset()
    {
        // 清除消息预览
        preview.text = "";
    }
}