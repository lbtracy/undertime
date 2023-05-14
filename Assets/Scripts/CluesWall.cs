using System.Collections.Generic;
using UnityEngine;

public class CluesWall : MonoBehaviour, INeedSave
{
    private readonly Dictionary<StaticData.ClueInfo, GameObject> _infos = new();
    public void SaveData(ref GameData gd) {}

    public void LoadData(GameData gd)
    {
        gd.collectedClues.ForEach(c => AddClue(c.id));
    }

    /// <summary>
    /// 在线索墙上添加一个线索
    /// </summary>
    /// <param name="id">线索序号</param>
    public void AddClue(int id)
    {
        var c = GameSaveManager.instance.definedClues[id];
        if (_infos.ContainsKey(c)) return; // 不允许在墙上创建重复的线索模型
        var go = Instantiate(c.prefabOnTheWall, transform);
        go.name = $"Clue{id}";
        go.transform.Translate(0, c.position.y, c.position.x);
        _infos.Add(c, go);
    }
}