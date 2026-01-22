using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;


public class MuseumManager : MonoBehaviour
{
    public RoomModule roomModulePrefab;

    const float roomSize = 5;

    public RoomModule[][] roomGrid;
    public Transform firstRoom;

    [SerializeField] string dbPathOverride = null;

    [Tooltip("Automatically populate frames on Start after building the museum?")]
    [SerializeField] bool populateArt = true;
    public int numArtPieces = 100;

    [Tooltip("Sets ArtFrames asynchronously, with a delay between the population of each frame.")]
    [SerializeField] bool asyncPopulate = true;
    [SerializeField] float populateDelay = 0.1f;

    private int numFrames = 0;

    //make sure this is no less than 9 probably
    const int chunkSize = 11;
    const int chunkMid = chunkSize / 2 + 1;
    struct SquareRoom
    {
        public int x, y, width, height;
        public bool add;

        public SquareRoom(int x, int y, int width, int height, bool add = true)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.add = add;
        }
    }

    class RoomPattern
    {
        public SquareRoom[] squares;

        public RoomPattern(SquareRoom[] squares)
        {
            this.squares = squares;
        }
    }

    RoomPattern[] patterns = new RoomPattern[]
    {
        new RoomPattern(new SquareRoom[]
        {
            new SquareRoom(3, 3, chunkSize - 4, chunkSize - 4),
            new SquareRoom(chunkMid - 1, chunkMid - 1, 3, 3, false)
        }),

        new RoomPattern(new SquareRoom[]
        {
            new SquareRoom(2, chunkMid, chunkSize - 2, 1),
            new SquareRoom(chunkMid, chunkMid - 1, 1, 1)
        }),

        new RoomPattern(new SquareRoom[]
        {
            new SquareRoom(-2, 1, chunkSize + 4, chunkSize - 2),
            new SquareRoom(chunkSize / 3, chunkMid - 1, 2, 2, false)
        }),

        new RoomPattern(new SquareRoom[]
        {
            new SquareRoom(3, 3, chunkSize - 3, chunkSize - 3)
        })
    };

    public async void Awake()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        await GenerateMuseum(numArtPieces);

        sw.Stop();
        Debug.Log("[Museum Manager] Time Elapsed: " + sw.ElapsedMilliseconds + " milliseconds");
    }

    public int ChunkCountForArtPieces(int numArtPieces)
    {
        //approximate error of +/- 20ish per chunk

        //1 chunk ~= 33
        //2 chunks ~= 130
        //3 chunks ~= 330
        //4 chunks ~= 583
        //5 chunks ~= 911
        //6 chunks ~= 1300

        float result = 12226f + (0.0014f - 12226f) / (1 + Mathf.Pow(numArtPieces / 377528.4f, 0.5f));
        result /= 100;

        int chunkCount = (int)(result + 1);

        return chunkCount;
    }

    public async Awaitable GenerateMuseum(int numArtPieces)
    {
        numFrames = 0;
        int size = (int)(numArtPieces);
        int numChunks = ChunkCountForArtPieces(numArtPieces); //TODO: Figure out the math for this
        InitMuseum(chunkSize * numChunks);

        Vector2Int startPos = new Vector2Int(numChunks/2, 0);
        Debug.Log("Starting Position: " + startPos.x + " " + startPos.y);
        bool[][] chunksTraversed = new bool[numChunks][];

        for(int i = 0; i < chunksTraversed.Length; i++) { chunksTraversed[i] = new bool[numChunks]; }

        RecurseChunks(startPos, ref chunksTraversed);

        AlignAllRooms();

        //Debug.Log("Count: " + CountArtSpots());

        if(populateArt)
            await AssignArt(numArtPieces);
    }

    public void InitMuseum(int size)
    {
        roomGrid = new RoomModule[size][];

        for (int i = 0; i < size; i++) 
        {
            roomGrid[i] = new RoomModule[size];
        }
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

                numFrames += roomGrid[x][y].GetNumArtDisplays();
            }
        }

        Debug.Log("numFrames = " + numFrames);
    }

    public void GenerateRandomRoomPattern(int chunkX, int chunkY)
    {
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;
        int roomIndex = Random.Range(0, patterns.Length);

        RoomPattern pattern = patterns[roomIndex];

        //GenRoom(startX, startY);
        //GenRoom(startX + chunkSize - 1, startY + chunkSize - 1);

        for (int i = 0; i < pattern.squares.Length; i++)
        {
            SquareRoom square = pattern.squares[i];
            if (square.add)
            {
                GenSquare(square.x + startX, square.y + startY, square.width, square.height);
            } else
            {
                RemoveSquare(square.x + startX, square.y + startY, square.width, square.height);
            }
        }
    }

    void RecurseChunks(Vector2Int startPos, ref bool[][] chunksTraversed)
    {
        GenerateRandomRoomPattern(startPos.x, startPos.y);

        List<Vector2Int> directions = new List<Vector2Int>() { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        //loop over every direction we checked in the first loop
        for (int i = 0; i < directions.Count; i++)
        {
            Vector2Int dir = directions[i];

            Vector2Int nextPos = startPos + dir;

            //if we haven't visited this chunk yet, mark it so and recurse on it
            if (InBounds(nextPos.x, nextPos.y, chunksTraversed.Length) && !chunksTraversed[nextPos.x][nextPos.y])
            {
                chunksTraversed[nextPos.x][nextPos.y] = true;

                RecurseChunks(nextPos, ref chunksTraversed);

                //connect the two chunk

                //get position on middle of boundary
                Vector2Int startRoomPos = (startPos * chunkSize) + new Vector2Int(chunkMid, chunkMid) + chunkMid * dir;

                //generate path towards room from mid
                Vector2Int current = startRoomPos;
                while (InBounds(current.x, current.y, roomGrid.Length) && roomGrid[current.x][current.y] == null)
                {
                    GenRoom(current.x, current.y);
                    current += dir;
                }

                //generate path towards original room from mid
                current = startRoomPos - dir;

                int tolerance = 3;
                while (InBounds(current.x, current.y, roomGrid.Length) && roomGrid[current.x][current.y] != null && tolerance > 0)
                {
                    current -= dir;
                    tolerance--;
                }

                while (InBounds(current.x, current.y, roomGrid.Length) && roomGrid[current.x][current.y] == null)
                {
                    GenRoom(current.x, current.y);
                    current -= dir;
                }
            }
        }
    }

    public int CountArtSpots()
    {
        int total = 0;

        for (int x = 0; x < roomGrid.Length; x++)
        {
            for (int y = 0; y < roomGrid[x].Length; y++)
            {
                if (roomGrid[x][y] != null)
                {
                    total += roomGrid[x][y].GetNumArtDisplays();
                }
            }
        }

        return total;
    }

    public async Awaitable AssignArt(int numArtPieces)
    {
        //int numSpots = CountArtSpots();
        int numSpots = numFrames;

        //take the minimum between the two, we can't request more art than we have spots for
        int requestCount = Mathf.Min(numSpots, numArtPieces);

        List<ArtworkData> items = PsycheDBMiddleware.CreateRandomProjectSObjects(requestCount, dbPathOverride);
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[MuseumManager] PopulateDisplays: Middleware returned no ArtworkData.");
            return;
        }

        int spotsFilled = 0;

        int numSpaces = (numSpots / numArtPieces) - 2;
        int n = 0;

        if (numSpaces == 0)
        {
            numSpaces = 1;
        }
        
        for (int x = 0; x < roomGrid.Length; x++)
        {
            for (int y = 0; y < roomGrid[x].Length; y++)
            {
                if (roomGrid[x][y] != null)
                {
                    if (spotsFilled >= numArtPieces)
                    {
                        await roomGrid[x][y].SetArtDisplays(0);
                        continue;
                    }

                    int numSet = roomGrid[x][y].GetNumArtDisplays();

                    if (numSet >= 1)
                    {
                        if (numSpots - spotsFilled > numArtPieces * 2)
                        {
                            if (n != 0)
                                numSet = 0;
                            else
                                numSet = 1;

                                n++;
                            n %= numSpaces;
                        }
                        else if(spotsFilled + numSet > numArtPieces)
                        {
                            numSet = numArtPieces - spotsFilled;
                        }
                    }

                    await roomGrid[x][y].SetArtDisplays(numSet, items, spotsFilled, asyncPopulate, populateDelay);

                    spotsFilled += numSet;
                }
            }
        }

        //Debug.Log("SpotsFilled: " + spotsFilled + " NumArtPieces: " + numArtPieces);
    }

    public void LoadModuleAsset()
    {
        GameObject funny = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Room Modules/Room_Module.prefab");
        roomModulePrefab = funny.GetComponent<RoomModule>();
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
                    continue;

                GenRoom(x, y);
            }
        }
    }

    public void RemoveSquare(int xTop, int yTop, int xSize, int ySize)
    {
        for (int x = xTop; x < xTop + xSize; x++)
        {
            for (int y = yTop; y < yTop + ySize; y++)
            {
                if (!InBounds(x, y, roomGrid.Length))
                    continue;

                Destroy(roomGrid[x][y].gameObject);
                roomGrid[x][y] = null;
            }
        }
    }

    public void GenRoom(int x, int y)
    {
        if (!InBounds(x, y, roomGrid.Length) || roomGrid[x][y] != null)
            return;


        if(roomModulePrefab == null)
        {
            LoadModuleAsset();
        }

        roomGrid[x][y] = Instantiate(roomModulePrefab);

        roomGrid[x][y].transform.position = new Vector3(x * roomSize, 0, y * roomSize);

        if(firstRoom == null)
        {
            firstRoom = roomGrid[x][y].transform;
        }
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
                room.SetOpenings(openNorth, openSouth, openWest, openEast, false);
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
                room.SetOpenings(openNorth, openSouth, openWest, openEast, false);
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
