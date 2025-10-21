using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class RoomModule : MonoBehaviour
{
    public enum RoomType
    {
        OneOpen,
        TwoOpenLShape,
        TwoOpenStraight,
        ThreeOpen,
        FourOpen
    }

    public RoomType roomType = RoomType.OneOpen;

    public GameObject[] roomModels;

    //North is -Z, South is +Z, West is +X, East is -X
    public bool openNorth, openSouth, openWest, openEast;

    public enum Orientation
    {
        North,
        South,
        West,
        East
    }

    //indicates the direction that -Z points
    public Orientation orientation = Orientation.North;

    ////boolean openings in each room are ordered: North, South, West, East
    Dictionary<RoomType, bool[]> roomInfos = new Dictionary<RoomType, bool[]> {
        {RoomType.OneOpen, new bool[] {false, true, false, false } },             
        {RoomType.TwoOpenLShape,    new bool[] {false, true, true, false } },     
        {RoomType.TwoOpenStraight,   new bool[] {true, true, false, false} },     
        {RoomType.ThreeOpen,   new bool[] {false, true, true, true } },           
        {RoomType.FourOpen,   new bool[] {true, true, true, true } }              
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupActiveRoom();
        Demo();
    }

    private async void Demo()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (gameObject == null)
                {
                    return;
                }

                SetRoomActive((RoomType)i, (Orientation)j);
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// Deactivates all room objects first and sets the roomType room to active. Useful for setup.
    /// </summary>
    /// <param name="nRoomType"></param>
    private void SetupActiveRoom()
    {
        foreach (GameObject roomModel in roomModels)
        {
            roomModel.SetActive(false);
        }

        roomModels[(int)roomType].SetActive(true);
        UpdateRoomOpenings();
    }

    /// <summary>
    /// Deactivates the previous room model and activates the model given. Then, sets the roomType property to the new value.
    /// </summary>
    /// <param name="nRoomType">The new room shape to use</param>
    private void SetRoomActive(RoomType nRoomType)
    {
        roomModels[((int)roomType)].SetActive(false);
        roomModels[(int)nRoomType].SetActive(true);
        roomType = nRoomType;
        UpdateRoomOpenings();
    }

    /// <summary>
    /// Overflow that also simultaneously sets the orientation for convenience
    /// </summary>
    /// <param name="nRoomType">The new room shape to use</param>
    /// <param name="roomOrientation">The new orientation</param>
    private void SetRoomActive(RoomType nRoomType, Orientation roomOrientation)
    {
        if (roomOrientation != orientation)
        {
            SetOrientation(roomOrientation, false);
        }

        roomModels[((int)roomType)].SetActive(false);
        roomModels[(int)nRoomType].SetActive(true);
        roomType = nRoomType;
        UpdateRoomOpenings();
    }

    /// <summary>
    /// Uses the orientation property to determine which sides will be open considering the given roomInfos of the selected room.
    /// Assumes that -Z is pointing towards our indicated direction.
    /// </summary>
    private void UpdateRoomOpenings()
    {
        bool[] info = roomInfos[roomType];

        switch (orientation)
        {
            case Orientation.North:
                openNorth = info[0];
                openSouth = info[1];
                openWest = info[2];
                openEast = info[3];
                break;
            case Orientation.South:
                openNorth = info[1]; //SOUTH
                openSouth = info[0]; //NORTH
                openWest = info[3];  //EAST
                openEast = info[2];  //WEST
                break;
            case Orientation.West:
                openNorth = info[3]; //EAST
                openSouth = info[2]; //WEST
                openWest = info[0]; //NORTH
                openEast = info[1];  //SOUTH
                break;
            case Orientation.East:
                openNorth = info[2]; //WEST
                openSouth = info[3]; //EAST
                openWest = info[1]; //SOUTH
                openEast = info[0]; //NORTH
                break;
        }
    }

    public void SetOrientation(Orientation orientation, bool updateRoomOpenings = true)
    {
        this.orientation = orientation;

        switch (orientation)
        {
            case Orientation.North:
                transform.localEulerAngles = new Vector3();
                break;
            case Orientation.South:
                transform.localEulerAngles = new Vector3(0, 180, 0);
                break;
            case Orientation.West:
                transform.localEulerAngles = new Vector3(0, -90, 0);
                break;
            case Orientation.East:
                transform.localEulerAngles = new Vector3(0, 90, 0);
                break;
        }

        if (updateRoomOpenings)
            UpdateRoomOpenings();
    }
}
