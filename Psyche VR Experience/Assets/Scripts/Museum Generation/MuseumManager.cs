using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MuseumManager : MonoBehaviour
{
    public RoomModule roomModulePrefab;

    const float roomSize = 5;

    RoomModule[][] roomGrid;

    private readonly List<Transform> activeDisplayTransforms = new(); // active image frame transforms (image frames of the active variant)
    private readonly List<FrameController> activeFrameControllers = new(); // active image frame framecontroller components (the actual art display scripts)

    public IReadOnlyList<Transform> ActiveDisplayTransforms => activeDisplayTransforms;
    public IReadOnlyList<FrameController> ActiveFrameControllers => activeFrameControllers;

    [Tooltip("Should the scan also gather inactive displays?")]
    [SerializeField] bool includeInactiveDisplays = false;         // usually false: only visible frames
    
    [SerializeField] string dbPathOverride = null;
    
    [Tooltip("Automatically populate frames on Start after building the museum?")]
    [SerializeField] bool autoPopulateOnStart = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateMuseum(20);

        RefreshActiveDisplays();

        if (autoPopulateOnStart)
            PopulateDisplays(ActiveFrameControllers.Count);

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

    /// rebuilds the lists(caches) of active display Transforms and FrameController components
    /// rebuilds the lists(caches) of active display Transforms and FrameController components
    /// <param name="includeInactive"> include inactive room variant image frames?(debugging/testing only)</param>
    public void RefreshActiveDisplays(bool includeInactive = false)
    {
        activeDisplayTransforms.Clear();
        activeFrameControllers.Clear();

        if (roomGrid == null || roomGrid.Length == 0) return;

        int size = roomGrid.Length;
        for (int x = 0; x < size; x++)
        {
            var col = roomGrid[x];
            if (col == null) continue;

            for (int y = 0; y < size; y++)
            {
                var room = col[y];
                if (room == null) continue;

                // uses the room module helpers to get transforms and the frame controller scripts
                room.CollectActiveDisplayTransforms(activeDisplayTransforms, includeInactive);
                room.CollectActiveFrameControllers(activeFrameControllers, includeInactive);
            }
        }
    }

    // Requests ScriptableObjects and assigns one-to-one to currently active frames.
    // No overflow/repeat; stops at the shorter of the two lists.
    public void PopulateDisplays(int numDisplays)
    {
        // ensure list caches are current
        RefreshActiveDisplays(includeInactiveDisplays);

        var frames = ActiveFrameControllers.ToList();
        if (frames.Count == 0)
        {
            Debug.LogWarning("[MuseumManager] PopulateDisplays: No active FrameControllers found.");
            return;
        }

        // making sure to request no more than we can display
        int requestCount = Mathf.Clamp(numDisplays, 0, frames.Count);
        if (requestCount == 0)
        {
            Debug.Log("[MuseumManager] PopulateDisplays: requestCount is 0; nothing to do.");
            return;
        }

        List<ArtworkData> items = PsycheDBMiddleware.CreateRandomProjectSObjects(requestCount, dbPathOverride);
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[MuseumManager] PopulateDisplays: Middleware returned no ArtworkData.");
            return;
        }

        int pairCount = Mathf.Min(frames.Count, items.Count);
        for (int i = 0; i < pairCount; i++)
        {
            var fc = frames[i];
            var data = items[i];
            if (!fc || !data) continue;

            
            fc.SetArtwork(data);
        }

        Debug.Log($"[MuseumManager] Assigned {pairCount} artwork ScriptableObjects to {frames.Count} frames.");
    }
}
