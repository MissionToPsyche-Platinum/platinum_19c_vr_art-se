#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PsycheDBMiddlewareEditorRuntimeTests
{
    private readonly List<ArtworkData> _toCleanup = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var so in _toCleanup)
        {
            if (so != null) Object.DestroyImmediate(so);
        }
        _toCleanup.Clear();
    }

    // helper, returns a valid project_id or marks test inconclusive if DB is empty/unavailable.

    private static long GetAnyProjectIdOrInconclusive()
    {
        var ids = PsycheDBMiddleware.GetAllProjectIds();
        if (ids == null || ids.Count == 0)
        {
            Assert.Inconclusive("No projects found in DB. Seed the database before running tests.");
        }
        return ids[0];
    }

    // this one checks that ids are being returned
    [Test]
    public void GetAllProjectIds_ReturnsIds()
    {
        var ids = PsycheDBMiddleware.GetAllProjectIds();
        Assert.IsNotNull(ids, "GetAllProjectIds returned null.");

        if (ids.Count == 0)
        {
            Assert.Inconclusive("No project IDs returned. Ensure the DB has at least one project row.");
        }
        else
        {
            Assert.GreaterOrEqual(ids.Count, 1, "Expected at least one project id.");
        }
    }


    // this one checks whether or not the scriptable objects are being populated properly
    [Test]
    public void TryLoadArtworkByProjectId_PopulatesScriptableObject()
    {
        long projectId = GetAnyProjectIdOrInconclusive();
        var so = ScriptableObject.CreateInstance<ArtworkData>();
        _toCleanup.Add(so);

        bool ok = PsycheDBMiddleware.TryLoadArtworkByProjectId(projectId, so);

        // asserting
        Assert.IsTrue(ok, $"TryLoadArtworkByProjectId({projectId}) returned false.");
        Assert.IsNotNull(so, "ScriptableObject instance was null.");
        Assert.AreEqual(projectId, so.artworkID, "artworkID should match requested projectId.");
        Assert.IsNotNull(so.artworkName, "artworkName should not be null.");
        Assert.IsNotNull(so.artistName, "artistName should not be null.");
        Assert.IsNotNull(so.artworkDescription, "artworkDescription should not be null.");
        Assert.IsNotNull(so.genre, "genre should not be null.");
        Assert.IsNotNull(so.artistMajor, "artistMajor should not be null.");
        Assert.IsNotNull(so.artworkDate, "artworkDate should not be null.");
        Assert.IsNotNull(so.artworkURLs, "artworkURLs should be initialized.");
        Assert.GreaterOrEqual(so.artworkCount, 0, "artworkCount should be >= 0.");
        Assert.AreEqual(so.artworkURLs.Count, so.artworkCount, "artworkCount should equal artworkURLs.Count.");
    }

    // ensures a list of scriptable object is returned by various methods
    [Test]
    public void LoadRandomArtworkData_ReturnsListOfScriptableObjects()
    {
        const int howMany = 3;

        var list = PsycheDBMiddleware.LoadRandomArtworkData(
            count: howMany,
            dbPathOverride: null
        );

        // Track for cleanup
        if (list != null) _toCleanup.AddRange(list);

        // assertions
        Assert.IsNotNull(list, "LoadRandomArtworkData returned null.");
        if (list.Count == 0)
        {
            Assert.Inconclusive("No ArtworkData returned. Ensure the DB has some projects.");
        }

        foreach (var so in list)
        {
            Assert.IsNotNull(so, "ArtworkData item was null.");
            Assert.Greater(so.artworkID, 0, "artworkID should be > 0.");
            Assert.IsNotNull(so.artworkName, "artworkName should not be null.");
            Assert.IsNotNull(so.artistName, "artistName should not be null.");
            Assert.IsNotNull(so.artworkDescription, "artworkDescription should not be null.");
            Assert.IsNotNull(so.genre, "genre should not be null.");
            Assert.IsNotNull(so.artistMajor, "artistMajor should not be null.");
            Assert.IsNotNull(so.artworkDate, "artworkDate should not be null.");
            Assert.IsNotNull(so.artworkURLs, "artworkURLs should be initialized.");
            Assert.AreEqual(so.artworkURLs.Count, so.artworkCount, "artworkCount should equal artworkURLs.Count.");
        }
    }

    // this one tests the factory method callable
    [Test]
    public void CreateRandomProjectSObjects_FactoryCreatesValidScriptableObjects()
    {
        const int howMany = 3;

        var list = PsycheDBMiddleware.CreateRandomProjectSObjects(howMany);

        if (list != null) _toCleanup.AddRange(list);

        // assertions
        Assert.IsNotNull(list, "CreateRandomProjectSObjects returned null.");

        if (list.Count == 0)
            Assert.Inconclusive("Factory did not produce any ArtworkData. Ensure DB has valid projects with media.");

        foreach (var so in list)
        {
            Assert.IsNotNull(so, "ArtworkData instance was null.");
            Assert.Greater(so.artworkID, 0, "artworkID should be > 0.");
            Assert.IsNotNull(so.artworkName, "artworkName should not be null.");
            Assert.IsNotNull(so.artistName, "artistName should not be null.");
            Assert.IsNotNull(so.artworkDescription, "artworkDescription should not be null.");
            Assert.IsNotNull(so.genre, "genre should not be null.");
            Assert.IsNotNull(so.artistMajor, "artistMajor should not be null.");
            Assert.IsNotNull(so.artworkDate, "artworkDate should not be null.");
            Assert.IsNotNull(so.artworkURLs, "artworkURLs list should be initialized.");
            Assert.AreEqual(so.artworkURLs.Count, so.artworkCount,
                "artworkCount should equal artworkURLs.Count.");
        }
    }
}
#endif