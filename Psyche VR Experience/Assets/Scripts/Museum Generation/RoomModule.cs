using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static RoomModule;

public class RoomModule : MonoBehaviour
{
    public enum RoomType
    {
        OneOpen,
        TwoOpenLShape,
        TwoOpenStraight,
        ThreeOpen,
        FourOpen,
        FlatOpen,
        SIZE
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

    public class RoomInfo
    {
        bool openNorth;
        bool openSouth;
        public bool openWest;
        public bool openEast;

        int numArt;

        public RoomInfo(bool openNorth, bool openSouth, bool openWest, bool openEast, int numArt)
        {
            this.openNorth = openNorth;
            this.openSouth = openSouth;
            this.openWest = openWest;
            this.openEast = openEast;
            this.numArt = numArt;
        }
    }

    ////boolean openings in each room are ordered: North, South, West, East
    Dictionary<RoomType, RoomInfo> roomInfos = new Dictionary<RoomType, RoomInfo> {
        {RoomType.OneOpen, new RoomInfo(false, true, false, false, 0) },             
        {RoomType.TwoOpenLShape, new RoomInfo(false, true, true, false, 0) },     
        {RoomType.TwoOpenStraight,   new RoomInfo(true, true, false, false, 0) },     
        {RoomType.ThreeOpen,   new RoomInfo(false, true, true, true, 0) },           
        {RoomType.FourOpen,   new RoomInfo(true, true, true, true, 0) },           
        {RoomType.FlatOpen,   new RoomInfo(true, true, true, true, 0) }              
    };

    //number of openings in each room
    int[] roomOpeningCounts = new int[(int)RoomType.SIZE];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SetupActiveRoom();

        for (int i = 0; i < (int)RoomType.SIZE; i++)
        {
            roomOpeningCounts[i] = CountNumDirections(roomInfos[(RoomType)i]);
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

    bool[] RotatedRoom(RoomType room, Orientation orient)
    {
        bool[] info = roomInfos[room];
        bool[] final = new bool[4];

        switch (orient)
        {
            case Orientation.North:
                final[0] = info[0];
                final[1] = info[1];
                final[2] = info[2];
                final[3] = info[3];
                break;
            case Orientation.South:
                final[0] = info[1]; //SOUTH
                final[1] = info[0]; //NORTH
                final[2] = info[3];  //EAST
                final[3] = info[2];  //WEST
                break;
            case Orientation.West:
                final[0] = info[3]; //EAST
                final[1] = info[2]; //WEST
                final[2] = info[0]; //NORTH
                final[3] = info[1];  //SOUTH
                break;
            case Orientation.East:
                final[0] = info[2]; //WEST
                final[1] = info[3]; //EAST
                final[2] = info[1]; //SOUTH
                final[3] = info[0]; //NORTH
                break;
        }

        return final;
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


    int CountNumDirections(bool[] directions)
    {
        int count = 0;

        for(int i = 0; i < directions.Length; i++)
        {
            if(directions[i]) count++;
        }

        return count;
    }

    /// <summary>
    /// Takes in a boolean list of openings {north, south, west, east} and figures out which room shape fits.
    /// This function is a bit of a mess, but it should do the job.
    /// </summary>
    /// <param name="openings">Boolean openings for each direction {north, south, west, east}</param>
    /// <returns>The RoomType corresponding to the shape of openings</returns>
    RoomType FigureOutRoomTypeFromOpenings(bool[] openings)
    {
        List<RoomType> rooms = new List<RoomType>();

        int numOpenings = CountNumDirections(openings);

        //this will need to be updated whenever new rooms are added
        for (int i = 0; i < (int)RoomType.SIZE; i++)
        {
            int roomSize = roomOpeningCounts[i];

            if (roomSize == numOpenings)
            {
                rooms.Add((RoomType)i);
            }
        }

        if (rooms.Count == 0)
        {
            return RoomType.SIZE;
        }

        if (rooms.Count == 1)
            return rooms[0];

        if (openings[0] && openings[1] || openings[2] && openings[3])
        {
            return RoomType.TwoOpenStraight;
        }
        else
        {
            return RoomType.TwoOpenLShape;
        }
    }

    public void SetOpenings(bool north, bool south, bool west, bool east)
    {
        bool[] directions = new bool[4] { north, south, west, east };

        RoomType room = FigureOutRoomTypeFromOpenings(directions);

        if(room == RoomType.SIZE)
        {
            Debug.LogError("ERROR: NUMBER OF OPENINGS SUPPORTS NO LOGGED ROOM TYPE");
            return;
        }

        //guess and check! there's probably a better way to do this
        Orientation dir = Orientation.North;
        
        for (int i = 0; i < 4; i++)
        {
            dir = (Orientation)i;
            bool[] info = RotatedRoom(room, dir);
                
            if(info[0] == directions[0] && info[1] == directions[1] && info[2] == directions[2] && info[3] == directions[3])
            {
                break;
            }
        }

        SetRoomActive(room, dir);
    }
}
