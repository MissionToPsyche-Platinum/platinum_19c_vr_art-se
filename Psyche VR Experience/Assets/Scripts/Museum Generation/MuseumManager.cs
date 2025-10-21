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


        GenSquare(0, 0, numArtPieces / 2, numArtPieces / 2);

        //while (numDisplays < numArtPieces)
        //{

        //}
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

        for (int x = xTop; x < xTop + xSize; x++)
        {
            for (int y = yTop; y < yTop + ySize; y++)
            {
                if (!InBounds(x, y, roomGrid.Length))
                    return;

                AutoOpening(x, y);
            }
        }
    }

    void GenRoom(int x, int y)
    {
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

        room.SetOpenings(openNorth, openSouth, openWest, openEast);
    }
}
