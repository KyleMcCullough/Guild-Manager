using UnityEngine;

public class Door 
{

    Tile Parent;
    bool isOpen = false;
    bool openDoor = false;
    float openTime;
    float currentOpenValue;

    public Door(Tile tile, float openTime)
    {
        this.Parent = tile;
        this.openTime = openTime;
    }

    public void OpenDoor()
    {
        this.openDoor = true;
    }

    public void CloseDoor()
    {
        this.openDoor = false;
    }

    public bool CheckIfOpen()
    {
        return this.isOpen;
    }

    public void Update(float deltaTime)
    {
        if (this.openDoor)
        {
            this.currentOpenValue += (1 * deltaTime);
        }

        else
        {
            if (this.currentOpenValue > 0)
            {
                this.currentOpenValue -= (1 * deltaTime);
            }
        }

        if (this.currentOpenValue >= this.openTime)
        {
            this.isOpen = true;
        }
    }
}