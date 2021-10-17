using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Item
{
    public Tile Parent;
    public Action<Item> ItemChangedEvent { get; protected set; }
    int maxStack;
    int currentStackAmount = 0;
    ItemType type = ItemType.Empty;


    public ItemType Type
    {
        get { return type; }

        set
        {
            ItemType previous = type;
            type = value;

            // Call callback to refresh tile visually.
            if (ItemChangedEvent != null && previous != type)
            {
                ItemChangedEvent(this);
            }
        }
    }

    public Item(Tile parent, int maxStack = 0)
    {
        this.Parent = parent;

        if (maxStack == 0)
        {
            this.maxStack = Data.StackLimit;
        }

        else
        {
            this.maxStack = maxStack;
        }
    }

    public void RegisterItemChanged(Action<Item> callback)
    {
        ItemChangedEvent += callback;
    }

    public void CreateNewStack(int amountToAdd, ItemType type)
    {
        if (TryAddingToStack(amountToAdd, type) > 0)
        {
            SendItemsToNeighbour(amountToAdd, type);
        }
    }

    public int TakeFromStack(int amountToTake, ItemType type)
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
    int TryAddingToStack(int amountToAdd, ItemType type)
    {

        if (this.Type == type || this.Type == ItemType.Empty && this.Parent.structure.Type == "Empty")
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
    void SendItemsToNeighbour(int amount, ItemType type)
    {
        int index = 1;
        while (amount > 0)
        {

            for (int x = Parent.x - index; x < Parent.x + 1 + index; x++)
            {
                for (int y = Parent.y - index; y < Parent.y + 1 + index; y++)
                {

                    if (amount == 0)
                    {
                        break;
                    }

                    try
                    {
                        amount = Parent.world.GetTile(x, y).Item.TryAddingToStack(amount, type);
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