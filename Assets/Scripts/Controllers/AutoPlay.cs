using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoPlayer : MonoBehaviour
{
    public Button ActiveAuto;
    public enum eAutoMode
    {
        WIN,
        LOSE,
    }

    [Header("References")]
    [Tooltip("Để trống → tự tìm trong scene bằng FindObjectOfType.")]
    [SerializeField] private BoardController m_boardController;

    [Header("Timing")]
    [Tooltip("Khoảng cách giữa mỗi action (giây).")]
    [SerializeField] private float m_stepDelay = 0.5f;

    public bool IsRunning { get; private set; }
    private eAutoMode m_currentMode;
    private Coroutine m_coroutine;

    private void Awake()
    {
        if (m_boardController == null)
            m_boardController = FindFirstObjectByType<BoardController>();

        if (m_boardController == null)
            Debug.LogError("[AutoPlayer] Awake() — BoardController NOT FOUND.");
        ActiveAuto.onClick.AddListener( () =>
            
                ToggleAutoplay(eAutoMode.LOSE)
    );
        ActiveAuto.GetComponentInChildren<Text>().text = "Auto";
    }


    public void StartAutoplay(eAutoMode mode)
    {
        if (m_boardController == null)
        {
            Debug.LogError("[AutoPlayer] StartAutoplay() — không có BoardController!");
            return;
        }

        StopAutoplay();

        m_currentMode = mode;
        IsRunning     = true;
        m_coroutine   = StartCoroutine(AutoplayLoop());

        Debug.Log($"[AutoPlayer] StartAutoplay({mode}) — delay={m_stepDelay}s.");
    }

    public void StopAutoplay()
    {
        if (m_coroutine != null)
        {
            StopCoroutine(m_coroutine);
            m_coroutine = null;
        }
        IsRunning = false;
    }

    public void ToggleAutoplay(eAutoMode mode)
    {
        if (IsRunning) StopAutoplay();
        else           StartAutoplay(mode);
    }

    public void StartAutoplayWin()  => StartAutoplay(eAutoMode.WIN);
    public void StartAutoplayLose() => StartAutoplay(eAutoMode.LOSE);
    public void Stop()              => StopAutoplay();


    private IEnumerator AutoplayLoop()
    {
        while (IsRunning)
        {
            yield return new WaitUntil(() => !m_boardController.IsBusy);

            if (!m_boardController.IsGameActive)
            {
                Debug.Log("[AutoPlayer] Game không còn active — dừng.");
                StopAutoplay();
                yield break;
            }

            Cell chosenCell = m_currentMode == eAutoMode.WIN
                ? PickCellForWin()
                : PickCellForLose();

            if (chosenCell == null)
            {
                Debug.LogWarning("[AutoPlayer] Không tìm được cell — dừng.");
                StopAutoplay();
                yield break;
            }

            Debug.Log($"[AutoPlayer] AutoStep({m_currentMode}) → {chosenCell.name}");
            m_boardController.ExecuteAutoMove(chosenCell);

            yield return new WaitForSeconds(m_stepDelay);
        }
    }


    private Cell PickCellForWin()
    {
        List<Cell> boardCells = m_boardController.GetNonEmptyBoardCells();
        List<Cell> belowCells = m_boardController.GetBelowCells();
        int        matchMin   = m_boardController.MatchMin;

        if (boardCells.Count == 0) return null;

        Dictionary<int, int> belowCounts = CountTypes(belowCells);

        Cell immediate = FindCellOfType(boardCells,
            t => belowCounts.TryGetValue(t, out int c) && c == matchMin - 1);
        if (immediate != null) return immediate;

        int bestType  = -1;
        int bestCount = -1;
        foreach (var kv in belowCounts)
        {
            if (kv.Value > bestCount)
            {
                bestCount = kv.Value;
                bestType  = kv.Key;
            }
        }

        if (bestType >= 0)
        {
            Cell found = boardCells.Find(c => GetCellType(c) == bestType);
            if (found != null) return found;
        }

        return boardCells[0];
    }


    private Cell PickCellForLose()
    {
        List<Cell> boardCells = m_boardController.GetNonEmptyBoardCells();
        List<Cell> belowCells = m_boardController.GetBelowCells();

        if (boardCells.Count == 0) return null;

        Dictionary<int, int> belowCounts = CountTypes(belowCells);

        Cell newType = boardCells.Find(c =>
        {
            int t = GetCellType(c);
            return t >= 0 && (!belowCounts.ContainsKey(t) || belowCounts[t] == 0);
        });
        if (newType != null) return newType;

        int leastType  = -1;
        int leastCount = int.MaxValue;
        foreach (var kv in belowCounts)
        {
            if (kv.Value < leastCount)
            {
                leastCount = kv.Value;
                leastType  = kv.Key;
            }
        }

        if (leastType >= 0)
        {
            Cell found = boardCells.Find(c => GetCellType(c) == leastType);
            if (found != null) return found;
        }

        return boardCells[0];
    }


    private Dictionary<int, int> CountTypes(List<Cell> cells)
    {
        var dict = new Dictionary<int, int>();
        foreach (Cell c in cells)
        {
            if (c.IsEmpty) continue;
            int t = GetCellType(c);
            if (t < 0) continue;
            dict.TryGetValue(t, out int cur);
            dict[t] = cur + 1;
        }
        return dict;
    }

    private int GetCellType(Cell cell)
    {
        if (cell == null || cell.IsEmpty) return -1;

        NormalItem ni = cell.Item as NormalItem;
        if (ni != null) return (int)ni.ItemType;

        BonusItem bi = cell.Item as BonusItem;
        if (bi != null) return (int)bi.ItemType + 100;
        return -1;
    }

    private Cell FindCellOfType(List<Cell> cells, System.Predicate<int> predicate)
    {
        foreach (Cell c in cells)
        {
            int t = GetCellType(c);
            if (t >= 0 && predicate(t)) return c;
        }
        return null;
    }
}