using UnityEngine;

public class MuseumManager : MonoBehaviour
{
    public RoomModule roomModulePrefab;

    const float roomSize = 5;

    RoomModule[][] roomGrid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateMuseum(20);
    }

    void GenerateMuseum(int numArtPieces)
    {
        int size = (int)(numArtPieces);
        roomGrid = new RoomModule[size][];

        for (int i = 0; i < size; i++)
            roomGrid[i] = new RoomModule[size];

        int numDisplays = 0;


        //GenSquare(0, 0, numArtPieces / 2, numArtPieces / 2);
        
        Vector2Int start1 = new Vector2Int(8, 0);

        Vector2Int start2 = new Vector2Int(0, 8);
        
        GenSquare(start1.x, start1.y, size, 5);
        GenSquare(start2.x, start2.y, 4, size);
        GenSquare(0, 10, size, 1);
        GenSquare(12, 0, 1, size);

        //while (numDisplays < numArtPieces)
        //{

        //}

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (!InBounds(x, y, roomGrid.Length) || roomGrid[x][y] == null)
                    continue;

                AutoOpening(x, y);
            }
        }
    }

    bool InBounds(int x, int y, int size)
    {
        return x >= 0 && x < size && y >= 0 && y < size;
    }

    void GenSquare(int xTop, int yTop, int xSize, int ySize)
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

    void GenRoom(int x, int y)
    {
        if (roomGrid[x][y] != null)
            return;

        roomGrid[x][y] = Instantiate(roomModulePrefab);

        roomGrid[x][y].transform.position = new Vector3(x * roomSize, 0, y * roomSize);
    }

    void AutoOpening(int x, int y)
    {
        RoomModule room = roomGrid[x][y];

        bool openNorth = y > 0 && roomGrid[x][y - 1] != null;
        bool openSouth = y < roomGrid.Length - 1 && roomGrid[x][y + 1] != null;
        bool openWest = x < roomGrid.Length - 1 && roomGrid[x + 1][y] != null;
        bool openEast = x > 0 && roomGrid[x - 1][y] != null;

        if(openNorth && openSouth && openWest && openEast)
        {
            if (roomGrid[x - 1][y - 1] != null && roomGrid[x + 1][y - 1] != null 
                && roomGrid[x + 1][y + 1] != null && roomGrid[x - 1][y + 1] != null)
            {
                room.SetRoomActive(RoomModule.RoomType.FlatOpen);
                return;
            }
        } 

        room.SetOpenings(openNorth, openSouth, openWest, openEast);
    }
}
