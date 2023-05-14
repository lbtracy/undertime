using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public class GameData
{
    public class Cycle
    {
        public float time; // 已经过时间，以整数部分以秒计算
        public int id;
        public bool isManuallyEnd; // 是否是玩家主动结束循环
        public List<Message> messages = new(); // 玩家与 NPC 发的消息
        public Dictionary<int, bool> bombState = new(); // 已被拆除的炸弹部件（模拟）
    }

    public class Message
    {
        public struct MessageText
        {
            public string text; // 消息文本
            public bool isReceived; // 是否是收到的消息
        }
        public int contactId;
        public List<MessageText> texts = new(); // 消息文本
    }

    public struct Clue
    {
        public int id; // 编号
        public int cycleId; // 获得时的循环编号
        public float cycleTime; // 获得时的循环已经过时间
    }
    
    public struct Contact
    {
        public int id; // 编号
        public int cycleId; // 获得时的循环编号
        public float cycleTime; // 获奖时的循环已经过时间
    }

    public Vector3 playerPosition;
    public Quaternion playerCameraRotation;
    public List<Cycle> experiencedCycles = new(); // 已经历的循环数据
    public List<Clue> collectedClues = new(); // 已收集线索
    public List<Contact> collectedContacts = new(); // 已知晓联系人
    public Cycle currentCycle = new(); // 当前循环
}

public static class StaticData
{
    /// <summary>
    /// 线索信息
    /// </summary>
    [System.Serializable]
    public class ClueInfo
    {
        [Tooltip("在线索墙上的位置，请注意是 z 轴和 y 轴")] public Vector2 position;
        [Tooltip("在线索界面中显示的名称.")] public LocalizedString displayName;
        [Tooltip("在线索详情页面显示的标题")] public LocalizedString title;

        [Tooltip("在线索详情页面显示的内容，可以在其中使用 {image1} 这样的占位符来表示这里有一张图片， {image2} 就表示是第二张图片，图片资源单独在另一个字段中显示")]
        public LocalizedString content;

        [Tooltip("在线索界面中会出现的图片资源")] public LocalizedTexture images;
        [Tooltip("在墙上时的模型")] public GameObject prefabOnTheWall;
        [Tooltip("在详细信息中的模型")] public GameObject prefabDetail;
    }

    /// <summary>
    /// 联系人信息
    /// </summary>
    [System.Serializable]
    public class Contact
    {
        [Tooltip("联系人名称")]
        public LocalizedString displayName;
    }
}