using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelMoves : LevelCondition
{
    private int m_moves;
    private BoardController m_board;

    public void Setup(float value, Text txt, BoardController board)
    {
        Debug.Log($"[LevelMoves] Setup() — total moves: {(int)value}");
        base.Setup(value, txt);

        m_moves = (int)value;
        m_board = board;
        m_board.UndoEnabled = false;
        m_board.OnMoveEvent += OnMove;

        UpdateText();
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;

        m_moves--;
        UpdateText();

        if (m_moves <= 0)
            OnConditionComplete();
    }

    protected override void UpdateText()
    {
        if (m_txt != null)
            m_txt.text = $"MOVES:\n{m_moves}";
    }

    protected override void OnDestroy()
    {
        if (m_board != null) m_board.OnMoveEvent -= OnMove;
        base.OnDestroy();
    }
}

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class LevelMoves : LevelCondition
// {
//     private int m_moves;

//     private BoardController m_board;

//     public override void Setup(float value, Text txt, BoardController board)
//     {
//         base.Setup(value, txt);

//         m_moves = (int)value;

//         m_board = board;

//         m_board.OnMoveEvent += OnMove;

//         UpdateText();
//     }

//     private void OnMove()
//     {
//         if (m_conditionCompleted) return;

//         m_moves--;

//         UpdateText();

//         if(m_moves <= 0)
//         {
//             OnConditionComplete();
//         }
//     }

//     protected override void UpdateText()
//     {
//         m_txt.text = string.Format("MOVES:\n{0}", m_moves);
//     }

//     protected override void OnDestroy()
//     {
//         if (m_board != null) m_board.OnMoveEvent -= OnMove;

//         base.OnDestroy();
//     }
// }
