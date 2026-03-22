using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelTime : LevelCondition
{
    private float m_time;
    private GameManager m_mngr;

    public override void Setup(float value, Text txt, GameManager mngr)
    {
        Debug.Log($"[LevelTime] Setup() — total time: {value}s");
        base.Setup(value, txt, mngr);

        m_mngr = mngr;
        m_time = value;

        BoardController board = FindFirstObjectByType<BoardController>();
        if (board != null)
        {
            board.UndoEnabled = true;
            Debug.Log("[LevelTime] UndoEnabled = true trên BoardController.");
        }
        else
        {
            Debug.LogWarning("[LevelTime] Không tìm thấy BoardController để bật UndoEnabled.");
        }

        UpdateText();
    }

    private void Update()
    {
        if (m_conditionCompleted) return;

        if (m_mngr.State != GameManager.eStateGame.GAME_STARTED)
            return;

        float previousTime = m_time;
        m_time -= Time.deltaTime;

        if ((int)previousTime != (int)m_time && m_time >= 0f)
            Debug.Log($"[LevelTime] Update() — time remaining: {(int)m_time}s");

        UpdateText();

        if (m_time <= -1f)
        {
            Debug.Log("[LevelTime] Time reached -1 — calling OnConditionComplete().");
            OnConditionComplete();
        }
    }

    protected override void UpdateText()
    {
        if (m_time < 0f) return;
        m_txt.text = string.Format("TIME:\n{0:00}", m_time);
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class LevelTime : LevelCondition
// {
//     private float m_time;

//     private GameManager m_mngr;

//     public override void Setup(float value, Text txt, GameManager mngr)
//     {
//         base.Setup(value, txt, mngr);

//         m_mngr = mngr;

//         m_time = value;

//         UpdateText();
//     }

//     private void Update()
//     {
//         if (m_conditionCompleted) return;

//         if (m_mngr.State != GameManager.eStateGame.GAME_STARTED) return;

//         m_time -= Time.deltaTime;

//         UpdateText();

//         if (m_time <= -1f)
//         {
//             OnConditionComplete();
//         }
//     }

//     protected override void UpdateText()
//     {
//         if (m_time < 0f) return;

//         m_txt.text = string.Format("TIME:\n{0:00}", m_time);
//     }
// }
