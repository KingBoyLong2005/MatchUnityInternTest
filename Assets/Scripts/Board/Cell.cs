using System;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int BoardX { get; private set; }

    public int BoardY { get; private set; }

    public Item Item { get; private set; }

    public Cell NeighbourUp { get; set; }

    public Cell NeighbourRight { get; set; }

    public Cell NeighbourBottom { get; set; }

    public Cell NeighbourLeft { get; set; }

    public bool IsEmpty => Item == null;

    public void Setup(int cellX, int cellY)
    {
        this.BoardX = cellX;
        this.BoardY = cellY;
    }

    public bool IsNeighbour(Cell other)
    {
        if (other == null) return false;
        return BoardX == other.BoardX && Mathf.Abs(BoardY - other.BoardY) == 1 ||
               BoardY == other.BoardY && Mathf.Abs(BoardX - other.BoardX) == 1;
    }

    public void Free()
    {
        Item = null;
    }

    public void Assign(Item item)
    {
        Item = item;
        Item.SetCell(this);
    }

    public void ApplyItemPosition(bool withAppearAnimation)
    {
        if (Item == null) return;

        Item.SetViewPosition(this.transform.position);

        if (withAppearAnimation)
        {
            Item.ShowAppearAnimation();
        }
    }


    internal void Clear()
    {
        if (Item != null)
        {
            Item.Clear();
            Item = null;
        }
    }

    internal bool IsSameType(Cell other)
    {
        if (other == null) return false;
        return Item != null && other.Item != null && Item.IsSameType(other.Item);
    }

    internal void ExplodeItem()
    {
        if (Item == null) return;

        Item item = Item;
        Item = null; 
        item.ExplodeView();
    }

    internal void AnimateItemForHint()
    {
        if (Item == null) return;
        Item.AnimateForHint();
    }

    internal void StopHintAnimation()
    {
        if (Item == null) return;
        Item.StopAnimateForHint();
    }

    internal void ApplyItemMoveToPosition()
    {
        if (Item == null) return;
        Item.AnimationMoveToPosition();
    }
}