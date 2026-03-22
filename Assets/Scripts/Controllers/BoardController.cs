using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }
    public bool UndoEnabled { get; set; } = false;

    public bool IsGameActive => !m_gameOver && m_gameManager != null &&
                                m_gameManager.State == GameManager.eStateGame.GAME_STARTED;

    public int MatchMin => m_gameSettings != null ? m_gameSettings.MatchesMin : 3;

    public List<Cell> GetNonEmptyBoardCells() => m_board?.GetNonEmptyBoardCells() ?? new List<Cell>();
    public List<Cell> GetBelowCells()          => m_board?.GetFilledBelowCells()  ?? new List<Cell>();

    public void ExecuteAutoMove(Cell cell)
    {
        if (!IsGameActive || IsBusy || cell == null || cell.IsEmpty) return;
        HandleBoardCellClick(cell);
    }

    private Board        m_board;
    private GameManager  m_gameManager;
    private Camera       m_cam;
    private GameSettings m_gameSettings;
    private bool         m_gameOver;

    private const int TOTAL_WAVES = 3;
    private int m_currentWave = 0;

    private List<Cell> m_potentialMatch = new List<Cell>();
    private float      m_timeAfterFill;
    private bool       m_hintIsShown;

    private Cell m_selectedBelowCell = null;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        Debug.Log("[BoardController] StartGame() called.");

        m_gameManager  = gameManager;
        m_gameSettings = gameSettings;
        m_gameManager.StateChangedAction += OnGameStateChange;
        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);
        m_board.OnBelowFull    += OnBelowFull;
        m_board.OnBoardCleared += OnBoardCleared;

        m_currentWave = 1;
        Debug.Log($"[BoardController] Wave {m_currentWave}/{TOTAL_WAVES} starting.");
        m_board.Fill();

        IsBusy         = false;
        m_timeAfterFill = 0f;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        Debug.Log($"[BoardController] OnGameStateChange() — {state}");
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
            case GameManager.eStateGame.GAME_WIN:
                m_gameOver = true;
                StopHints();
                break;
        }
    }

    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy)     return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    private void HandleClick()
    {
        var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider == null) return;

        Cell clickedCell = hit.collider.GetComponent<Cell>();
        if (clickedCell == null) return;

        if (UndoEnabled && m_board.IsBelowCell(clickedCell))
        {
            HandleBelowCellClick(clickedCell);
            return;
        }

        if (m_board.IsBoardCell(clickedCell))
            HandleBoardCellClick(clickedCell);
    }

    private void HandleBoardCellClick(Cell cell)
    {
        if (cell.IsEmpty) return;

        DeselectBelowCell();
        StopHints();

        IsBusy = true;
        m_board.SendItemBelow(cell, () =>
        {
            IsBusy = false;
            OnMoveEvent();
            m_timeAfterFill = 0f;
        });
    }

    private void HandleBelowCellClick(Cell belowCell)
    {
        if (belowCell.IsEmpty) return;

        if (m_selectedBelowCell == belowCell)
        {
            DeselectBelowCell();
            return;
        }

        DeselectBelowCell();
        SelectBelowCell(belowCell);

        Cell emptyBoardCell = m_board.FindEmptyBoardCell();
        if (emptyBoardCell == null)
        {
            Debug.LogWarning("[BoardController] No empty board cell to return item to.");
            DeselectBelowCell();
            return;
        }

        IsBusy = true;
        m_board.ReturnItemToBoard(belowCell, emptyBoardCell, () =>
        {
            DeselectBelowCell();
            IsBusy = false;
            m_timeAfterFill = 0f;
        });
    }

    private void SelectBelowCell(Cell cell)
    {
        m_selectedBelowCell = cell;
        cell.Item?.View?.DOScale(1.2f, 0.15f);
    }

    private void DeselectBelowCell()
    {
        if (m_selectedBelowCell == null) return;
        m_selectedBelowCell.Item?.View?.DOScale(1.0f, 0.1f);
        m_selectedBelowCell = null;
    }

    private void OnBoardCleared()
    {
        if (m_gameOver) return;

        Debug.Log($"[BoardController] Wave {m_currentWave}/{TOTAL_WAVES} cleared!");

        if (m_currentWave >= TOTAL_WAVES)
        {
            Debug.Log("[BoardController] Tất cả wave hoàn thành — GameWin!");
            m_gameManager.GameWin();
        }
        else
        {
            m_currentWave++;
            Debug.Log($"[BoardController] Load wave {m_currentWave}/{TOTAL_WAVES}.");

            IsBusy = true;
            DOVirtual.DelayedCall(0.5f, () =>
            {
                m_board.Refill();
                IsBusy = false;
                m_timeAfterFill = 0f;
                Debug.Log($"[BoardController] Wave {m_currentWave} ready.");
            });
        }
    }

    private void OnBelowFull()
    {
        Debug.Log("[BoardController] OnBelowFull — hàng dưới đầy, GameOver.");
        m_gameManager.GameOver();
    }

    private void ShowHint()
    {
        m_potentialMatch = m_board.GetNonEmptyBoardCells();
        if (m_potentialMatch.Count == 0) return;

        m_hintIsShown = true;
        m_potentialMatch[0].AnimateItemForHint();
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
            if (!cell.IsEmpty) cell.StopHintAnimation();
        m_potentialMatch.Clear();
    }

    internal void Clear()
    {
        Debug.Log("[BoardController] Clear() called.");
        StopHints();

        if (m_board != null)
        {
            m_board.OnBelowFull    -= OnBelowFull;
            m_board.OnBoardCleared -= OnBoardCleared;
            m_board.Clear();
        }

        if (m_gameManager != null)
            m_gameManager.StateChangedAction -= OnGameStateChange;
    }
}
// using DG.Tweening;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class BoardController : MonoBehaviour
// {
//     public event Action OnMoveEvent = delegate { };

//     public bool IsBusy { get; private set; }

//     private Board m_board;

//     private GameManager m_gameManager;

//     private bool m_isDragging;

//     private Camera m_cam;

//     private Collider2D m_hitCollider;

//     private GameSettings m_gameSettings;

//     private List<Cell> m_potentialMatch;

//     private float m_timeAfterFill;

//     private bool m_hintIsShown;

//     private bool m_gameOver;

//     public void StartGame(GameManager gameManager, GameSettings gameSettings)
//     {
//         m_gameManager = gameManager;

//         m_gameSettings = gameSettings;

//         m_gameManager.StateChangedAction += OnGameStateChange;

//         m_cam = Camera.main;

//         m_board = new Board(this.transform, gameSettings);

//         Fill();
//     }

//     private void Fill()
//     {
//         m_board.Fill();
//         FindMatchesAndCollapse();
//     }

//     private void OnGameStateChange(GameManager.eStateGame state)
//     {
//         switch (state)
//         {
//             case GameManager.eStateGame.GAME_STARTED:
//                 IsBusy = false;
//                 break;
//             case GameManager.eStateGame.PAUSE:
//                 IsBusy = true;
//                 break;
//             case GameManager.eStateGame.GAME_OVER:
//                 m_gameOver = true;
//                 StopHints();
//                 break;
//         }
//     }


//     public void Update()
//     {
//         if (m_gameOver) return;
//         if (IsBusy) return;

//         if (!m_hintIsShown)
//         {
//             m_timeAfterFill += Time.deltaTime;
//             if (m_timeAfterFill > m_gameSettings.TimeForHint)
//             {
//                 m_timeAfterFill = 0f;
//                 ShowHint();
//             }
//         }

//         if (Input.GetMouseButtonDown(0))
//         {
//             var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
//             if (hit.collider != null)
//             {
//                 m_isDragging = true;
//                 m_hitCollider = hit.collider;
//             }
//         }

//         if (Input.GetMouseButtonUp(0))
//         {
//             ResetRayCast();
//         }

//         if (Input.GetMouseButton(0) && m_isDragging)
//         {
//             var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
//             if (hit.collider != null)
//             {
//                 if (m_hitCollider != null && m_hitCollider != hit.collider)
//                 {
//                     StopHints();

//                     Cell c1 = m_hitCollider.GetComponent<Cell>();
//                     Cell c2 = hit.collider.GetComponent<Cell>();
//                     if (AreItemsNeighbor(c1, c2))
//                     {
//                         IsBusy = true;
//                         SetSortingLayer(c1, c2);
//                         m_board.Swap(c1, c2, () =>
//                         {
//                             FindMatchesAndCollapse(c1, c2);
//                         });

//                         ResetRayCast();
//                     }
//                 }
//             }
//             else
//             {
//                 ResetRayCast();
//             }
//         }
//     }

//     private void ResetRayCast()
//     {
//         m_isDragging = false;
//         m_hitCollider = null;
//     }

//     private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
//     {
//         if (cell1.Item is BonusItem)
//         {
//             cell1.ExplodeItem();
//             StartCoroutine(ShiftDownItemsCoroutine());
//         }
//         else if (cell2.Item is BonusItem)
//         {
//             cell2.ExplodeItem();
//             StartCoroutine(ShiftDownItemsCoroutine());
//         }
//         else
//         {
//             List<Cell> cells1 = GetMatches(cell1);
//             List<Cell> cells2 = GetMatches(cell2);

//             List<Cell> matches = new List<Cell>();
//             matches.AddRange(cells1);
//             matches.AddRange(cells2);
//             matches = matches.Distinct().ToList();

//             if (matches.Count < m_gameSettings.MatchesMin)
//             {
//                 m_board.Swap(cell1, cell2, () =>
//                 {
//                     IsBusy = false;
//                 });
//             }
//             else
//             {
//                 OnMoveEvent();

//                 CollapseMatches(matches, cell2);
//             }
//         }
//     }

//     private void FindMatchesAndCollapse()
//     {
//         List<Cell> matches = m_board.FindFirstMatch();

//         if (matches.Count > 0)
//         {
//             CollapseMatches(matches, null);
//         }
//         else
//         {
//             m_potentialMatch = m_board.GetPotentialMatches();
//             if (m_potentialMatch.Count > 0)
//             {
//                 IsBusy = false;

//                 m_timeAfterFill = 0f;
//             }
//             else
//             {
//                 //StartCoroutine(RefillBoardCoroutine());
//                 StartCoroutine(ShuffleBoardCoroutine());
//             }
//         }
//     }

//     private List<Cell> GetMatches(Cell cell)
//     {
//         List<Cell> listHor = m_board.GetHorizontalMatches(cell);
//         if (listHor.Count < m_gameSettings.MatchesMin)
//         {
//             listHor.Clear();
//         }

//         List<Cell> listVert = m_board.GetVerticalMatches(cell);
//         if (listVert.Count < m_gameSettings.MatchesMin)
//         {
//             listVert.Clear();
//         }

//         return listHor.Concat(listVert).Distinct().ToList();
//     }

//     private void CollapseMatches(List<Cell> matches, Cell cellEnd)
//     {
//         for (int i = 0; i < matches.Count; i++)
//         {
//             matches[i].ExplodeItem();
//         }

//         if(matches.Count > m_gameSettings.MatchesMin)
//         {
//             m_board.ConvertNormalToBonus(matches, cellEnd);
//         }

//         StartCoroutine(ShiftDownItemsCoroutine());
//     }

//     private IEnumerator ShiftDownItemsCoroutine()
//     {
//         m_board.ShiftDownItems();

//         yield return new WaitForSeconds(0.2f);

//         m_board.FillGapsWithNewItems();

//         yield return new WaitForSeconds(0.2f);

//         FindMatchesAndCollapse();
//     }

//     private IEnumerator RefillBoardCoroutine()
//     {
//         m_board.ExplodeAllItems();

//         yield return new WaitForSeconds(0.2f);

//         m_board.Fill();

//         yield return new WaitForSeconds(0.2f);

//         FindMatchesAndCollapse();
//     }

//     private IEnumerator ShuffleBoardCoroutine()
//     {
//         m_board.Shuffle();

//         yield return new WaitForSeconds(0.3f);

//         FindMatchesAndCollapse();
//     }


//     private void SetSortingLayer(Cell cell1, Cell cell2)
//     {
//         if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
//         if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
//     }

//     private bool AreItemsNeighbor(Cell cell1, Cell cell2)
//     {
//         return cell1.IsNeighbour(cell2);
//     }

//     internal void Clear()
//     {
//         m_board.Clear();
//     }

//     private void ShowHint()
//     {
//         m_hintIsShown = true;
//         foreach (var cell in m_potentialMatch)
//         {
//             cell.AnimateItemForHint();
//         }
//     }

//     private void StopHints()
//     {
//         m_hintIsShown = false;
//         foreach (var cell in m_potentialMatch)
//         {
//             cell.StopHintAnimation();
//         }

//         m_potentialMatch.Clear();
//     }
// }
