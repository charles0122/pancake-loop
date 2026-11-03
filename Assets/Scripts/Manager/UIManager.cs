using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : PersistentMonoSingleton<UIManager>
{
    public GameObject MainMenu;
    public GameObject PauseMenu;
    public GameObject ExplainPanel;
    public GameObject setPanel;
    public GameObject TipPanel;
    public GameObject GamePanel;
    public bool inGame;


    public void ShowPanel(GameObject Go)
    {
        if (!Go.activeSelf)
        {
            Go.SetActive(true);
        }
    }
    public void HidePanel(GameObject Go)
    {
        if (Go.activeSelf)
        {
            Go.SetActive(false);
        }
    }

    public void SwitchMainMenuState(bool state)
    {
        if(MainMenu!=null)
        MainMenu.SetActive(state);

        // 回主菜单时隐藏游戏内 HUD
        if (state && ByteLoop.Manager.GameManager.Instance != null)
        {
            ByteLoop.Manager.GameManager.Instance.HideGameHud();
        }

        // if(state){
        //     AudioManager.Instance.PlayBGM(Music.MainMenuBGM,true);
        // }else{
        //     AudioManager.Instance.Stop();
        // }
    }

    public void SwitchExplain(bool state){
        if(ExplainPanel!=null)
        ExplainPanel.SetActive(state);
    }

    public void HidePause()
    {
        PauseMenu.SetActive(!PauseMenu.activeSelf);
        if (PauseMenu.activeSelf)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ExplainPanel.SetActive(!ExplainPanel.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            PauseMenu.SetActive(!PauseMenu.activeSelf);
            if (PauseMenu.activeSelf)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
    }



    // private bool _paused = false;

    // public void StartGame()
    // {
    //     SceneManager.LoadScene(0);
    // }

    // public void RestartGame()
    // {
    //     SceneManager.LoadScene(1);
    // }

    // public void ExitGame()
    // {
    //     Application.Quit();
    // }

    // public void Pause()
    // {
    //     setPanel.SetActive(true);
    //     Time.timeScale = 0;
    //     _paused = true;
    // }
    // public void ReTime()
    // {
    //     setPanel.SetActive(false);
    //     Time.timeScale = 1;
    //     _paused = false;
    // }

    // public void ReturnToMenu()
    // {
    //     Time.timeScale = 1;
    //     _paused = false;
    //     SceneManager.LoadScene(0);
    // }
}
