using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// GameManager — quản lý vòng đời game.
///
/// WIN:  BoardController gọi GameWin() khi hoàn thành tất cả 3 wave.
/// LOSE: BoardController gọi GameOver() khi hàng dưới đầy 5 ô.
///       LevelTime gọi GameOver() khi hết thời gian (Timer mode).
///
/// Chế độ chơi:
///   FREE  — không giới hạn moves, không timer. Thắng khi clear 3 wave.
///   TIMER — có đếm ngược thời gian + undo (trả item về board).
///           Thua khi hết giờ hoặc hàng dưới đầy.
/// </summary>
public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public static GameManager Instance { get; private set; }

    public enum eLevelMode
    {
        FREE,   // Không giới hạn — thắng khi clear 3 wave
        TIMER,  // Đếm ngược — thua khi hết giờ hoặc hàng dưới đầy
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER, 
        GAME_WIN,   
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;
            Debug.Log($"[GameManager] State → {m_state}");
            StateChangedAction(m_state);
        }
    }

    private GameSettings     m_gameSettings;
    private BoardController  m_boardController;
    private UIMainManager    m_uiMenu;
    private LevelCondition   m_levelCondition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        if (m_gameSettings == null)
            Debug.LogError("[GameManager] GameSettings NOT FOUND: " + Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        if (m_uiMenu == null)
            Debug.LogError("[GameManager] UIMainManager NOT FOUND in scene!");
        else
            m_uiMenu.Setup(this);
    }

    private void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    private void Update()
    {
        if (m_boardController != null)
            m_boardController.Update();
    }

    internal void SetState(eStateGame state)
    {
        State = state;

        if (State == eStateGame.PAUSE)
            DOTween.PauseAll();
        else
            DOTween.PlayAll();
    }

    public void LoadLevel(eLevelMode mode)
    {
        Debug.Log($"[GameManager] LoadLevel({mode})");

        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        if (mode == eLevelMode.TIMER)
        {
            Debug.Log($"[GameManager] TIMER mode — time={m_gameSettings.LevelTime}s");
            LevelTime lt = this.gameObject.AddComponent<LevelTime>();
            lt.Setup(m_gameSettings.LevelTime, m_uiMenu.GetLevelConditionView(), this);
            m_levelCondition = lt;
        }
        else // FREE
        {
            Debug.Log("[GameManager] FREE mode — không có điều kiện thời gian.");
            m_levelCondition = null;

            var condView = m_uiMenu.GetLevelConditionView();
            if (condView != null) condView.gameObject.SetActive(false);
        }

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent += GameOver;  
            m_levelCondition.ConditionWinEvent      += GameWin;   
        }

        State = eStateGame.GAME_STARTED;
    }


    public void GameWin()
    {
        Debug.Log("[GameManager] GameWin() called.");
        StartCoroutine(FinishGame(isWin: true));
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] GameOver() called.");
        StartCoroutine(FinishGame(isWin: false));
    }

    public void GameOver(bool isWin)
    {
        if (isWin) GameWin();
        else       GameOver();
    }

    internal void ClearLevel()
    {
        Debug.Log("[GameManager] ClearLevel()");

        if (m_boardController != null)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }

        var condView = m_uiMenu?.GetLevelConditionView();
        if (condView != null) condView.gameObject.SetActive(true);
    }

    private IEnumerator FinishGame(bool isWin)
    {
        if (m_boardController != null)
        {
            int frames = 0;
            while (m_boardController.IsBusy)
            {
                frames++;
                yield return new WaitForEndOfFrame();
            }
            Debug.Log($"[GameManager] Board free after {frames} frame(s). Waiting 1s...");
        }

        yield return new WaitForSeconds(1f);

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            m_levelCondition.ConditionWinEvent      -= GameWin;
            Destroy(m_levelCondition);
            m_levelCondition = null;
        }

        State = isWin ? eStateGame.GAME_WIN : eStateGame.GAME_OVER;
    }
}
// using DG.Tweening;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class GameManager : MonoBehaviour
// {
//     public event Action<eStateGame> StateChangedAction = delegate { };

//     public enum eLevelMode
//     {
//         TIMER,
//         MOVES
//     }

//     public enum eStateGame
//     {
//         SETUP,
//         MAIN_MENU,
//         GAME_STARTED,
//         PAUSE,
//         GAME_OVER,
//     }

//     private eStateGame m_state;
//     public eStateGame State
//     {
//         get { return m_state; }
//         private set
//         {
//             m_state = value;

//             StateChangedAction(m_state);
//         }
//     }


//     private GameSettings m_gameSettings;


//     private BoardController m_boardController;

//     private UIMainManager m_uiMenu;

//     private LevelCondition m_levelCondition;

//     private void Awake()
//     {
//         State = eStateGame.SETUP;

//         m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

//         m_uiMenu = FindObjectOfType<UIMainManager>();
//         m_uiMenu.Setup(this);
//     }

//     void Start()
//     {
//         State = eStateGame.MAIN_MENU;
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         if (m_boardController != null) m_boardController.Update();
//     }


//     internal void SetState(eStateGame state)
//     {
//         State = state;

//         if(State == eStateGame.PAUSE)
//         {
//             DOTween.PauseAll();
//         }
//         else
//         {
//             DOTween.PlayAll();
//         }
//     }

//     public void LoadLevel(eLevelMode mode)
//     {
//         m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
//         m_boardController.StartGame(this, m_gameSettings);

//         if (mode == eLevelMode.MOVES)
//         {
//             m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
//             m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
//         }
//         else if (mode == eLevelMode.TIMER)
//         {
//             m_levelCondition = this.gameObject.AddComponent<LevelTime>();
//             m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), this);
//         }

//         m_levelCondition.ConditionCompleteEvent += GameOver;

//         State = eStateGame.GAME_STARTED;
//     }

//     public void GameOver()
//     {
//         StartCoroutine(WaitBoardController());
//     }

//     internal void ClearLevel()
//     {
//         if (m_boardController)
//         {
//             m_boardController.Clear();
//             Destroy(m_boardController.gameObject);
//             m_boardController = null;
//         }
//     }

//     private IEnumerator WaitBoardController()
//     {
//         while (m_boardController.IsBusy)
//         {
//             yield return new WaitForEndOfFrame();
//         }

//         yield return new WaitForSeconds(1f);

//         State = eStateGame.GAME_OVER;

//         if (m_levelCondition != null)
//         {
//             m_levelCondition.ConditionCompleteEvent -= GameOver;

//             Destroy(m_levelCondition);
//             m_levelCondition = null;
//         }
//     }
// }
