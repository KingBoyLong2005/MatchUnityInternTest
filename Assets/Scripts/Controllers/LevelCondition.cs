using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelCondition : MonoBehaviour
{
    public event Action ConditionCompleteEvent = delegate { };

    public event Action ConditionWinEvent = delegate { };

    protected Text m_txt;
    protected bool m_conditionCompleted = false;
    public virtual void Setup(float value, Text txt)
    {
        Debug.Log($"[LevelCondition] Setup(value={value})");
        m_txt = txt;
    }

    public virtual void Setup(float value, Text txt, GameManager mngr)
    {
        Debug.Log($"[LevelCondition] Setup(value={value}, GameManager)");
        m_txt = txt;
    }

    protected virtual void UpdateText() { }


    protected void OnConditionComplete()
    {
        if (m_conditionCompleted) return;
        m_conditionCompleted = true;
        Debug.Log("[LevelCondition] OnConditionComplete() — THUA. Firing ConditionCompleteEvent.");
        ConditionCompleteEvent();
    }

    protected void OnConditionWin()
    {
        if (m_conditionCompleted) return;
        m_conditionCompleted = true;
        Debug.Log("[LevelCondition] OnConditionWin() — THẮNG. Firing ConditionWinEvent.");
        ConditionWinEvent();
    }

    protected virtual void OnDestroy()
    {
        Debug.Log("[LevelCondition] OnDestroy()");
    }
}
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class LevelCondition : MonoBehaviour
// {
//     public event Action ConditionCompleteEvent = delegate { };

//     protected Text m_txt;

//     protected bool m_conditionCompleted = false;

//     public virtual void Setup(float value, Text txt)
//     {
//         m_txt = txt;
//     }

//     public virtual void Setup(float value, Text txt, GameManager mngr)
//     {
//         m_txt = txt;
//     }

//     public virtual void Setup(float value, Text txt, BoardController board)
//     {
//         m_txt = txt;
//     }

//     protected virtual void UpdateText() { }

//     protected void OnConditionComplete()
//     {
//         m_conditionCompleted = true;

//         ConditionCompleteEvent();
//     }

//     protected virtual void OnDestroy()
//     {

//     }
// }
