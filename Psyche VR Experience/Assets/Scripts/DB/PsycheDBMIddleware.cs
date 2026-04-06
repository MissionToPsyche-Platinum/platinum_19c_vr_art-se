using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static PsycheDBMiddleware;
using static PsycheDB.DatabaseManager;
using UnityEngine.Networking;
using SQLite4Unity3d;

public static class PsycheDBMiddleware
{
    // resolve the default DB path: ../Psyche VR Experience/Assets/Database/psyche.db
    // relative to Application.dataPath (which is .../Psyche VR Experience/Assets)
    public static string GetDefaultDbPath()
    {
        return DatabasePath;
    }

    class ProjectData
    {
        public string title { get; set; }
        public string description { get; set; }
        public string date { get; set; }
        public string genre_medium { get; set; }
        public string artist_name { get; set; }
        public string artist_major { get; set; }
    }

    class MediaData
    {
        public string media_id { get; set; }
        public string filepath { get; set; }
        public string media_type { get; set; }
    }

    // load a single project (by project_id) and populate an ArtworkData ScriptableObject
    // true on success
    // requires an existing instance of ArtworkDate, method below this one does this.
    //          NOTE:   at runtime, SO instances will be held in memory, so using them to
    //                  populate the prefab should be followed immediately by destruction
    //                  of the SO in question. pipe in the data, dump the container
    public static bool TryLoadArtworkByProjectId(long projectId, ArtworkData target, string dbPathOverride = null)
    {
        if (target == null)
        {
            Debug.LogError("PsycheDbMiddleware.TryLoadArtworkByProjectId: target ArtworkData is null.");
            return false;
        }

        string dbPath = GetDefaultDbPath();
        if (!File.Exists(dbPath))
        {
            Debug.LogError($"PsycheDbMiddleware: DB not found at {dbPath}");
            return false;
        }

        string connStr = dbPath;

        // query project + artist table via join on artist id hash
        string sqlProject = @"
            SELECT p.project_id, p.title, p.description, p.date, p.genre_medium,
                   a.artist_id, a.name AS artist_name, a.major AS artist_major
            FROM projects p
            JOIN artists a ON a.artist_id = p.artist_id
            WHERE p.project_id = @pid;
        ";

        // query media table via project id hash
        string sqlMedia = @"
            SELECT media_id, filepath, media_type
            FROM project_media
            WHERE project_id = @pid
            ORDER BY media_id ASC;
        ";

        try
        {
            using (var conn = new SQLiteConnection(connStr))
            {


                // populate the artist and project table data into temporary variables
                string title = null, description = null, dateIso = null, genre = null;
                string artistName = null, artistMajor = null;

                // get project info from id
                {
                    string command = sqlProject;

                    command = command.Replace("@pid", projectId.ToString());

                    List<ProjectData> projectdata = conn.Query<ProjectData>(command);

                    if(projectdata.Count == 0)
                    {
                        Debug.LogWarning($"PsycheDbMiddleware: No project found with id {projectId}");
                        return false;
                    }

                    ProjectData data = projectdata[0];

                    title = data.title;
                    description = data.description;
                    dateIso = data.date;
                    genre = data.genre_medium;
                    artistName = data.artist_name;
                    artistMajor = data.artist_major;
                }

                // populate the file paths for media into a temporary list 
                var mediaPaths = new List<string>();
                {
                    string command = sqlMedia;

                    command = command.Replace("@pid", projectId.ToString());

                    List<MediaData> mediaData = conn.Query<MediaData>(command);

                    foreach (MediaData d in mediaData)
                    {
                        if (!string.IsNullOrEmpty(d.filepath))
                        {
                            mediaPaths.Add(d.filepath);
                        }
                    }
                }

                // map variables to passed-in ScriptableObject(ArtworkData) fields
                // put in empty strings if the data is MIA for some reason

                target.artworkID = projectId;
                target.artworkName = title ?? string.Empty;
                target.artworkDescription = description ?? string.Empty;
                target.genre = genre ?? string.Empty;

                target.artistName = artistName ?? string.Empty;
                target.artistMajor = artistMajor ?? string.Empty;

                // convert ISO (YYYY-MM-DD) to "MONTH - DAY - YEAR" as on the website (if possible(just fails if unity has an aneurism))
                target.artworkDate = ConvertIsoToMonthDayYear(dateIso);

                // media list instantiates and populates ;)
                target.artworkURLs = target.artworkURLs ?? new List<string>();
                target.artworkURLs.Clear();
                target.artworkURLs.AddRange(mediaPaths);
                target.artworkCount = mediaPaths.Count;

                Debug.Log($"DATA = id -> {target.artworkID}, desc -> {target.artworkDescription}, artwork_count -> {target.artworkCount}");

                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"PsycheDbMiddleware.TryLoadArtworkByProjectId exception: {ex}");
            return false;
        }
    }

    // will return an in-memory SO(ArtworkData) that is safe to pull the data from and delete immediately following. Can also be reused, potentially.
    // I have ideas for the multi-project retrieval here
    public static ArtworkData LoadArtworkIntoNewSO(long projectId, string dbPathOverride = null)
    {
        var so = ScriptableObject.CreateInstance<ArtworkData>();
        if (!(TryLoadArtworkByProjectId(projectId, so, dbPathOverride)))
        {
            UnityEngine.Object.DestroyImmediate(so);
            return null;
        }
        return so;
    }

    // convert "YYYY-MM-DD" into "MONTH - DAY - YEAR" as on website
    // will return originally stored ISO8601 date if it fails
    private static string ConvertIsoToMonthDayYear(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return string.Empty;

        // try multiple ISO-like patterns just in case we missed one somewhere and it slipped by like a slithery little snake
        string[] fmts = { "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d" };
        if (DateTime.TryParseExact(iso, fmts, CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var dt))
        {
            return dt.ToString("MMMM - dd - yyyy", CultureInfo.InvariantCulture); // uncultured swine?
        }

        // fallback option, just does a standard general parse. should work fine, only here if my fancier way sucks
        if (DateTime.TryParse(iso, out dt))
            return dt.ToString("MMMM - dd - yyyy", CultureInfo.InvariantCulture);

        return iso; // return original
    }

    struct ProjectID
    {
        [Column("project_id")]
        public string ProjectId { get; set; }
    }

    // will return all projectIds currently in the project for use in random selection
    // or for direct usage into LoadArtworkIntoNewSO/TryLoadArtworkByProjectId
    public static List<long> GetAllProjectIds(string dbPathOverride = null)
    {
        var dbPath = dbPathOverride ?? GetDefaultDbPath();
        if (!File.Exists(dbPath))
        {
            Debug.LogError($"PsycheDbMiddleware: db not found at {dbPath}");
            return new List<long>();
        }
        var projectIds = new List<long>();

        SQLiteConnection conn = new SQLiteConnection(dbPath);
        string command = "SELECT project_id FROM projects;";

        List<ProjectID> projectIDs = conn.Query<ProjectID>(command);

        foreach (var projectID in projectIDs)
        {
            projectIds.Add(Convert.ToInt64(projectID.ProjectId));
        }

        return projectIds;
    }

    // will return all projectIds with a media filepath in the database. Any without will be excluded.
    // This will see more usage than GetAllProjectIds, considering it returns invalid projects 
    public static List<long> GetProjectIdsWithMediaRows(string dbPathOverride = null)
    {
        var dbPath = dbPathOverride ?? GetDefaultDbPath();
        if (!File.Exists(dbPath))
        {
            Debug.LogError($"PsycheDbMiddleware: DB not found at {dbPath}");
            return new List<long>();
        }

        var ids = new List<long>();
        var conn = new SQLiteConnection(dbPath);

        var command = "SELECT DISTINCT project_id FROM project_media;";

        List<ProjectID> projectIDs = conn.Query<ProjectID>(command);

        foreach (var projectID in projectIDs)
        {
            ids.Add(Convert.ToInt64(projectID.ProjectId));
            Debug.Log($"PROJECT ID: {projectID.ProjectId}");
        }

        return ids;
    }

    // randomized the project ids, selecting a range that is entered as an input, allowing us to adjust to that sweet spot.
    // Once we figure that out we can make a duplicate method that does a specific amount for exhibit mode and for full mode
    public static List<long> GetRandomProjectIds(int count, string dbPathOverride = null)
    {
        var allIds = GetProjectIdsWithMediaRows(dbPathOverride);
        if (allIds.Count == 0) return allIds;
        if (count == 0) return new List<long>();

        var random = new System.Random();
        // using Fisher Yates shuffle to randomize list and then select the range of size count
        for(int i = allIds.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (allIds[i], allIds[j]) = (allIds[j],allIds[i]); 
        }
        // this conditional is after the shuffle so the exhibit is *functionally* never the same
        if (count >= allIds.Count) return allIds;
        return allIds.GetRange(0, count);
    }


    // safe for runtime use, all returned Scriptable objects are stored in memory(currently
    public static List<ArtworkData> LoadRandomArtworkData(int count, string dbPathOverride = null)
    {
        
        var ids = GetRandomProjectIds(count, dbPathOverride);
        var list = new List<ArtworkData>(ids.Count);
        foreach (var id in ids)
        {
            var so = ScriptableObject.CreateInstance<ArtworkData>();
            if (TryLoadArtworkByProjectId(id, so, dbPathOverride))
            {
                // @TODO
                // add a check for having media files associated with project id, in the event of invalid types.
                list.Add(so);
            }
            else
            {
                //in case of fail, don't keep Scriptable object in memory
                UnityEngine.Object.Destroy(so);
            }
        }
        return list;
    }

    /*********************************************************************************************
     *  Implemented the factory pattern within the file. The goal is to keep in self-contained   *
     *********************************************************************************************/

    public interface InterfaceArtworkFactory
    {
        ArtworkData Create(long projectId);
        List<ArtworkData> CreateMany(IEnumerable<long> projectIds);
        List<ArtworkData> CreateRandom(int count);
    }

    // sealing a class allows the compiler to perform optimizations by removing the ability to inherit.
    // factory class to allow ease of use  of the entirety of this classes functionality. I left the methods of
    // PsycheDBMiddleware public just in case and for testing purposes, but this class is the smooth way
    public sealed class ArtworkFactory : InterfaceArtworkFactory
    {
        private readonly string _dbPathOverride;

        public ArtworkFactory(string dbPathOverride = null)
        {
            _dbPathOverride = dbPathOverride;
        }

        //factory method to create a singular instance of a scriptable object with the project id.
        public ArtworkData Create(long projectId)
        {
            var so = ScriptableObject.CreateInstance<ArtworkData>();
            if (!(TryLoadArtworkByProjectId(projectId, so, _dbPathOverride)))
            {
                UnityEngine.Object.Destroy(so);
                return null;
            }
            return so;
        }

        // factory method to return Scriptable objects for the passed in list of project ids
        public List<ArtworkData> CreateMany(IEnumerable<long> ids)
        {
            var list = new List<ArtworkData>();
            foreach (var id in ids)
            {
                var so = Create(id);
                if (so != null) list.Add(so);
            }
            return list;
        }
        // factory method to return the random scriptable objects based on the prior work with random ids and
        // valid projects(has media row in DB)
        public List<ArtworkData> CreateRandom(int count)
        {
            var ids = GetRandomProjectIds(count);
            return CreateMany(ids);
        }
    }

    // this is the method that puts it all together, callable and all that ;)
    // standard usage :
    // var exhibitSOs = PsycheDBMiddleware.CreateRandomProjectSObjects(n);
    // testing or using a different version of the db somewhere else:
    // var testExhibitSOs = PsycheDBMiddleware.CreateRandomProjectSObjects(n, testDB_path);
    public static List<ArtworkData> CreateRandomProjectSObjects(int count, string dbPathOverride = null)
    {
        var factory = new ArtworkFactory(dbPathOverride);
        return factory.CreateRandom(count);
    }

}
