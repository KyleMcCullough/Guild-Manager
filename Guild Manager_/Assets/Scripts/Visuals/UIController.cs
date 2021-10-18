using System.Security.Authentication;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{

    [SerializeField]
    UnityEngine.UI.Text tileDetails;
    [SerializeField]
    MouseController mouseController;
    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void Start()
    {
        this.mouseController = FindObjectOfType<MouseController>();
    }

    void Update()
    {
        Tile tile = world.GetTile(Mathf.FloorToInt(mouseController.mousePosition.x), Mathf.FloorToInt(mouseController.mousePosition.y));

        if (tile != null)
        {
            string text = $"{tile.x}-{tile.y}, {tile.Type.Replace("_", " ")}\n";

            if (tile.structure.IsConstructed)
            {
                text += tile.structure.Type.ToString().Replace("_", " ");
            }

            text += tile.world.rooms.IndexOf(tile.room);

            tileDetails.text = text;
        }
    }
}
