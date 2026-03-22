using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIMainManager : MonoBehaviour
{
    private IMenu[] m_menuList;

    private GameManager m_gameManager;

    private void Awake()
    {
        m_menuList = GetComponentsInChildren<IMenu>(true);
    }

    void Start()
    {
        for (int i = 0; i < m_menuList.Length; i++)
        {
            m_menuList[i].Setup(this);
        }
    }

    internal void ShowMainMenu()
    {
        m_gameManager.ClearLevel();
        m_gameManager.SetState(GameManager.eStateGame.MAIN_MENU);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_gameManager.State == GameManager.eStateGame.GAME_STARTED)
            {
                m_gameManager.SetState(GameManager.eStateGame.PAUSE);
            }
            else if (m_gameManager.State == GameManager.eStateGame.PAUSE)
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
            }
        }
    }

    internal void Setup(GameManager gameManager)
    {
        m_gameManager = gameManager;
        m_gameManager.StateChangedAction += OnGameStateChange;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.SETUP:
                break;

            case GameManager.eStateGame.MAIN_MENU:
                ShowMenu<UIPanelMain>();
                break;

            case GameManager.eStateGame.GAME_STARTED:
                ShowMenu<UIPanelGame>();
                break;

            case GameManager.eStateGame.PAUSE:
                ShowMenu<UIPanelPause>();
                break;

            case GameManager.eStateGame.GAME_OVER:
                ShowResultPanel(isWin: false);
                break;

            case GameManager.eStateGame.GAME_WIN:
                ShowResultPanel(isWin: true);
                break;
        }
    }

    /// <summary>
    /// Set text win/lose trên UIPanelGameOver trước rồi mới Show.
    /// Cả hai kết quả đều dùng chung panel này.
    /// </summary>
    private void ShowResultPanel(bool isWin)
    {
        // Tìm panel và set kết quả trước khi show
        UIPanelGameOver resultPanel = m_menuList
            .OfType<UIPanelGameOver>()
            .FirstOrDefault();

        if (resultPanel != null)
        {
            resultPanel.SetResult(isWin);
        }
        else
        {
            Debug.LogError("[UIMainManager] ShowResultPanel — UIPanelGameOver NOT FOUND!");
        }

        ShowMenu<UIPanelGameOver>();
    }

    private void ShowMenu<T>() where T : IMenu
    {
        bool found = false;
        for (int i = 0; i < m_menuList.Length; i++)
        {
            IMenu menu = m_menuList[i];
            if (menu is T)
            {
                menu.Show();
                found = true;
                Debug.Log($"[UIMainManager] ShowMenu: showing {menu.GetType().Name}");
            }
            else
            {
                menu.Hide();
            }
        }
        if (!found)
            Debug.LogError($"[UIMainManager] ShowMenu<{typeof(T).Name}>: panel NOT FOUND in scene!");
    }

    internal Text GetLevelConditionView()
    {
        UIPanelGame game = m_menuList.Where(x => x is UIPanelGame).Cast<UIPanelGame>().FirstOrDefault();
        if (game)
        {
            return game.LevelConditionView;
        }

        return null;
    }

    internal void ShowPauseMenu()
    {
        m_gameManager.SetState(GameManager.eStateGame.PAUSE);
    }

    internal void LoadLevelFree()
    {
        m_gameManager.LoadLevel(GameManager.eLevelMode.FREE);
    }

    internal void LoadLevelTimer()
    {
        m_gameManager.LoadLevel(GameManager.eLevelMode.TIMER);
    }

    internal void ShowGameMenu()
    {
        m_gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
    }
}