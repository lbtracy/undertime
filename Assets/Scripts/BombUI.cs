using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BombUI : MonoBehaviour, INeedSave, INeedReset
{
    public InputAction removePartAction;
    public GameObject bombPrefab;
    public GameObject currentBomb;
    
    private int _selectedId;
    private GameObject _selectedBombPart;

    private Dictionary<int, bool> _bombState = new();

    private void Update()
    {
        CheckIfRemovePartActionPerformed();
    }

    private void CheckIfRemovePartActionPerformed()
    {
        if (!removePartAction.WasPressedThisFrame()) return;
        if (_selectedId == -1) return;
        Destroy(_selectedBombPart);
        _bombState.Add(_selectedId, false);
    }

    public void OnBombPartSelected(GameObject obj, bool selected)
    {
        if (!selected)
        {
            _selectedId = -1;
            _selectedBombPart = null;
        }

        _selectedId = int.Parse(obj.name["Bomb".Length..]);
        _selectedBombPart = obj;
    }

    private void OnEnable()
    {
        removePartAction.Enable();
    }

    private void OnDisable()
    {
        removePartAction.Disable();
    }

    public void SaveData(ref GameData gd)
    {
        gd.currentCycle.bombState = _bombState;
        foreach (var (key, value) in _bombState)
        {
            Debug.Log($"Saved data: Bomb{key} removed: {value}");
        }
    }

    public void LoadData(GameData gd)
    {
        _bombState = gd.currentCycle.bombState;
        // 根据已经保存的状态移除炸弹部件
        foreach (var (key, value) in _bombState)
        {
            foreach (var o in FindObjectsOfType<GameObject>())
            {
                if (o.name != $"Bomb{key}" || value) continue;
                Destroy(o);
                Debug.Log($"Loaded data: Bomb{key} removed");
            }
        }
    }

    public void Reset()
    {
        Destroy(currentBomb);
        currentBomb = Instantiate(bombPrefab, transform);
        _bombState = new();
    }
}
