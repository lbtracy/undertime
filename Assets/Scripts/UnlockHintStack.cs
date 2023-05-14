using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class UnlockHintStack : MonoBehaviour
{
    // 解锁提醒项预制体
    public GameObject unlockHintPrefab;

    // 解锁提醒显示队列
    private readonly Queue<UnlockedHintUI> _unlockHintQueue = new();

    // 显示新的提醒
    public void ShowNewHint(UnlockedHintUI.ItemType type, int id)
    {
        var description = "";
        switch (type)
        {
            case UnlockedHintUI.ItemType.Clue:
                description = GameSaveManager.instance.definedClues[id].title.GetLocalizedString();
                break;
            case UnlockedHintUI.ItemType.Contact:
                description = GameSaveManager.instance.definedContacts[id].displayName.GetLocalizedString();
                break;
            // TODO: 成就
        }

        var hint = Instantiate(unlockHintPrefab, transform).GetComponent<UnlockedHintUI>();
        hint.SetInfo(type, description);
        
        // 添加事件监听器
        hint.hintDestroyed.AddListener(OnHintDestroyed);
        // TODO: 设置位置
        // 放入提醒队列
        _unlockHintQueue.Enqueue(hint);
    }
    
    // 当提醒消失时触发
    private void OnHintDestroyed()
    {
        _unlockHintQueue.Dequeue();
    }
    
    // 阻止实例消失
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}