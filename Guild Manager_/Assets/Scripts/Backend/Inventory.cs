using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory
{
    int maxSlots
    {
        get
        {
            return Data.MaxInventory;
        }
    }

    public bool isFull
    {
        get
        {
            return items.Count == maxSlots;
        }
    }

    public List<Item> items {get; private set;}

    public Inventory() 
    {
        items = new List<Item>();
    }

    public void AddItemFromTile(Item item)
    {
        item.CurrentStackAmount = this.AddItem(item.Type, item.CurrentStackAmount);
    }

    public int AddItem(string type, int amount)
    {
        Debug.Log("Adding " + type + " " + amount + " to inventory.");
        foreach (Item item in items)
        {
            if (item.Type == type && item.maxStack < item.CurrentStackAmount)
            {
                amount = TryAddingToInventory(amount, item);
            }
        }

        if (items.Count != maxSlots && amount > 0)
        {
            items.Add(new Item(type, amount, this));
            return 0;
        }

        return amount;
    }

    int TryAddingToInventory(int amountToAdd, Item item)
    {
        if (amountToAdd > item.maxStack)
        {
            return amountToAdd;
        }

        if (amountToAdd + item.CurrentStackAmount <= item.maxStack)
        {
            item.CurrentStackAmount += amountToAdd;
            return 0;
        }

        int remaining = item.maxStack - item.CurrentStackAmount;
        item.CurrentStackAmount += remaining;

        return amountToAdd - remaining;
    }

    public void DropItem(Tile tile, string type)
    {
        foreach (Item item in items.ToArray())
        {
            if (item.type == type)
            {
                item.AssignParent(tile, item.CurrentStackAmount, item.type);
                items.Remove(item);
            }
        }
    }

    public void DeleteItem(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
        }
    }

    public int GetCountOfItemInInventory(string type)
    {
        int count = 0;
        foreach (Item item in items.ToArray())
        {
            if (item.type == type)
            {
                count += item.CurrentStackAmount;
            }
        }
        return count;
    }

    public bool ContainsRequiredMaterials(List<BuildingRequirements> requirements)
    {
        // int contains = 0;
        // foreach (BuildingRequirements requirement in requirements)
        // {
        //     foreach (Item item in items.ToArray())
        //     {
        //         if (requirement.material == item.Type && item.CurrentStackAmount >= requirement.amount)
        //         {
        //             contains += 1;
        //         }
        //     }
        // }
        // return contains;

        foreach (BuildingRequirements requirement in requirements)
        {
            int amount = GetCountOfItemInInventory(requirement.material);

            if (amount < requirement.amount) return false;
        }
        return true;
    }
}