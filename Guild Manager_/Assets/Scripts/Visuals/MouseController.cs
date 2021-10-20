using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Events;
using UnityEngine.EventSystems;


public class MouseController : MonoBehaviour
{

    Vector3 lastMousePosition;
    Vector3 dragStartPosition;
    public Vector3 mousePosition;
    List<GameObject> dragPreviewArea;
    public GameObject highlightCursorPrefab;
    public float CameraMoveSpeed = 10f;
    BuildController buildController;
    bool allowDragging = true;

    // Start is called before the first frame update
    void Start()
    {
        dragPreviewArea = new List<GameObject>();
        buildController = GameObject.FindObjectOfType<BuildController>();
    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Gets currently selected tile.
        // Update cursor position.
        Tile tileUnderMouse = WorldController.Instance.GetTileAtCoordinate(mousePosition);

        // Enables/Disables dragging depending on placement mode.
        if (buildController.buildType == 1 && Data.structureData[buildController.buildObject].placementMode == 1)
        {
            this.allowDragging = false;
        }
        
        else
        {
            this.allowDragging = true;
        }

        if (!this.allowDragging && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Tile tile = WorldController.Instance.GetTileAtCoordinate(mousePosition);

            if (tile != null)
            {
                buildController.Build(tile);
            }
        }

        else
        {
            // Handles left mouse clicks/drags.
            UpdateTileDragging();
        }

        UpdateCameraMovement();

        lastMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastMousePosition.z = 0;
    }

    void UpdateCameraMovement()
    {
        // Handles screen draggin with right and middle mouse buttons.
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 difference = lastMousePosition - mousePosition;
            Camera.main.transform.Translate(difference);
        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);


        Vector3 updatedMovement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            updatedMovement += new Vector3(0, CameraMoveSpeed, 0) * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            updatedMovement += new Vector3(0, -CameraMoveSpeed, 0) * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            updatedMovement += new Vector3(-CameraMoveSpeed, 0, 0) * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            updatedMovement += new Vector3(CameraMoveSpeed, 0, 0) * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            updatedMovement *= 2;
        }

        Camera.main.transform.position += updatedMovement;
    }

    //TODO: Set a max area for tile to optimize performance.
    void UpdateTileDragging()
    {

        // Checks if we are over a UI element.
        if (EventSystem.current.IsPointerOverGameObject() || Input.GetMouseButtonDown(1) || !allowDragging) return;

        // Starts drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = mousePosition;
        }

        int startX = Mathf.FloorToInt(dragStartPosition.x);
        int endX = Mathf.FloorToInt(mousePosition.x);
        int startY = Mathf.FloorToInt(dragStartPosition.y);
        int endY = Mathf.FloorToInt(mousePosition.y);

        //FIXME: May cause issues with some objects.
        // Only allows objects to be dragged on either x or y axis.
        if (buildController.buildType == 1 && Data.structureData[buildController.buildObject].placementMode == 2)
        {
            if (Math.Abs(endY - startY) > Math.Abs(endX - startX))
            {
                endX = startX;
            }

            else
            {
                endY = startY;
            }
        }

        // Swaps the positions if start x is before end x.
        if (endX < startX)
        {
            int temp = endX;
            endX = startX;
            startX = temp;
        }

        // Swaps the positions if start x is before end x.
        if (endY < startY)
        {
            int temp = endY;
            endY = startY;
            startY = temp;
        }


        // Garbage collect old drag preview images.
        while (dragPreviewArea.Count > 0)
        {
            GameObject obj = dragPreviewArea[0];
            dragPreviewArea.RemoveAt(0);
            SimplePool.Despawn(obj);
        }

        if (Input.GetMouseButton(0) && this.allowDragging)
        {

            // Display preview of drag area.
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTile(x, y);

                    if (tile != null)
                    {
                        GameObject obj = SimplePool.Spawn(highlightCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);

                        obj.transform.SetParent(this.transform, true);
                        dragPreviewArea.Add(obj);
                    }
                }
            }
        }


        // Ends drag
        if (Input.GetMouseButtonUp(0) && this.allowDragging)
        {
            int num = 0;
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTile(x, y);

                    num++;
                    if (tile != null)
                    {
                        buildController.Build(tile);
                    }
                }
            }
        }
    }
}
