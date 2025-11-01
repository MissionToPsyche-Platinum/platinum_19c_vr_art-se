#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PsycheMuseumTest : MonoBehaviour
{
    RoomModule[][] roomGrid;

    [Test]
    public void TwoXTwoTest()
    {
        GameObject empty = new GameObject();
        MuseumManager manager = empty.AddComponent<MuseumManager>();
        manager.InitMuseum(2);
        manager.GenSquare(0, 0, 2, 2);
        manager.AlignAllRooms();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Assert.AreEqual(RoomModule.RoomType.TwoOpenLShapeFlat, manager.roomGrid[0][0].roomType);
            }
        }
    }

    [Test]
    public void IntersectionTest()
    {
        GameObject empty = new GameObject();
        MuseumManager manager = empty.AddComponent<MuseumManager>();
        manager.InitMuseum(3);

        manager.GenSquare(1, 0, 1, 3);
        manager.GenSquare(0, 1, 3, 1);
        manager.AlignAllRooms();

        Assert.AreEqual(RoomModule.RoomType.FourOpen, manager.roomGrid[1][1].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[0][1].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[1][0].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[2][1].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[1][2].roomType);
    }

    [Test]
    public void BigRoomTest()
    {
        GameObject empty = new GameObject();
        MuseumManager manager = empty.AddComponent<MuseumManager>();
        manager.InitMuseum(5);

        manager.GenSquare(0, 0, 5, 5);
        manager.AlignAllRooms();

        Assert.AreEqual(RoomModule.RoomType.FlatOpen, manager.roomGrid[2][2].roomType);
        Assert.AreEqual(RoomModule.RoomType.ThreeOpenFlat, manager.roomGrid[0][2].roomType);
        Assert.AreEqual(RoomModule.RoomType.ThreeOpenFlat, manager.roomGrid[2][0].roomType);
        Assert.AreEqual(RoomModule.RoomType.ThreeOpenFlat, manager.roomGrid[4][2].roomType);
        Assert.AreEqual(RoomModule.RoomType.ThreeOpenFlat, manager.roomGrid[2][4].roomType);
        Assert.AreEqual(RoomModule.RoomType.TwoOpenLShapeFlat, manager.roomGrid[0][0].roomType);
        Assert.AreEqual(RoomModule.RoomType.TwoOpenLShapeFlat, manager.roomGrid[4][0].roomType);
        Assert.AreEqual(RoomModule.RoomType.TwoOpenLShapeFlat, manager.roomGrid[0][4].roomType);
        Assert.AreEqual(RoomModule.RoomType.TwoOpenLShapeFlat, manager.roomGrid[4][4].roomType);
    }

    [Test]
    public void TShapeTest()
    {
        GameObject empty = new GameObject();
        MuseumManager manager = empty.AddComponent<MuseumManager>();
        manager.InitMuseum(3);

        manager.GenSquare(0, 0, 3, 1);
        manager.GenSquare(1, 0, 1, 3);
        manager.AlignAllRooms();

        Assert.AreEqual(RoomModule.RoomType.ThreeOpen, manager.roomGrid[0][1].roomType);
        Assert.AreEqual(RoomModule.RoomType.TwoOpenStraight, manager.roomGrid[1][1].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[1][2].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[0][0].roomType);
        Assert.AreEqual(RoomModule.RoomType.OneOpen, manager.roomGrid[2][0].roomType);
    }
}
#endif