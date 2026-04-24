#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Data;
using System.IO;
using SQLite4Unity3d;
using UnityEngine.Networking;


namespace PsycheDB
{
    public static class DatabaseManager
    {
        // intended database location: Assets/Database/psyche_data.db
        public static readonly string DatabaseFolder = Path.Combine(Application.persistentDataPath, "Database");
        public static readonly string DatabasePath = Path.Combine(DatabaseFolder, "artwork.db");

        // connection string
        public static string ConnString => DatabasePath;

        /// <summary>
        /// Public entrypoint: ensure folder, create DB if missing, apply schema.
        /// Safe to call multiple times.
        /// </summary>
        public async static void Initialize()
        {
            //EnsureFolder();
            //var firstTime = !File.Exists(DatabasePath);
            //string dbPath = Application.streamingAssetsPath + "/Database/" + "artwork.db";

            //UnityWebRequest request = UnityWebRequest.Get(dbPath);
            //await request.SendWebRequest();

            //if (request.result == UnityWebRequest.Result.Success)
            //{
            //    File.WriteAllBytes(DatabasePath, request.downloadHandler.data);
            //    Debug.Log("COPIED DATABASE PROPERLY TO " + DatabasePath);
            //}
            //else
            //{
            //    Debug.LogError("FAILED TO LOAD DATABASE FROM STREAMINGASSETS");
            //}

            
        }

        /*  API  */

        // Insert helpers (use these when finishing ImportFromJson).
        // Upsert(Insert/Update) by primary key.
        public static void UpsertArtist(int artistId, string name, string major)
        {
            using (var connection = Open())
            {
                connection.BeginTransaction();

                string command = $@"
                        INSERT INTO artists (artist_id, name, major)
                        VALUES ({artistId}, {name}, {major})
                        ON CONFLICT(artist_id) DO UPDATE SET
                            name = excluded.name,
                            major = excluded.major;
                    ";

                connection.Execute(command);

                connection.Commit();
            }
        }

        public static void UpsertProject(
            int projectId, string title, string description, string dateIso8601,
            string genreMedium, int artistId)
        {
            using (var connection = Open()) ;
            //using (var transaction = connection.BeginTransaction())
            //using (var command = connection.CreateCommand())
            //{
            //    command.CommandText = @"
            //        INSERT INTO projects (project_id, title, description, date, genre_medium, artist_id)
            //        VALUES (@id, @title, @desc, @date, @genre, @artist)
            //        ON CONFLICT(project_id) DO UPDATE SET
            //            title = excluded.title,
            //            description = excluded.description,
            //            date = excluded.date,
            //            genre_medium = excluded.genre_medium,
            //            artist_id = excluded.artist_id;
            //    ";
            //    command.Parameters.Add(new SqliteParameter("@id", projectId));
            //    command.Parameters.Add(new SqliteParameter("@title", title ?? string.Empty));
            //    command.Parameters.Add(new SqliteParameter("@desc", description ?? string.Empty));
            //    command.Parameters.Add(new SqliteParameter("@date", dateIso8601 ?? string.Empty));// store dates as ISO8601 TEXT ("YYYY-MM-DD")
            //    command.Parameters.Add(new SqliteParameter("@genre", genreMedium ?? string.Empty));
            //    command.Parameters.Add(new SqliteParameter("@artist", artistId));
            //    command.ExecuteNonQuery();
            //    transaction.Commit();
            //}
        }

        public static void UpsertProjectMedia(int mediaId, string filepath, string mediaType, int projectId)
        {
            // normalize mediaType
            string mType = (mediaType ?? "").ToLowerInvariant();
            if (mType != "image" && mType != "video" && mType != "audio" && mType != "multi")
            {
                throw new ArgumentException("media_type must be one of: image, video, audio");
            }

            using (var connection = Open()) ;
            //using (var transasction = connection.BeginTransaction())
            //using (var command = connection.CreateCommand())
            //{
            //    command.CommandText = @"
            //        INSERT INTO project_media (media_id, filepath, media_type, project_id)
            //        VALUES (@id, @path, @type, @proj)
            //        ON CONFLICT(media_id) DO UPDATE SET
            //            filepath = excluded.filepath,
            //            media_type = excluded.media_type,
            //            project_id = excluded.project_id;
            //    ";
            //    command.Parameters.Add(new SqliteParameter("@id", mediaId));
            //    command.Parameters.Add(new SqliteParameter("@path", filepath ?? string.Empty));
            //    command.Parameters.Add(new SqliteParameter("@type", mType));
            //    command.Parameters.Add(new SqliteParameter("@proj", projectId));
            //    command.ExecuteNonQuery();
            //    transasction.Commit();
            //}
        }

        /* internals */

        // makes sure the Assets/Database folder is there
        private static void EnsureFolder()
        {
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
                Debug.Log($"[DB] Created folder: {DatabaseFolder}");
            }
        }

        // Establishes connection
        private static SQLiteConnection Open()
        {
            var connection = new SQLiteConnection(ConnString);
            return connection;
        }

        // sets ups the schemas
        private static void CreateSchema(SQLiteConnection connection)
        {
            // artists
            string command = @"
                    CREATE TABLE IF NOT EXISTS artists (
                        artist_id      INTEGER PRIMARY KEY,
                        name           TEXT NOT NULL,
                        major          TEXT
                    );
                ";
            connection.Execute(command);

            // projects / artworks
            command = @"
                    CREATE TABLE IF NOT EXISTS projects (
                        project_id     INTEGER PRIMARY KEY,
                        title          TEXT NOT NULL,
                        description    TEXT,
                        date           TEXT, -- store ISO8601 strings, e.g. '2025-10-07'
                        genre_medium   TEXT,
                        artist_id      INTEGER NOT NULL,
                        FOREIGN KEY (artist_id)
                          REFERENCES artists(artist_id)
                          ON UPDATE CASCADE
                          ON DELETE CASCADE
                    );
                ";
            connection.Execute(command);

            // project media (enum via CHECK)
            command = @"
                    CREATE TABLE IF NOT EXISTS project_media (
                        media_id       INTEGER PRIMARY KEY,
                        filepath       TEXT NOT NULL,
                        media_type     TEXT NOT NULL CHECK (media_type IN ('image','video','audio')),
                        project_id     INTEGER NOT NULL,
                        FOREIGN KEY (project_id)
                          REFERENCES projects(project_id)
                          ON UPDATE CASCADE
                          ON DELETE CASCADE
                    );
                ";
            connection.Execute(command);

            // helpful indexes for debug and perusing
            command = @"
                    CREATE INDEX IF NOT EXISTS idx_projects_artist ON projects(artist_id);
                    CREATE INDEX IF NOT EXISTS idx_media_project   ON project_media(project_id);
                ";
            connection.Execute(command);
        }


        // Dangerous but handy while iterating.
        // I repeat, this is dangerous and honestly should not be in here. 
        // I put it in in case we want to wipe it or mess it up somehow.
        // Good for now, will remove once the database is set up and beautiful

        public static void DropAllDataAndSchema()
        {
            if (!File.Exists(DatabasePath)) return;
            using (var connection = Open())
            {

            }
            //using (var command = connection.CreateCommand())
            //{
            //    command.CommandText = @"
            //        PRAGMA foreign_keys = OFF;
            //        DROP TABLE IF EXISTS project_media;
            //        DROP TABLE IF EXISTS projects;
            //        DROP TABLE IF EXISTS artists;
            //        PRAGMA foreign_keys = ON;
            //    ";
            //    command.ExecuteNonQuery();
            //}
            Debug.Log("[DB] Dropped all tables.");
        }
    }


    // attach this to an empty GameObject if you want it to auto-init in play mode.
    public class DatabaseBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            DatabaseManager.Initialize();
        }
    }



    // @TODO
    // probably gonna throw in some menu popup for db interaction so we don't have to make buttons in game or anything
    // will make maintenance and debug easier too if we run into issues later.

}