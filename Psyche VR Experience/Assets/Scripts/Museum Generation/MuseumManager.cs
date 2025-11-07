using System.Drawing;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class MuseumManager : MonoBehaviour
{
    public RoomModule roomModulePrefab;

    const float roomSize = 5;

    public RoomModule[][] roomGrid;

    public void Awake()
    {
        GenerateMuseum(20);
    }

    public void LoadModuleAsset()
    {
        GameObject funny = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Room Modules/Room_Module.prefab");
        roomModulePrefab = funny.GetComponent<RoomModule>();
    }

    public void InitMuseum(int size)
    {
        roomGrid = new RoomModule[size][];

        for (int i = 0; i < size; i++)
            roomGrid[i] = new RoomModule[size];
    }

    public void AlignAllRooms()
    {
        for (int x = 0; x < roomGrid.Length; x++)
        {
            for (int y = 0; y < roomGrid.Length; y++)
            {
                if (!InBounds(x, y, roomGrid.Length) || roomGrid[x][y] == null)
                    continue;

                AutoOpening(x, y);
            }
        }
    }

    void GenerateMuseum(int numArtPieces)
    {
        int size = (int)(numArtPieces);

        InitMuseum(3);

        GenSquare(0, 0, 3, 1);
        GenSquare(1, 0, 1, 3);
        AlignAllRooms();
    }

    public bool InBounds(int x, int y, int size)
    {
        return x >= 0 && x < size && y >= 0 && y < size;
    }

    public void GenSquare(int xTop, int yTop, int xSize, int ySize)
    {
        for(int x = xTop; x < xTop + xSize; x++)
        {
            for (int y = yTop; y < yTop + ySize; y++)
            {
                if (!InBounds(x, y, roomGrid.Length))
                    return;

                GenRoom(x, y);
            }
        }
    }

    public void GenRoom(int x, int y)
    {
        if (roomGrid[x][y] != null)
            return;

        if(roomModulePrefab == null)
        {
            LoadModuleAsset();
        }

        roomGrid[x][y] = Instantiate(roomModulePrefab);


        roomGrid[x][y].transform.position = new Vector3(x * roomSize, 0, y * roomSize);
    }

    public void AutoOpening(int x, int y)
    {
        RoomModule room = roomGrid[x][y];

        bool openNorth = y > 0 && roomGrid[x][y - 1] != null;
        bool openSouth = y < roomGrid.Length - 1 && roomGrid[x][y + 1] != null;
        bool openWest = x < roomGrid.Length - 1 && roomGrid[x + 1][y] != null;
        bool openEast = x > 0 && roomGrid[x - 1][y] != null;

        //if fully open, check if go pillarless
        if(openNorth && openSouth && openWest && openEast)
        {
            if (roomGrid[x - 1][y - 1] != null || roomGrid[x + 1][y - 1] != null 
                || roomGrid[x + 1][y + 1] != null || roomGrid[x - 1][y + 1] != null)
            {
                room.SetRoomActive(RoomModule.RoomType.FlatOpen);
                return;
            }
        }

        int numOpenings = 0;

        if (openNorth)
            numOpenings++;
        if (openSouth) 
            numOpenings++;
        if (openWest)
            numOpenings++;
        if (openEast)
            numOpenings++;

        //figure out if we go pillarless on 2 (only if not straight 2 opening)
        if(numOpenings == 2 && !(openNorth && openSouth || openWest && openEast))
        {
            int yOffset = openNorth ? -1 : 1;
            int xOffset = openWest ? 1 : -1;

            bool range = InRange(x + xOffset, y + yOffset, roomGrid.Length);
           
            //if we're in range and there's a room on the corner, we omit the beams
            if(range && roomGrid[x + xOffset][y + yOffset] != null)
            {
                room.SetOpenings(openNorth, openSouth, openWest, openEast);
                room.SetRoomActive(RoomModule.RoomType.TwoOpenLShapeFlat);
                return;
            }
        }

        //check whether to get rid of pillars on three openings
        if(numOpenings == 3)
        {
            Vector2Int gridOffset = new Vector2Int();

            if (!openNorth)
                gridOffset = new Vector2Int(0, 1);
            else if (!openSouth)
                gridOffset = new Vector2Int(0, -1);
            else if(!openWest)
                gridOffset = new Vector2Int(-1, 0);
            else if(!openEast)
                gridOffset = new Vector2Int(1, 0);

            //gridOffset += new Vector2Int(x, y);

            bool range = InRange(x + gridOffset.x, y + gridOffset.y, roomGrid.Length);

            int xN = x + gridOffset.x;
            int yN = y + gridOffset.y;

            Vector2Int diagonalOne = gridOffset;
            Vector2Int diagonalTwo = gridOffset;

            if(gridOffset.y != 0)
            {
                diagonalOne += new Vector2Int(1, 0);
                diagonalTwo += new Vector2Int(-1, 0);          
            } 
            else
            {
                diagonalOne += new Vector2Int(0, 1);
                diagonalTwo += new Vector2Int(0, -1);
            }

            bool diagonals = InRange(x + diagonalOne.x, y + diagonalOne.y, roomGrid.Length) && roomGrid[x + diagonalOne.x][y + diagonalOne.y] != null 
                || InRange(x + diagonalTwo.x, y + diagonalTwo.y, roomGrid.Length) && roomGrid[x + diagonalTwo.x][y + diagonalTwo.y] != null;

            if (range && roomGrid[xN][yN] != null && diagonals)
            {
                room.SetOpenings(openNorth, openSouth, openWest, openEast);
                room.SetRoomActive(RoomModule.RoomType.ThreeOpenFlat);
                return;
            }
        }

        room.SetOpenings(openNorth, openSouth, openWest, openEast);
    }

    bool InRange(int x, int y, int size)
    {
        return y > -1 && x > -1 && y < size && x < size;
    }
}
