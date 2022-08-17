using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Inventory : IXmlSerializable
{
    int maxSlots;

    public bool isFull
    {
        get
        {
            return items.Count >= maxSlots;
        }
    }

    public List<Item> items {get; private set;}

    public Inventory(int maxSlots) 
    {
        items = new List<Item>();
        this.maxSlots = maxSlots;
    }

    public void AddItemFromTile(ref Item item)
    {
        item.CurrentStackAmount = this.AddItem(item.Type, item.CurrentStackAmount);
    }

    public int AddItem(string type, int amount)
    {
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
                Item.CreateNewItem(tile, item.type, item.CurrentStackAmount);
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

    public bool ContainsRequiredMaterials(List<buildingRequirement> requirements)
    {
        foreach (buildingRequirement requirement in requirements)
        {
            int amount = GetCountOfItemInInventory(requirement.material);

            if (amount < requirement.amount) return false;
        }
        return true;
    }

    public bool ContainsItem(string type)
    {
        foreach (Item item in items)
        {
            if (item.Type == type) return true;
        }

        return false;
    }

    public void TransferItem(Inventory i, string type, int amount)
    {
        if (!i.isFull && this.ContainsItem(type))
        {
            foreach (Item item in this.items.ToArray())
            {
                if (item.Type == type && item.CurrentStackAmount >= amount)
                {
                    i.AddItem(type, amount);
                    item.CurrentStackAmount -= amount;
                }
                
                else if (item.Type == type && item.CurrentStackAmount < amount)
                {
                    i.AddItem(type, item.CurrentStackAmount);
                    item.CurrentStackAmount = 0;
                }
            }
        }
    }

    #region Saving/Loading
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        string itemString = "";
        string amounts = "";

        foreach (Item item in items)
        {
            itemString += item.Type + "/";
            amounts += item.CurrentStackAmount + "/";
        }

        writer.WriteAttributeString("items", itemString);
        writer.WriteAttributeString("amounts", amounts);
    }
    #endregion
}