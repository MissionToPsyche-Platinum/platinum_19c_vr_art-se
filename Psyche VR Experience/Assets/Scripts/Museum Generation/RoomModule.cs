using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
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
        TwoOpenLShapeFlat,
        ThreeOpenFlat,
        FlatOpen,
        SIZE
    }

    public RoomType roomType = RoomType.OneOpen;

    public GameObject[] roomModels;
    public ArtDisplayList[] artDisplayLists;

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
        public bool openNorth;
        public bool openSouth;
        public bool openWest;
        public bool openEast;

        public int numArt;
        public int numDirections = 0;

        public RoomInfo(bool openNorth, bool openSouth, bool openWest, bool openEast, int numArt)
        {
            this.openNorth = openNorth;
            this.openSouth = openSouth;
            this.openWest = openWest;
            this.openEast = openEast;
            this.numArt = numArt;

            if (openNorth)
                numDirections++;
            if (openSouth)
                numDirections++;
            if (openWest)
                numDirections++;
            if (openEast)
                numDirections++;
        }
    }

    ////boolean openings in each room are ordered: North, South, West, East
    Dictionary<RoomType, RoomInfo> roomInfos = new Dictionary<RoomType, RoomInfo> {
        {RoomType.OneOpen, new RoomInfo(false, true, false, false, 3) },             
        {RoomType.TwoOpenLShape, new RoomInfo(false, true, true, false, 2) },     
        {RoomType.TwoOpenStraight,   new RoomInfo(true, true, false, false, 2) },     
        {RoomType.ThreeOpen,   new RoomInfo(false, true, true, true, 1) },           
        {RoomType.FourOpen,   new RoomInfo(true, true, true, true, 0) },           
        {RoomType.TwoOpenLShapeFlat, new RoomInfo(false, true, true, false, 2) },
        {RoomType.ThreeOpenFlat,   new RoomInfo(false, true, true, true, 1) },
        {RoomType.FlatOpen,   new RoomInfo(true, true, true, true, 0) }
    };

    //number of openings in each room
    int[] roomOpeningCounts = new int[(int)RoomType.SIZE];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //SetupActiveRoom();
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

        roomModels[(int)roomType].gameObject.SetActive(true);
        UpdateRoomOpenings();
    }

    /// <summary>
    /// Deactivates the previous room model and activates the model given. Then, sets the roomType property to the new value.
    /// </summary>
    /// <param name="nRoomType">The new room shape to use</param>
    public void SetRoomActive(RoomType nRoomType)
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
        roomModels[((int)roomType)].gameObject.SetActive(false);
        roomModels[(int)nRoomType].gameObject.SetActive(true);
        roomType = nRoomType;
        UpdateRoomOpenings();
    }

    /// <summary>
    /// Uses the orientation property to determine which sides will be open considering the given roomInfos of the selected room.
    /// Assumes that -Z is pointing towards our indicated direction.
    /// </summary>
    private void UpdateRoomOpenings()
    {
        bool[] info = RotatedRoom(roomType, orientation);

        openNorth = info[0];
        openSouth = info[1];
        openWest = info[2];
        openEast = info[3];
    }

    bool[] RotatedRoom(RoomType room, Orientation orient)
    {
        RoomInfo info = roomInfos[room];
        bool[] final = new bool[4];

        switch (orient)
        {
            case Orientation.North:
                final[0] = info.openNorth; 
                final[1] =  info.openSouth;
                final[2] = info.openWest;  
                final[3] = info.openEast;  
                break;
            case Orientation.South:
                final[0] =  info.openSouth;
                final[1] =  info.openNorth;
                final[2] = info.openEast;  
                final[3] = info.openWest;  
                break;
            case Orientation.West:
                final[0] =  info.openEast; 
                final[1] =  info.openWest; 
                final[2] = info.openNorth; 
                final[3] = info.openSouth; 
                break;
            case Orientation.East:
                final[0] =  info.openWest; 
                final[1] =  info.openEast; 
                final[2] = info.openSouth; 
                final[3] = info.openNorth; 
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

        for (int i = 0; i < directions.Length; i++)
        {
            if (directions[i]) count++;
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
            int roomSize = roomInfos[(RoomType)i].numDirections;

            if (roomSize == numOpenings)
            {
                rooms.Add((RoomType)i);
            }
        }

        if (rooms.Count == 0)
        {
            return RoomType.FourOpen;
        }

        if (numOpenings == 2)
        {
            if (openings[0] && openings[1] || openings[2] && openings[3])
            {
                return RoomType.TwoOpenStraight;
            }
            else
            {
                return RoomType.TwoOpenLShape;
            }
        }

        return rooms[0];
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

    public int GetNumArtDisplays()
    {
        return this.roomInfos[this.roomType].numArt;
    }

    /// <summary>
    /// Set some number of frames in the room to art from the given list, starting from start_index and iterating along numSet times.
    /// </summary>
    /// <returns>Number of art displays set.</returns>
    public async Awaitable SetArtDisplays(int numSet, List<ArtworkData> art = null, int start_index = 0)
    {
        for (int i = 0; i < artDisplayLists[(int)roomType].artFrames.Length; i++)
        {
            if(i < numSet)
            {
                GameObject frameObject = artDisplayLists[(int)roomType].artFrames[i];
                FrameController frame = frameObject.GetComponent<FrameController>();

                frame.SetArtwork(art[start_index + i]);
            }
            else if(artDisplayLists[(int)roomType].artFrames.Length > i)
            {
                GameObject frameObject = artDisplayLists[(int)roomType].artFrames[i];

                if(frameObject != null)
                    frameObject.gameObject.SetActive(false);
            }

            await Awaitable.WaitForSecondsAsync(0.01f);
        }

    }

    /// finds the active room model variant and returns it
    /// prefers roomModels[(int)roomType], but safely falls back to any active model.
    public GameObject GetActiveModelRoot()
    {
        // roomModels[(int)roomType] is the one we actively set, but also check activeSelf
        var go = roomModels[(int)roomType];
        if (go != null && go.activeSelf) return go;

        // find any active in case something changed externally(shouldn't happen in any normal circumstance)
        foreach (var m in roomModels)
            if (m != null && m.activeSelf) return m;

        return null;
    }
}
