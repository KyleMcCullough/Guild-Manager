using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Item
{
    public Tile parent = null;
    public Action<Item> ItemChangedEvent { get; protected set; } = null;
    public Inventory relatedInventory = null;
    public int maxStack
    {
        get
        {
            return Data.GetStackLimit(type);
        }
    }
    int currentStackAmount = 0;

    public int CurrentStackAmount
    {
        get {return currentStackAmount; }
        set
        {

            // If there are no contents to the item stack, destroy it.
            if (value == 0)
            {
                this.Type = ItemType.Empty;
                this.DeleteItem();
            }

            else
            {
                currentStackAmount = value;
            }
        }
    }
    public string type = ItemType.Empty;

    public string Type
    {
        get { return type; }

        set
        {
            string previous = type;
            type = value;

            // Call callback to refresh tile visually.
            if (ItemChangedEvent != null && previous != type)
            {
                ItemChangedEvent(this);
            }
        }
    }

    public Item(Tile parent)
    {
        this.parent = parent;
        ItemChangedEvent += this.parent.world.itemChangedEvent;
    }

    public Item(Tile parent, string type, int currentStackAmount)
    {
        AssignParent(parent, currentStackAmount, type);
        
        if (ItemChangedEvent != null)
        {
            ItemChangedEvent(this);
        }
    }

    public Item(string type, int currentStackAmount, Inventory relatedInventory)
    {
        this.Type = type;
        this.currentStackAmount = currentStackAmount;
        this.relatedInventory = relatedInventory;
        this.ItemChangedEvent = null;
    }

    // Assigns parent and registers item changed callback from it.
    public void AssignParent(Tile tile, int amount, string type)
    {
        if(tile.item != null)
        {
            if (tile.item.maxStack == tile.item.currentStackAmount)
            {
                SendItemsToNeighbour(amount, type, tile);
            }

            else
            {
                amount = TryAddingToStack(amount, type, tile);
                Debug.Log(amount);
                if (amount > 0)
                {
                    SendItemsToNeighbour(amount, type, tile);
                }

                if (parent.room != null)
                {
                    parent.room.AssignItemToRoom(this.Type, this.CurrentStackAmount);
                }
            }
            return;
        }

        else
        {
            this.parent = tile;
            this.type = type;

            if (amount > maxStack)
            {
                this.currentStackAmount = maxStack;
                SendItemsToNeighbour(amount - this.currentStackAmount, type, tile);
            }

            else
            {
                this.currentStackAmount = amount;
            }

            this.parent.item = this;

            if (parent.room != null)
            {
                parent.room.AssignItemToRoom(this.Type, this.CurrentStackAmount);
            }
        }

        if (this.parent.room != null) this.parent.room.ResetUnreachableJobs();
        this.relatedInventory = null;

        if (ItemChangedEvent == null)
        {
            ItemChangedEvent += this.parent.world.itemChangedEvent;
        }
    }

    // Tries adding to a stack. Returns the amount that can't be added due to limit.
    int TryAddingToStack(int amountToAdd, string type, Tile tile)
    {

        if (this.Type == type || this.Type == ItemType.Empty && (tile.structure == null || tile.structure.Type == ObjectType.Empty))
        {
            this.Type = type;
        }

        else
        {
            return amountToAdd;
        }

        if (this.currentStackAmount == this.maxStack)
        {
            return amountToAdd;
        }

        if (amountToAdd + CurrentStackAmount <= maxStack)
        {
            CurrentStackAmount += amountToAdd;
            return 0;
        }

        int remaining = maxStack - CurrentStackAmount;
        CurrentStackAmount += remaining;

        return amountToAdd - remaining;
    }

    // Loops through neighbors until all the items are placed.
    void SendItemsToNeighbour(int amount, string type, Tile tile)
    {
        int index = 1;
        while (amount > 0)
        {
            for (int x = tile.x - index; x < tile.x + 1 + index; x++)
            {
                for (int y = tile.y - index; y < tile.y + 1 + index; y++)
                {

                    if (amount == 0)
                    {
                        break;
                    }

                    try
                    {
                        Tile t = tile.world.GetTile(x, y);

                        if (t.structure.Type == ObjectType.Empty && (t.item == null || t.item.Type == type && t.item.CurrentStackAmount != t.item.maxStack))
                        {
                            if (t.item == null)
                            {
                                t.item = new Item(t);
                            }

                            amount = t.item.TryAddingToStack(amount, type, t);

                            if (t.room != null)
                            {
                                t.room.AssignItemToRoom(t.item.Type, t.item.currentStackAmount);
                            }
                        }
                    }

                    //FIXME: To be taken out once the borders around the edges that restricts building is implemented.
                    catch (NullReferenceException)
                    {
                        Debug.Log("SendItemsToNeighbour - Hit edge.");
                        continue;
                    }
                }
            }

            index++;
        }
    }

    public void DeleteItem()
    {
        if (this.relatedInventory != null)
        {
            this.relatedInventory.DeleteItem(this);
        }

        if (this.parent != null)
        {
            this.parent.item = null;
            this.parent = null;
        }
    }

    public static Tile SearchForItem(string type, Tile tile)
    {

        List<Tile> checkedTiles = new List<Tile>();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = null;
            while (tilesToCheck.Count > 0)
            {
                t = tilesToCheck.Dequeue();

                if (checkedTiles.Contains(t)) continue;

                break;
            }
            
            if (t == null) return null;

            checkedTiles.Add(t);

            if (t.item != null && t.item.Type == type) return t;

                Tile[] ns = t.GetNeighbors();
                foreach (Tile t2 in ns)
                {
                    if (checkedTiles.Contains(t2))
                    {
                        continue;
                    }

                    if (t2 == null)
                    {
                        return null;
                    }

                    // We know t2 is not null nor is it an empty tile, so just make sure it
                    // hasn't already been processed and isn't a "wall" type tile.
                    if (t2.structure == null || t2.structure.canCreateRooms == false || t2.structure.canCreateRooms && !t2.structure.IsConstructed || t2.structure.IsDoor())
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
        }

        return null;
    }
}