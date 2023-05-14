using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour
{
    private TMP_Dropdown _dropdown;
    public CanvasGroup saveTip;

    private Coroutine _showHideSaveTipCoroutine;
    
    // Start is called before the first frame update
    IEnumerator Start()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
        // 等待语言初始化并在函数结束后返回语言选项
        yield return LocalizationSettings.InitializationOperation;

        // 根据可用语言生成选项
        var options = new List<TMP_Dropdown.OptionData>();
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var selected = 0;
        for (int i = 0; i < locales.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(locales[i].LocaleName));
            // 将当前语言设置为已选中
            if (LocalizationSettings.SelectedLocale == locales[i])
            {
                selected = i;
            }
        }

        // 将选项应用至选择框
        _dropdown.options = options;
        _dropdown.value = selected;
        _dropdown.onValueChanged.AddListener(i => ApplyLanguage(locales[i]));
        Debug.Log("i18n loaded: " + options);
    }

    private void ApplyLanguage(Locale l)
    {
        LocalizationSettings.SelectedLocale = l;
        
        // save setting
        GameSettings.Instance.Language = l.LocaleName;
        if (_showHideSaveTipCoroutine != null)
        {
            StopCoroutine(_showHideSaveTipCoroutine);
        }
        _showHideSaveTipCoroutine = StartCoroutine(ShowSaveTipAnimation());
    }

    private IEnumerator ShowSaveTipAnimation()
    {
        yield return StartCoroutine(GsapLike.FromTo(() => saveTip.alpha, it => saveTip.alpha = it, 0, 1, 1));
        yield return StartCoroutine(GsapLike.FromTo(() => saveTip.alpha, it => saveTip.alpha = it, 1, 0, 1));
    }
}
