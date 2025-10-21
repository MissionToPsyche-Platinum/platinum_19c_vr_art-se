using UnityEngine;

public class MuseumManager : MonoBehaviour
{
    public RoomModule roomModulePrefab;

    class RoomListNode
    {
        public RoomModule rm;

        public RoomListNode nextNorth = null;
        public RoomListNode nextSouth = null;
        public RoomListNode nextWest = null;
        public RoomListNode nextEast = null;

        public RoomListNode(RoomModule rm)
        {
            this.rm = rm;
        }
    }

    RoomListNode roomLinkedList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomLinkedList = new RoomListNode(Instantiate(roomModulePrefab));
        roomLinkedList.rm.SetOpenings(true, true, true, true);
    }

    void GenerateMuseum(int numArtPieces)
    {

    }
}
