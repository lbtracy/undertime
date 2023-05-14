using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public GameObject settingsUIPrefab;
    public GameObject continueButton;

    private IEnumerator Start()
    {
        // 等待存档管理器加载好
        yield return new WaitUntil(() => GameSaveManager.instance != null);
        if (!GameSaveManager.instance.IsGameExists())
        {
            // 不存在存档，隐藏继续游戏按钮
            continueButton.SetActive(false);
        }
    }

    private void EnterGame()
    {
        SceneManager.LoadScene("Playground");
        SceneManager.UnloadScene(SceneManager.GetSceneByName("StartScreen"));
    }
    
    public void NewGame() 
    {
        GameSaveManager.instance.NewGame();
        EnterGame();
    }

    public void ContinueGame()
    {
        EnterGame();
    }

    public void OpenSettings()
    {
        Instantiate(settingsUIPrefab, transform.parent);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
