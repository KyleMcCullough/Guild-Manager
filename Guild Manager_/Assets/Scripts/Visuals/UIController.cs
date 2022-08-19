using System.Security.Authentication;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{

    [SerializeField]
    UnityEngine.UI.Text tileDetails;
    [SerializeField]
    MouseController mouseController;
    [SerializeField]
    GameObject console;
    [SerializeField]
    TextMeshProUGUI consoleOutput;
    [SerializeField]
    Scrollbar verticalScrollbar;

    World world
    {
        get { return WorldController.Instance.World; }
    }

    void Start() 
    {
        DebugConsole.ConsoleUpdated += RefreshConsole;
    }

    void Update()
    {
        Tile tile = world.GetTile(Mathf.FloorToInt(mouseController.mousePosition.x), Mathf.FloorToInt(mouseController.mousePosition.y));

        if (tile != null)
        {
            string text = $"{tile.x}-{tile.y}, {tile.Type.Replace("_", " ")}\n";

            if (tile.structure.Type != ObjectType.Empty)
            {
                text += $"{tile.structure.Type.Replace("_", " ")}\n";
            }

            if (tile.structure.parentStructure != null)
            {
                text += tile.structure.parentStructure.Type.ToString().Replace("_", " ");
            }

            else if (tile.structure.IsConstructed)
            {
                text += tile.structure.Type.ToString().Replace("_", " ");
            }

            else if (tile.item != null && tile.item.CurrentStackAmount > 0)
            {
                text += tile.item.Type.ToString() + " " + tile.item.CurrentStackAmount + " Room ";
            }

            text += tile.world.rooms.IndexOf(tile.room);

            tileDetails.text = text;
        }

        ManageConsole();
    }

    public void ManageConsole()
    {

        if (Input.GetKeyDown(KeyCode.Backslash)) {
            console.SetActive(!console.activeInHierarchy);
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            DebugConsole.WriteInfo("logging");
            DebugConsole.WriteWarning("warning");
            DebugConsole.WriteError("erroring");
        }

        if (Input.GetKeyDown(KeyCode.O)) {
            DebugConsole.Dump();
        }
    }

    public void RefreshConsole(string tag, string content)
    {

        switch(tag){
            case "Info": {
                consoleOutput.text += content + "\n";
                break;
            }
            case "Warning": {
                consoleOutput.text += "<color=yellow>" + content + "</color> \n";
                break;
            }
            case "Error": {
                consoleOutput.text += "<color=red>" + content + "</color> \n";
                break;
            }
        }

        verticalScrollbar.value = 0;
    }
}
