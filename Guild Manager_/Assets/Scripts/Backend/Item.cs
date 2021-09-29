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
            this.maxStack = Settings.StackLimit;
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

    // Tries adding to a stack. Returns the amount that can't be added due to limit.
    int TryAddingToStack(int amountToAdd, ItemType type)
    {

        if (this.Type == ItemType.Empty && this.Parent.structure.Type == ObjectType.Empty) 
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
        int x = Parent.x;
        int y = Parent.y;

        int index = 1;
        while (amount > 0)
        {
            int[,] neighbours = {{x, y + index}, {x + index, y}, {x, y - index}, {x - index, y}, {x + index, y + index}, {x + index, y - index}, {x - index, y - index}, {x - index, y + index}};
            
            for (int i = 0; i < neighbours.Length / 2; i++)
            {

                amount = Parent.world.GetTile(neighbours[i,0], neighbours[i,1]).Item.TryAddingToStack(amount, type);

                if (amount == 0)
                {
                    break;
                }
            }
            index++;
        }
    }
}