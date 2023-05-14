using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

using UnityEngine.UI;

public class GamingUI : MonoBehaviour, INeedSave, IPauseable
{
    public InputAction pauseKey;
    public InputAction phoneKey;
    public InputAction timeKey;
    [Tooltip("与物体交互用的按键键")]
    public InputAction interactKey;
    [HideInInspector]
    public string interactKeyName;

    // UI
    public TMP_Text timeText;
    public TMP_Text autoSaveText;
    public TMP_Text keyHintText;
    public StarterAssets.ClueUI  clueUI;

    public Image phone;
    public Image pauseDialog;
    public CanvasGroup sleepObj;

    private GameData.Cycle _cycle = new();
    private float _timeScale = 1;
    private bool _cycleEnd;

    private Coroutine _autoSaveAnimation; // 自动保存图标闪烁动画
    private bool _shouldStopAutoSaveAnimation;

    private bool _isPaused;
    
    // 当前正在看的线索
    private int _currentClueId = -1;
    
    // 线索页面显示事件
    [FormerlySerializedAs("onCluePageShowOrHide")] public UnityEvent<bool> pausedWithoutTime;
    
    // 用来显示炸弹的相机
    public GameObject bombUI;
    private bool _lookingAtBomb;
    private ObjectSelector _objectSelectorSelector;

    // Start is called before the first frame update
    void Start()
    {
        autoSaveText.alpha = 0;
        // 获取到物体选取者对象
        _objectSelectorSelector = GetComponent<ObjectSelector>();
        // 将按键名称暴露出去
        interactKeyName = interactKey.bindings[0].path["<keyboard>/".Length..].ToUpper();
        // 加载存档数据
        GameSaveManager.instance.LoadGame();
        pauseDialog.gameObject.SetActive(false);
        phone.gameObject.SetActive(false);
        keyHintText.gameObject.SetActive(false);
        clueUI.gameObject.SetActive(false);
        bombUI.SetActive(false);
    }

    public void OnExitClick(bool exitToDesktop)
    {
        // 保存游戏
        GameSaveManager.instance.SaveGame(() =>
        {
            SceneManager.LoadScene("StartScreen");
            SceneManager.UnloadScene("Playground");
        });
        if (!exitToDesktop) return;
        Application.Quit();
    }

    private void CheckPauseKeyPressed()
    {
        if (!pauseKey.WasPressedThisFrame()) return;
        // 如果线索界面存在就先关掉线索界面
        if (clueUI.gameObject.activeSelf)
        {
            clueUI.gameObject.SetActive(false);
            pausedWithoutTime?.Invoke(false);
            return;
        }
        // 如果手机界面存在就先关掉手机界面
        if (phone.gameObject.activeSelf)
        {
            phone.gameObject.SetActive(false);
            pausedWithoutTime?.Invoke(false);
            return;
        }
        // 如果正在查看炸弹，就返回主相机
        if (bombUI.activeSelf)
        {
            bombUI.SetActive(false);
            pausedWithoutTime?.Invoke(false);
            _objectSelectorSelector.enabled = true;
            return;
        }
        
        GameObject o;
        (o = pauseDialog.gameObject).SetActive(!pauseDialog.gameObject.activeSelf);
        PauseOrResumeAllObject(o.activeSelf);
    }

    private void PauseOrResumeAllObject(bool pause)
    {
        var pauseables = FindObjectsOfType<MonoBehaviour>(true).OfType<IPauseable>();
        foreach (var pauseable in pauseables)
        {
            pauseable.PauseOrResume(pause);
        }
    }

    private void CheckPhoneKeyPressed()
    {
        // TODO: 动画
        if (!phoneKey.WasPressedThisFrame()) return;
        if (pauseDialog.gameObject.activeSelf) return;
        GameObject o;
        (o = phone.gameObject).SetActive(!phone.gameObject.activeSelf);
        PauseOrResumeAllObject(o.activeSelf);
    }

#if DEVELOPMENT_BUID || UNITY_EDITOR
    private void CheckTimeSpeedKeyPressed()
    {
        if (!timeKey.WasPressedThisFrame()) return;
        if (Math.Abs(_timeScale - 5f) == 0)
        {
            _timeScale = 1f;
            return;
        }

        if (Math.Abs(_timeScale - 1f) == 0)
        {
            _timeScale = 5f;
        }
    }
#endif

    private void CheckNeedSave()
    {
        if (_cycle.time < 60) return;
        _cycleEnd = true;
        CycleEnd(false);
    }
    
    // 检查交互键是否按下
    private void CheckInteractKeyPressed()
    {
        if (!interactKey.WasPressedThisFrame()) return;
        if (pauseDialog.gameObject.activeSelf) return;
        if (phone.gameObject.activeSelf) return;
        if (_currentClueId != -1)
        {
            // 触发事件，让玩家控制器暂停响应，但是时间继续流逝
            pausedWithoutTime?.Invoke(true);
            // 显示线索界面
            clueUI.clueId = _currentClueId;
            clueUI.gameObject.SetActive(true);   
        } else if (_lookingAtBomb)
        {
            // 触发事件，让玩家控制器暂停响应，但是时间继续流逝
            pausedWithoutTime?.Invoke(true);
            bombUI.SetActive(true);
            ShowOrHideKeyHintForBomb(false);
            // 禁用主画布的物体选择器
            _objectSelectorSelector.enabled = false;
        }
    }

    private void Update()
    {
        if (_cycleEnd)
        {
            return;
        }
        _cycle.time += Time.deltaTime * _timeScale;
        // ReSharper disable once SpecifyACultureInStringConversionExplicitly
        timeText.text = _cycle.time.ToString();
        CheckNeedSave();
        
        CheckPauseKeyPressed();
        CheckPhoneKeyPressed();
#if DEVELOPMENT_BUID || UNITY_EDITOR
        CheckTimeSpeedKeyPressed();
#endif
        CheckInteractKeyPressed();
    }

    private void OnEnable()
    {
        pauseKey.Enable();
        phoneKey.Enable();
        timeKey.Enable();
        interactKey.Enable();
    }

    private void OnDisable()
    {
        pauseKey.Disable();
        phoneKey.Disable();
        timeKey.Disable();
        interactKey.Disable();
    }

    private IEnumerator AutoSaveAnimation()
    {
        // while (true)
        // {
        //     yield return StartCoroutine(GsapLike.FromTo(() => autoSaveText.alpha, a => autoSaveText.alpha = a, 0, 1,
        //         0.25f));
        //     yield return StartCoroutine(GsapLike.FromTo(() => autoSaveText.alpha, a => autoSaveText.alpha = a, 1, 0,
        //         0.25f));
        // }
        yield return StartCoroutine(GsapLike.FromToCycle(() => autoSaveText.alpha, a => autoSaveText.alpha = a, 1,
            0, 0.25f, () => _shouldStopAutoSaveAnimation));
    }

    private void AfterSave()
    {
        // 重置所有可以被重置的物件
        var needResets = FindObjectsOfType<MonoBehaviour>(true).OfType<INeedReset>();
        foreach (var needReset in needResets)
        {
            needReset.Reset();
        }
        StartCoroutine(GsapLike.FromTo(() => sleepObj.alpha, a => sleepObj.alpha = a, 1, 0, 2f));
        // 禁用炸弹相机
        bombUI.SetActive(false);
        _objectSelectorSelector.enabled = true;
        // 继续所有物件
        PauseOrResumeAllObject(false);
    }

    public void CycleEnd(bool manually)
    {
        _cycleEnd = true;
        _cycle.isManuallyEnd = manually;
        // 关闭线索界面和手机界面
        clueUI.gameObject.SetActive(false);
        phone.gameObject.SetActive(false);
        // 暂停所有物件
        PauseOrResumeAllObject(true);
        // 显示渐变动画
        StartCoroutine(GsapLike.FromTo(() => sleepObj.alpha, a => sleepObj.alpha = a, 0, 1, 2f, () =>
        {
            if (GameSaveManager.instance != null)
            {
                // 开始显示保存图标
                if (!manually) _autoSaveAnimation = StartCoroutine(AutoSaveAnimation());
                GameSaveManager.instance.SaveGame(() =>
                {
                    // 停止显示保存图标
                    _shouldStopAutoSaveAnimation = true;
                    _cycleEnd = false;
                    AfterSave();
                });
                return;
            }
            AfterSave();
        }));
    }

    public void SaveData(ref GameData gd)
    {
        if (_cycleEnd)
        {
            gd.experiencedCycles.Add(_cycle);
            _cycle = new GameData.Cycle();
        }

        gd.currentCycle = _cycle;
    }

    public void LoadData(GameData gd)
    {
        _cycle = gd.currentCycle;
    }

    public void PauseOrResume(bool pause)
    {
        _isPaused = pause;
        // 仅在暂停界面显示的时候暂停时间流逝
        _timeScale = _isPaused && pauseDialog.gameObject.activeSelf ? 0 : 1;
    }

    private void ShowOrHideKeyHint(bool show)
    { 
        keyHintText.gameObject.SetActive(show);
    }

    public void ShowOrHideKeyHintForBomb(bool show)
    {
        ShowOrHideKeyHint(show);
        _lookingAtBomb = show;
    }

    /// <summary>
    /// 当视线看向线索时显示按键提示，并且在显示按键提示时按下按键的话可以进入线索界面。
    /// 当视线从线索中移开时隐藏按键提示。
    /// </summary>
    /// <param name="id">线索 ID</param>
    /// <param name="show">显示还是隐藏</param>
    public void ShowOrHideClueDetailKeyHint(int id, bool show)
    {
        ShowOrHideKeyHint(show);
        if (!show)
        {
            _currentClueId = -1;
            return;
        }

        _currentClueId = id;
    }

    public void OnObjectSelected(GameObject obj, bool selected)
    {
        var objName = obj.name;
        if (objName.StartsWith("Clue"))
        {
            var id = int.Parse(objName["Clue".Length..]);
            ShowOrHideClueDetailKeyHint(id, selected);
            return;
        }

        if (objName == "Bomb")
        {
            ShowOrHideKeyHintForBomb(selected);
        }
    }
}