using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;
    private readonly Dictionary<string, object> _settingsMap = new();
    private readonly Dictionary<string, Type> _settingTypes = new();
    
    private readonly string _keyLanguage = "language";
    private readonly string _keyResolutionWidth = "resolutionWidth";
    private readonly string _keyResolutionHeight = "resolutionHeight";

    IEnumerator Start()
    {
        Instance = this;
        Instance.InitSettingTypes();
        Instance.LoadSettings();
        
        // 等待语言加载完成
        yield return LocalizationSettings.InitializationOperation;
        // 应用设置中的语言
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales.Find(l => l.LocaleName == Instance.Language);
        // 阻止销毁
        DontDestroyOnLoad(Instance);
    }

    private void InitSettingTypes()
    {
        var strType = typeof(string);
        var intType = typeof(int);
        var floatType = typeof(float);
        
        _settingTypes[_keyLanguage] = strType;
        _settingTypes[_keyResolutionWidth] = intType;
        _settingTypes[_keyResolutionHeight] = intType;
    }

    [NotNull]
    private object GetPlayerRef(string key, Type t)
    {
        if (t == typeof(string))
        {
            return PlayerPrefs.GetString(key);
        }

        if (t == typeof(int))
        {
            return PlayerPrefs.GetInt(key);
        }

        if (t == typeof(bool))
        {
            return PlayerPrefs.GetInt(key) != 0;
        }

        return t == typeof(float) ? PlayerPrefs.GetFloat(key) : Activator.CreateInstance(t);
    }

    private void SetPlayerRef<T>(string key, T value)
    {
        switch (value)
        {
            case string:
                PlayerPrefs.SetString(key, (string)(object)value);
                break;
            case int:
                PlayerPrefs.SetInt(key, (int)(object)value);
                break;
            case bool:
                PlayerPrefs.SetInt(key, (bool)(object)value ? 1 : 0);
                break;
            case float:
                PlayerPrefs.SetFloat(key, (float)(object)value);
                break;
        }
    }

    private void LoadSettings()
    {
        // 根据类型读取设置数据
        foreach (var entry in _settingTypes)
        {
            _settingsMap[entry.Key] = GetPlayerRef(entry.Key, entry.Value);
            Debug.Log($"Settings loaded: {entry.Key}:{_settingsMap[entry.Key]}");
        }
    }

    public string Language
    {
        set
        {
            _settingsMap[_keyLanguage] = value;
            SetPlayerRef(_keyLanguage, value);
            PlayerPrefs.Save();
        }

        get => (string)_settingsMap[_keyLanguage];
    }

    public Vector2 Resolution
    {
        get
        {
            var width = (int)_settingsMap[_keyResolutionWidth];
            var height = (int)_settingsMap[_keyResolutionHeight];
            
            if (width != 0 && height != 0) return new Vector2(width, height);
            width = 1920;
            height = 1080;

            return new Vector2(width, height);
        }

        set
        {
            _settingsMap[_keyResolutionWidth] = (int)value.x;
            _settingsMap[_keyResolutionHeight] = (int)value.y;
            
            SetPlayerRef(_keyResolutionWidth, (int)value.x);
            SetPlayerRef(_keyResolutionHeight, (int)value.y);
            PlayerPrefs.Save();
        }
    }
}
