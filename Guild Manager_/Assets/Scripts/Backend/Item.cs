using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Item
{
    public Tile parent;
    public Action<Item> ItemChangedEvent { get; protected set; }
    int maxStack = 0;
    int currentStackAmount = 0;
    string type = ItemType.Empty;

    public string Type
    {
        get { return type; }

        set
        {
            string previous = type;
            type = value;

            this.maxStack = Data.GetStackLimit(this.Type);

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
    }

    public void RegisterItemChanged(Action<Item> callback)
    {
        ItemChangedEvent += callback;
    }

    public void CreateNewStack(int amountToAdd, string type)
    {
        int leftOver = TryAddingToStack(amountToAdd, type);
        if (leftOver > 0)
        {
            SendItemsToNeighbour(leftOver, type);
        }
    }

    public int TakeFromStack(int amountToTake, string type)
    {
        if (this.type == type && amountToTake <= currentStackAmount)
        {
            currentStackAmount -= amountToTake;
            return amountToTake;
        }

        else if (amountToTake > currentStackAmount)
        {
            currentStackAmount = 0;
            return currentStackAmount;
        }

        return 0;
    }

    // Tries adding to a stack. Returns the amount that can't be added due to limit.
    int TryAddingToStack(int amountToAdd, string type)
    {

        if (this.Type == type || this.Type == ItemType.Empty && this.parent.structure.Type == ObjectType.Empty)
        {
            this.Type = type;
        }

        else
        {
            return amountToAdd;
        }

        if (amountToAdd > maxStack)
        {
            return amountToAdd;
        }

        if (amountToAdd + currentStackAmount <= maxStack)
        {
            currentStackAmount += amountToAdd;
            return 0;
        }

        int remaining = maxStack - currentStackAmount;
        currentStackAmount += remaining;

        return amountToAdd - remaining;
    }

    // Loops through neighbours until all the items are placed.
    void SendItemsToNeighbour(int amount, string type)
    {
        int index = 1;
        while (amount > 0)
        {

            for (int x = parent.x - index; x < parent.x + 1 + index; x++)
            {
                for (int y = parent.y - index; y < parent.y + 1 + index; y++)
                {

                    if (amount == 0)
                    {
                        break;
                    }

                    try
                    {
                        amount = parent.world.GetTile(x, y).Item.TryAddingToStack(amount, type);
                    }

                    //FIXME: To be taken out once the borders around the edges that restricts building is implemented.
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }

            index++;
        }
    }
}