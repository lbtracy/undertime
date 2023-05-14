using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

public class GameSaveManager : MonoBehaviour
{
    // 单例
    public static GameSaveManager instance;
    public readonly string gameSaveIdentifier = "UnderTime";
    
    // 物品解锁提示
    public UnlockHintStack unlockHintStack;

    // 测试时用于快速解锁线索的按钮
    public InputAction quickUnlockClueKey;
    // 测试时用于快速解锁联系人的按钮
    public InputAction quickUnlockContactKey;

    // 已定义线索列表
    public List<StaticData.ClueInfo> definedClues;
    // 已定义联系人列表
    public List<StaticData.Contact> definedContacts;

    // 游戏存档
    [HideInInspector] public GameData gameData = new();

    private void Awake()
    {
        instance = this;
        // 阻止销毁
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// 检查是否已经有存档
    /// </summary>
    public bool IsGameExists()
    {
        return BayatGames.SaveGameFree.SaveGame.Exists(gameSaveIdentifier);
    }

    /// <summary>
    /// 新建游戏并且保存
    /// </summary>
    public void NewGame()
    {
        gameData = new GameData();
        BayatGames.SaveGameFree.SaveGame.Save(gameSaveIdentifier, gameData);
    }

    public void LoadGame()
    {
        gameData = BayatGames.SaveGameFree.SaveGame.Load<GameData>(gameSaveIdentifier);
        if (gameData == null)
        {
            Debug.LogWarning("No game data loaded.");
        }

        // 加载数据到所有游戏对象
        var needSaves = FindObjectsOfType<MonoBehaviour>(true).OfType<INeedSave>();
        foreach (var needSave in needSaves)
        {
            needSave.LoadData(gameData);
            Debug.Log($"Saved game data applied: {((MonoBehaviour)needSave).gameObject.name}");
        }
    }

    public void SaveGame(Action onSaved)
    {
        // 寻找所有的可保存对象
        var needSaves = FindObjectsOfType<MonoBehaviour>(true).OfType<INeedSave>();
        foreach (var needSave in needSaves)
        {
            // 保存数据
            needSave.SaveData(ref gameData);
            Debug.Log($"Game data saved: {((MonoBehaviour)needSave).gameObject.name}");
        }

        // 调用存储
        BayatGames.SaveGameFree.SaveGame.Save("UnderTime", gameData);
        onSaved?.Invoke();
    }

    /// <summary>
    /// 解锁线索
    /// </summary>
    /// <param name="id">线索序号</param>
    public void UnlockClue(int id)
    {
        // 不能重复解锁线索
        if (gameData.collectedClues.Exists(c => c.id == id)) return;
        gameData.collectedClues.Add(new GameData.Clue
        {
            cycleId = gameData.currentCycle.id,
            cycleTime = gameData.currentCycle.time,
            id = id
        });
        // 找到线索墙对象
        var cluesWall = GameObject.FindWithTag("CluesWall");
        if (cluesWall == null)
        {
            Debug.LogWarning("You need call this function in MainScene");
            return;
        }

        var wallScript = cluesWall.GetComponent<CluesWall>();
        wallScript.AddClue(id);
        
        // UI 显示
        unlockHintStack.ShowNewHint(StarterAssets.UnlockedHintUI.ItemType.Clue, id);
    }

    /// <summary>
    /// 解锁新的联系人
    /// </summary>
    /// <param name="id">联系人序号</param>
    public void UnlockContact(int id)
    {
        // 不能数重复解锁联人
        if (gameData.collectedContacts.Exists(c => c.id == id)) return;
        gameData.collectedContacts.Add(new GameData.Contact
        {
            id = id,
            cycleId = gameData.currentCycle.id,
            cycleTime = gameData.currentCycle.time
        });
        // 找到手机消息界面
        var messageList = Utils.FindObjectWithTag("PhoneUI", true);
        if (messageList == null)
        {
            Debug.LogWarning("You need call this function in MainScene");
            return;
        }

        var phoneScript = messageList.GetComponent<PhoneUI>();
        phoneScript.AddMessageItem(id, "", true);
        
        // UI 显示
        unlockHintStack.ShowNewHint(StarterAssets.UnlockedHintUI.ItemType.Contact, id);
    }

    #region OnEnableAndOnDisable
    private void OnEnable()
    {
#if DEVELOPMENT_BUID || UNITY_EDITOR
        quickUnlockClueKey.Enable();
        quickUnlockClueKey.performed += _ =>
        {
            if (gameData.collectedClues.Count == instance.definedClues.Count)
            {
                return;
            }

            UnlockClue(gameData.collectedClues.Count);
        };

        quickUnlockContactKey.Enable();
        quickUnlockContactKey.performed += _ =>
        {
            if (gameData.collectedContacts.Count == instance.definedContacts.Count)
            {
                return;
            }

            UnlockContact(gameData.collectedContacts.Count);
        };
#endif
    }

    private void OnDisable()
    {
#if DEVELOPMENT_BUID || UNITY_EDITOR
        quickUnlockClueKey.Disable();
        quickUnlockContactKey.Disable();
#endif
    }
    #endregion

    public class Utils
    {
        public static GameObject FindObjectWithTag(string tag, bool includeInactive = false)
        {
            if (!includeInactive) return GameObject.FindWithTag(tag);
            var objects = FindObjectsOfType<GameObject>(true);
            var obj = objects.ToList().Find(it => it.CompareTag(tag));
            return obj;
        }
    }
}