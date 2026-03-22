using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Board
{

    public event Action OnBelowFull = delegate { };

    public event Action OnBoardCleared = delegate { };

    private readonly int m_boardSizeX;
    private readonly int m_boardSizeY;
    private readonly int m_matchMin;
    private readonly int m_maxBelow = 5;

    private Cell[,] m_cells;
    private Cell[]  m_belowCells;
    private int     m_belowCount;
    private Transform m_root;

    public bool IsBoardEmpty
    {
        get
        {
            for (int x = 0; x < m_boardSizeX; x++)
                for (int y = 0; y < m_boardSizeY; y++)
                    if (m_cells[x, y] != null && !m_cells[x, y].IsEmpty)
                        return false;
            return true;
        }
    }

    public Board(Transform root, GameSettings settings)
    {
        m_root       = root;
        m_matchMin   = settings.MatchesMin;
        m_boardSizeX = settings.BoardSizeX;
        m_boardSizeY = settings.BoardSizeY;

        m_cells      = new Cell[m_boardSizeX, m_boardSizeY];
        m_belowCells = new Cell[m_maxBelow];

        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(
            -m_boardSizeX * 0.5f + 0.5f,
            -m_boardSizeY * 0.5f + 0.5f,
            0f);

        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);

        float belowStartX = -(m_maxBelow * 0.5f) + 0.5f;
        float belowY      = origin.y - 2f;

        for (int x = 0; x < m_maxBelow; x++)
        {
            Vector3 pos = new Vector3(belowStartX + x, belowY, 0f);
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = pos;
            go.transform.SetParent(m_root);
            go.name = $"BelowCell_{x}";

            Cell cell = go.GetComponent<Cell>();
            cell.Setup(x, -2);
            m_belowCells[x] = cell;
        }

        for (int x = 0; x < m_maxBelow; x++)
        {
            if (x + 1 < m_maxBelow) m_belowCells[x].NeighbourRight = m_belowCells[x + 1];
            if (x > 0)              m_belowCells[x].NeighbourLeft  = m_belowCells[x - 1];
        }

        for (int x = 0; x < m_boardSizeX; x++)
        {
            for (int y = 0; y < m_boardSizeY; y++)
            {
                Vector3 pos = origin + new Vector3(x, y, 0f);
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = pos;
                go.transform.SetParent(m_root);
                go.name = $"Cell_{x}_{y}";

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);
                m_cells[x, y] = cell;
            }
        }

        for (int x = 0; x < m_boardSizeX; x++)
        {
            for (int y = 0; y < m_boardSizeY; y++)
            {
                if (y + 1 < m_boardSizeY) m_cells[x, y].NeighbourUp    = m_cells[x, y + 1];
                if (x + 1 < m_boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0)                m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0)               m_cells[x, y].NeighbourLeft   = m_cells[x - 1, y];
            }
        }
    }

    internal void Fill()
    {
        int totalCells = m_boardSizeX * m_boardSizeY;
        int typeCount  = System.Enum.GetValues(typeof(NormalItem.eNormalType)).Length;

        List<NormalItem.eNormalType> pool = BuildDivisiblePool(totalCells, typeCount);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        int poolIdx = 0;
        for (int x = 0; x < m_boardSizeX && poolIdx < pool.Count; x++)
        {
            for (int y = 0; y < m_boardSizeY && poolIdx < pool.Count; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();
                item.SetType(pool[poolIdx++]);
                item.SetView();
                item.SetViewRoot(m_root);
                cell.Assign(item);
                cell.ApplyItemPosition(false);
            }
        }

        Debug.Log($"[Board] Fill() — pool={pool.Count}/{totalCells} items placed.");
    }


    private List<NormalItem.eNormalType> BuildDivisiblePool(int totalCells, int typeCount)
    {
        var pool  = new List<NormalItem.eNormalType>();
        var types = (NormalItem.eNormalType[])System.Enum.GetValues(typeof(NormalItem.eNormalType));

        int setsPerType = Mathf.Max(1, totalCells / (typeCount * 3));

        while (setsPerType * 3 * typeCount > totalCells)
            setsPerType--;

        if (setsPerType < 1) setsPerType = 1;

        foreach (var t in types)
            for (int i = 0; i < setsPerType * 3; i++)
                pool.Add(t);

        int typeIdx = 0;
        while (pool.Count + 3 <= totalCells)
        {
            var t = types[typeIdx % types.Length];
            pool.Add(t); pool.Add(t); pool.Add(t);
            typeIdx++;
        }

        Debug.Log($"[Board] BuildDivisiblePool: {pool.Count} items, {types.Length} types x{setsPerType * 3} minimum.");
        return pool;
    }

    public void SendItemBelow(Cell boardCell, Action callback)
    {
        if (boardCell.IsEmpty)
        {
            callback?.Invoke();
            return;
        }

        if (m_belowCount >= m_maxBelow)
        {
            Debug.LogWarning("[Board] SendItemBelow: hàng dưới đã đầy!");
            OnBelowFull();
            callback?.Invoke();
            return;
        }

        int  slotIdx    = m_belowCount;
        Cell targetSlot = m_belowCells[slotIdx];
        Item item       = boardCell.Item;

        boardCell.Free();
        targetSlot.Assign(item);
        m_belowCount++;

        Debug.Log($"[Board] SendItemBelow → slot {slotIdx}. belowCount={m_belowCount}");

        if (m_belowCount >= m_maxBelow)
        {
            item.View.DOMove(targetSlot.transform.position, 0.3f).OnComplete(() =>
            {
                Debug.LogWarning("[Board] Hàng dưới đầy — fire OnBelowFull.");
                OnBelowFull();
                callback?.Invoke();
            });
            return;
        }

        item.View.DOMove(targetSlot.transform.position, 0.3f).OnComplete(() =>
        {
            CheckBelowMatch(targetSlot, () =>
            {
                if (IsBoardEmpty)
                {
                    Debug.Log("[Board] Board đã sạch — fire OnBoardCleared.");
                    OnBoardCleared();
                }
                callback?.Invoke();
            });
        });
    }

    public void ReturnItemToBoard(Cell belowCell, Cell boardCell, Action callback)
    {
        if (belowCell.IsEmpty || !boardCell.IsEmpty)
        {
            callback?.Invoke();
            return;
        }

        Item item    = belowCell.Item;
        int usedSlot = System.Array.IndexOf(m_belowCells, belowCell);
        belowCell.Free();
        boardCell.Assign(item);

        item.View.DOMove(boardCell.transform.position, 0.3f).OnComplete(() =>
        {
            CompactBelowSlots();
            Debug.Log($"[Board] ReturnItemToBoard: slot {usedSlot} → {boardCell.name}. belowCount={m_belowCount}");
            callback?.Invoke();
        });
    }

    private void CheckBelowMatch(Cell arrivedCell, Action callback)
    {
        List<Cell> matches = GetHorizontalMatchesBelow(arrivedCell);

        if (matches.Count >= m_matchMin)
        {
            Debug.Log($"[Board] CheckBelowMatch: MATCH {matches.Count} — exploding!");
            foreach (Cell c in matches)
                c.ExplodeItem();
            CompactBelowSlots();
        }
        else
        {
            Debug.Log($"[Board] CheckBelowMatch: {matches.Count}/{m_matchMin} — chưa đủ.");
        }

        callback?.Invoke();
    }

    private List<Cell> GetHorizontalMatchesBelow(Cell cell)
    {
        var list = new List<Cell> { cell };

        Cell cur = cell;
        while (true)
        {
            Cell next = cur.NeighbourRight;
            if (next == null || !IsBelowCell(next) || !next.IsSameType(cell)) break;
            list.Add(next); cur = next;
        }

        cur = cell;
        while (true)
        {
            Cell next = cur.NeighbourLeft;
            if (next == null || !IsBelowCell(next) || !next.IsSameType(cell)) break;
            list.Add(next); cur = next;
        }

        return list;
    }

    private void CompactBelowSlots()
    {
        int writeIdx = 0;
        for (int x = 0; x < m_maxBelow; x++)
        {
            Cell slot = m_belowCells[x];
            if (slot.IsEmpty) continue;

            if (writeIdx != x)
            {
                Item item = slot.Item;
                slot.Free();
                m_belowCells[writeIdx].Assign(item);
                item.View.DOMove(m_belowCells[writeIdx].transform.position, 0.2f);
            }
            writeIdx++;
        }
        m_belowCount = writeIdx;
        Debug.Log($"[Board] CompactBelowSlots: belowCount={m_belowCount}");
    }


    public bool IsBelowCell(Cell cell)
        => System.Array.IndexOf(m_belowCells, cell) >= 0;

    public bool IsBoardCell(Cell cell)
    {
        for (int x = 0; x < m_boardSizeX; x++)
            for (int y = 0; y < m_boardSizeY; y++)
                if (m_cells[x, y] == cell) return true;
        return false;
    }

    public Cell FindEmptyBoardCell()
    {
        for (int y = 0; y < m_boardSizeY; y++)
            for (int x = 0; x < m_boardSizeX; x++)
                if (m_cells[x, y].IsEmpty) return m_cells[x, y];
        return null;
    }

    public List<Cell> GetNonEmptyBoardCells()
    {
        var list = new List<Cell>();
        for (int x = 0; x < m_boardSizeX; x++)
            for (int y = 0; y < m_boardSizeY; y++)
                if (!m_cells[x, y].IsEmpty) list.Add(m_cells[x, y]);
        return list;
    }

    public List<Cell> GetFilledBelowCells()
    {
        var list = new List<Cell>();
        for (int x = 0; x < m_maxBelow; x++)
            if (!m_belowCells[x].IsEmpty) list.Add(m_belowCells[x]);
        return list;
    }

    internal void Refill()
    {
        // Xóa item cũ còn sót (nếu có)
        for (int x = 0; x < m_boardSizeX; x++)
            for (int y = 0; y < m_boardSizeY; y++)
                m_cells[x, y]?.Clear();

        Fill();
        Debug.Log("[Board] Refill() — board chính đã được fill lại.");
    }

    public void Clear()
    {
        for (int x = 0; x < m_boardSizeX; x++)
        {
            for (int y = 0; y < m_boardSizeY; y++)
            {
                if (m_cells[x, y] == null) continue;
                m_cells[x, y].Clear();
                GameObject.Destroy(m_cells[x, y].gameObject);
                m_cells[x, y] = null;
            }
        }

        for (int x = 0; x < m_maxBelow; x++)
        {
            if (m_belowCells[x] == null) continue;
            m_belowCells[x].Clear();
            GameObject.Destroy(m_belowCells[x].gameObject);
            m_belowCells[x] = null;
        }

        m_belowCount = 0;
    }
}
//
// using DG.Tweening;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class Board
// {
//     public enum eMatchDirection
//     {
//         NONE,
//         HORIZONTAL,
//         VERTICAL,
//         ALL
//     }

//     private int boardSizeX;

//     private int boardSizeY;

//     private Cell[,] m_cells;

//     private Transform m_root;

//     private int m_matchMin;

//     public Board(Transform transform, GameSettings gameSettings)
//     {
//         m_root = transform;

//         m_matchMin = gameSettings.MatchesMin;

//         this.boardSizeX = gameSettings.BoardSizeX;
//         this.boardSizeY = gameSettings.BoardSizeY;

//         m_cells = new Cell[boardSizeX, boardSizeY];

//         CreateBoard();
//     }

//     private void CreateBoard()
//     {
//         Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
//         GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 GameObject go = GameObject.Instantiate(prefabBG);
//                 go.transform.position = origin + new Vector3(x, y, 0f);
//                 go.transform.SetParent(m_root);

//                 Cell cell = go.GetComponent<Cell>();
//                 cell.Setup(x, y);

//                 m_cells[x, y] = cell;
//             }
//         }

//         //set neighbours
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
//                 if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
//                 if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
//                 if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
//             }
//         }

//     }

//     internal void Fill()
//     {
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];
//                 NormalItem item = new NormalItem();

//                 List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
//                 if (cell.NeighbourBottom != null)
//                 {
//                     NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
//                     if (nitem != null)
//                     {
//                         types.Add(nitem.ItemType);
//                     }
//                 }

//                 if (cell.NeighbourLeft != null)
//                 {
//                     NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
//                     if (nitem != null)
//                     {
//                         types.Add(nitem.ItemType);
//                     }
//                 }

//                 item.SetType(Utils.GetRandomNormalTypeExcept(types.ToArray()));
//                 item.SetView();
//                 item.SetViewRoot(m_root);

//                 cell.Assign(item);
//                 cell.ApplyItemPosition(false);
//             }
//         }
//     }

//     internal void Shuffle()
//     {
//         List<Item> list = new List<Item>();
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 list.Add(m_cells[x, y].Item);
//                 m_cells[x, y].Free();
//             }
//         }

//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 int rnd = UnityEngine.Random.Range(0, list.Count);
//                 m_cells[x, y].Assign(list[rnd]);
//                 m_cells[x, y].ApplyItemMoveToPosition();

//                 list.RemoveAt(rnd);
//             }
//         }
//     }


//     internal void FillGapsWithNewItems()
//     {
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];
//                 if (!cell.IsEmpty) continue;

//                 NormalItem item = new NormalItem();

//                 item.SetType(Utils.GetRandomNormalType());
//                 item.SetView();
//                 item.SetViewRoot(m_root);

//                 cell.Assign(item);
//                 cell.ApplyItemPosition(true);
//             }
//         }
//     }

//     internal void ExplodeAllItems()
//     {
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];
//                 cell.ExplodeItem();
//             }
//         }
//     }

//     public void Swap(Cell cell1, Cell cell2, Action callback)
//     {
//         Item item = cell1.Item;
//         cell1.Free();
//         Item item2 = cell2.Item;
//         cell1.Assign(item2);
//         cell2.Free();
//         cell2.Assign(item);

//         item.View.DOMove(cell2.transform.position, 0.3f);
//         item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
//     }

//     public List<Cell> GetHorizontalMatches(Cell cell)
//     {
//         List<Cell> list = new List<Cell>();
//         list.Add(cell);

//         //check horizontal match
//         Cell newcell = cell;
//         while (true)
//         {
//             Cell neib = newcell.NeighbourRight;
//             if (neib == null) break;

//             if (neib.IsSameType(cell))
//             {
//                 list.Add(neib);
//                 newcell = neib;
//             }
//             else break;
//         }

//         newcell = cell;
//         while (true)
//         {
//             Cell neib = newcell.NeighbourLeft;
//             if (neib == null) break;

//             if (neib.IsSameType(cell))
//             {
//                 list.Add(neib);
//                 newcell = neib;
//             }
//             else break;
//         }

//         return list;
//     }


//     public List<Cell> GetVerticalMatches(Cell cell)
//     {
//         List<Cell> list = new List<Cell>();
//         list.Add(cell);

//         Cell newcell = cell;
//         while (true)
//         {
//             Cell neib = newcell.NeighbourUp;
//             if (neib == null) break;

//             if (neib.IsSameType(cell))
//             {
//                 list.Add(neib);
//                 newcell = neib;
//             }
//             else break;
//         }

//         newcell = cell;
//         while (true)
//         {
//             Cell neib = newcell.NeighbourBottom;
//             if (neib == null) break;

//             if (neib.IsSameType(cell))
//             {
//                 list.Add(neib);
//                 newcell = neib;
//             }
//             else break;
//         }

//         return list;
//     }

//     internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
//     {
//         eMatchDirection dir = GetMatchDirection(matches);

//         BonusItem item = new BonusItem();
//         switch (dir)
//         {
//             case eMatchDirection.ALL:
//                 item.SetType(BonusItem.eBonusType.ALL);
//                 break;
//             case eMatchDirection.HORIZONTAL:
//                 item.SetType(BonusItem.eBonusType.HORIZONTAL);
//                 break;
//             case eMatchDirection.VERTICAL:
//                 item.SetType(BonusItem.eBonusType.VERTICAL);
//                 break;
//         }

//         if (item != null)
//         {
//             if (cellToConvert == null)
//             {
//                 int rnd = UnityEngine.Random.Range(0, matches.Count);
//                 cellToConvert = matches[rnd];
//             }

//             item.SetView();
//             item.SetViewRoot(m_root);

//             cellToConvert.Free();
//             cellToConvert.Assign(item);
//             cellToConvert.ApplyItemPosition(true);
//         }
//     }


//     internal eMatchDirection GetMatchDirection(List<Cell> matches)
//     {
//         if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

//         var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
//         if (listH.Count == matches.Count)
//         {
//             return eMatchDirection.VERTICAL;
//         }

//         var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
//         if (listV.Count == matches.Count)
//         {
//             return eMatchDirection.HORIZONTAL;
//         }

//         if (matches.Count > 5)
//         {
//             return eMatchDirection.ALL;
//         }

//         return eMatchDirection.NONE;
//     }

//     internal List<Cell> FindFirstMatch()
//     {
//         List<Cell> list = new List<Cell>();

//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];

//                 var listhor = GetHorizontalMatches(cell);
//                 if (listhor.Count >= m_matchMin)
//                 {
//                     list = listhor;
//                     break;
//                 }

//                 var listvert = GetVerticalMatches(cell);
//                 if (listvert.Count >= m_matchMin)
//                 {
//                     list = listvert;
//                     break;
//                 }
//             }
//         }

//         return list;
//     }

//     public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
//     {
//         var dir = GetMatchDirection(matches);

//         var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
//         if(bonus == null)
//         {
//             return matches;
//         }

//         List<Cell> result = new List<Cell>();
//         switch (dir)
//         {
//             case eMatchDirection.HORIZONTAL:
//                 foreach (var cell in matches)
//                 {
//                     BonusItem item = cell.Item as BonusItem;
//                     if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
//                     {
//                         result.Add(cell);
//                     }
//                 }
//                 break;
//             case eMatchDirection.VERTICAL:
//                 foreach (var cell in matches)
//                 {
//                     BonusItem item = cell.Item as BonusItem;
//                     if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
//                     {
//                         result.Add(cell);
//                     }
//                 }
//                 break;
//             case eMatchDirection.ALL:
//                 foreach (var cell in matches)
//                 {
//                     BonusItem item = cell.Item as BonusItem;
//                     if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
//                     {
//                         result.Add(cell);
//                     }
//                 }
//                 break;
//         }

//         return result;
//     }

//     internal List<Cell> GetPotentialMatches()
//     {
//         List<Cell> result = new List<Cell>();
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];

//                 //check right
//                 /* example *\
//                   * * * * *
//                   * * * * *
//                   * * * ? *
//                   * & & * ?
//                   * * * ? *
//                 \* example  */

//                 if (cell.NeighbourRight != null)
//                 {
//                     result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
//                     if (result.Count > 0)
//                     {
//                         break;
//                     }
//                 }

//                 //check up
//                 /* example *\
//                   * ? * * *
//                   ? * ? * *
//                   * & * * *
//                   * & * * *
//                   * * * * *
//                 \* example  */
//                 if (cell.NeighbourUp != null)
//                 {
//                     result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
//                     if (result.Count > 0)
//                     {
//                         break;
//                     }
//                 }

//                 //check bottom
//                 /* example *\
//                   * * * * *
//                   * & * * *
//                   * & * * *
//                   ? * ? * *
//                   * ? * * *
//                 \* example  */
//                 if (cell.NeighbourBottom != null)
//                 {
//                     result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
//                     if (result.Count > 0)
//                     {
//                         break;
//                     }
//                 }

//                 //check left
//                 /* example *\
//                   * * * * *
//                   * * * * *
//                   * ? * * *
//                   ? * & & *
//                   * ? * * *
//                 \* example  */
//                 if (cell.NeighbourLeft != null)
//                 {
//                     result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
//                     if (result.Count > 0)
//                     {
//                         break;
//                     }
//                 }

//                 /* example *\
//                   * * * * *
//                   * * * * *
//                   * * ? * *
//                   * & * & *
//                   * * ? * *
//                 \* example  */
//                 Cell neib = cell.NeighbourRight;
//                 if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
//                 {
//                     Cell second = LookForTheSecondCellVertical(neib, cell);
//                     if (second != null)
//                     {
//                         result.Add(cell);
//                         result.Add(neib.NeighbourRight);
//                         result.Add(second);
//                         break;
//                     }
//                 }

//                 /* example *\
//                   * * * * *
//                   * & * * *
//                   ? * ? * *
//                   * & * * *
//                   * * * * *
//                 \* example  */
//                 neib = null;
//                 neib = cell.NeighbourUp;
//                 if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
//                 {
//                     Cell second = LookForTheSecondCellHorizontal(neib, cell);
//                     if (second != null)
//                     {
//                         result.Add(cell);
//                         result.Add(neib.NeighbourUp);
//                         result.Add(second);
//                         break;
//                     }
//                 }
//             }

//             if (result.Count > 0) break;
//         }

//         return result;
//     }

//     private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
//     {
//         List<Cell> result = new List<Cell>();

//         if (neighbour != null && neighbour.IsSameType(cell))
//         {
//             Cell third = LookForTheThirdCell(target, neighbour);
//             if (third != null)
//             {
//                 result.Add(cell);
//                 result.Add(neighbour);
//                 result.Add(third);
//             }
//         }

//         return result;
//     }

//     private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
//     {
//         if (target == null) return null;
//         if (target.IsSameType(main)) return null;

//         //look right
//         Cell second = null;
//         second = target.NeighbourRight;
//         if (second != null && second.IsSameType(main))
//         {
//             return second;
//         }

//         //look left
//         second = null;
//         second = target.NeighbourLeft;
//         if (second != null && second.IsSameType(main))
//         {
//             return second;
//         }

//         return null;
//     }

//     private Cell LookForTheSecondCellVertical(Cell target, Cell main)
//     {
//         if (target == null) return null;
//         if (target.IsSameType(main)) return null;

//         //look up        
//         Cell second = target.NeighbourUp;
//         if (second != null && second.IsSameType(main))
//         {
//             return second;
//         }

//         //look bottom
//         second = null;
//         second = target.NeighbourBottom;
//         if (second != null && second.IsSameType(main))
//         {
//             return second;
//         }

//         return null;
//     }

//     private Cell LookForTheThirdCell(Cell target, Cell main)
//     {
//         if (target == null) return null;
//         if (target.IsSameType(main)) return null;

//         //look up
//         Cell third = CheckThirdCell(target.NeighbourUp, main);
//         if (third != null)
//         {
//             return third;
//         }

//         //look right
//         third = null;
//         third = CheckThirdCell(target.NeighbourRight, main);
//         if (third != null)
//         {
//             return third;
//         }

//         //look bottom
//         third = null;
//         third = CheckThirdCell(target.NeighbourBottom, main);
//         if (third != null)
//         {
//             return third;
//         }

//         //look left
//         third = null;
//         third = CheckThirdCell(target.NeighbourLeft, main); ;
//         if (third != null)
//         {
//             return third;
//         }

//         return null;
//     }

//     private Cell CheckThirdCell(Cell target, Cell main)
//     {
//         if (target != null && target != main && target.IsSameType(main))
//         {
//             return target;
//         }

//         return null;
//     }

//     internal void ShiftDownItems()
//     {
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             int shifts = 0;
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];
//                 if (cell.IsEmpty)
//                 {
//                     shifts++;
//                     continue;
//                 }

//                 if (shifts == 0) continue;

//                 Cell holder = m_cells[x, y - shifts];

//                 Item item = cell.Item;
//                 cell.Free();

//                 holder.Assign(item);
//                 item.View.DOMove(holder.transform.position, 0.3f);
//             }
//         }
//     }

//     public void Clear()
//     {
//         for (int x = 0; x < boardSizeX; x++)
//         {
//             for (int y = 0; y < boardSizeY; y++)
//             {
//                 Cell cell = m_cells[x, y];
//                 cell.Clear();

//                 GameObject.Destroy(cell.gameObject);
//                 m_cells[x, y] = null;
//             }
//         }
//     }
// }

//
