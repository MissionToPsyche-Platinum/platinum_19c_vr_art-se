import hashlib
import os
import sqlite3
from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
from pathlib import Path
from typing import Optional

import pandas as pd

import src.media_handling.PsycheMedia as PsycheMedia

def UpdateDatabase():
    csv_file = GetCSVFile()
    if csv_file is None:
        print("There should be exactly one CSV file in the input directory.  Please fix this and try again.")
        return
    
    InitializeDatabase()
    columns = ['Artist Name', 'Artist Major', 'Project Title', 'Project Date', 'Project Genre', 'Project Description', 'Project Link']
    dataframe = pd.read_csv(csv_file, usecols=columns)
    
    BATCH_SIZE = 16
    for start in range(0, len(dataframe), BATCH_SIZE):
        print(f"Processing rows {start} to {min(start + BATCH_SIZE, len(dataframe))}...")
        batch = dataframe.iloc[start:start + BATCH_SIZE]

        rows = [batch.iloc[i] for i in range(len(batch))]
        batchFilepaths = []
        with ThreadPoolExecutor() as executor:
            batchFilepaths = list(executor.map(PsycheMedia.get_art_filepath, rows))

        for i in range(len(batch)):
            row = batch.iloc[i]
            projectFilepaths = batchFilepaths[i]
            UpsertRowToDatabase(row, projectFilepaths)

def GetCSVFile():
    INPUT_PATH = Path(os.getenv('INPUT_PATH'))
    csv_files = list(INPUT_PATH.glob("*.csv"))
    if (len(csv_files) != 1):
        return None
    return csv_files[0]

def UpsertRowToDatabase(row, projectFilepaths):
    artistName = row['Artist Name']
    artistMajor = row['Artist Major'].title()
    artistId = CreateArtistId(artistName)
    UpsertArtist(artistId, artistName, artistMajor)

    projectTitle = row['Project Title']
    projectId = CreateProjectId(artistName, projectTitle)
    projectDescription = row['Project Description']
    projectDate = row['Project Date']
    projectGenre = row['Project Genre'].title()
    UpsertProject(projectId, projectTitle, projectDescription, projectDate, projectGenre, artistId)

    # media hash and upsert
    for filepath in projectFilepaths:
        mediaType = GetMediaType(filepath)
        mediaId = CreateMediaId(artistName, projectTitle, filepath)
        UpsertMedia(mediaId, filepath, mediaType, projectId)


def UpsertArtist(artist_id: int, name: str, major: Optional[str] = None):
    with connection() as conn:
        conn.execute("""
            INSERT INTO artists (artist_id, name, major)
            VALUES (?, ?, ?)
            ON CONFLICT(artist_id) DO UPDATE SET
                name = excluded.name,
                major = excluded.major;
        """, (artist_id, name, major))

def CreateArtistId(artist_name: str) -> int:
    return _hash_to_int63(f"artist::{_norm(artist_name)}")

def UpsertProject(project_id: int, title: str, description: Optional[str],
                   date: Optional[str], genre_medium: Optional[str], artist_id: int):
    with connection() as conn:
        conn.execute("""
            INSERT INTO projects (project_id, title, description, date, genre_medium, artist_id)
            VALUES (?, ?, ?, ?, ?, ?)
            ON CONFLICT(project_id) DO UPDATE SET
                title = excluded.title,
                description = excluded.description,
                date = excluded.date,
                genre_medium = excluded.genre_medium,
                artist_id = excluded.artist_id;
        """, (project_id, title, description, date, genre_medium, artist_id))

def CreateProjectId(artist_name: str, artwork_title: str) -> int:
    return _hash_to_int63(f"project::{_norm(artist_name)}::{_norm(artwork_title)}")

def GetMediaType(filepath: str) -> str:
    ext = Path(filepath).suffix.lower()
    if ext in {".mp4", ".mov", ".m4v", ".avi", ".webm"}:
        return "video"
    if ext in {".mp3", ".wav", ".flac", ".ogg", ".m4a"}:
        return "audio"
    return "image"

def CreateMediaId(artist_name: str, artwork_title: str, filepath: str) -> int:
    basename = Path(filepath).name
    return _hash_to_int63(f"media::{_norm(artist_name)}::{_norm(artwork_title)}::{_norm(basename)}")

def UpsertMedia(media_id: int, filepath: str, media_type: str, project_id: int):
    with connection() as conn:
        conn.execute("""
            INSERT INTO project_media (media_id, filepath, media_type, project_id)
            VALUES (?, ?, ?, ?)
            ON CONFLICT(media_id) DO UPDATE SET
                filepath = excluded.filepath,
                media_type = excluded.media_type,
                project_id = excluded.project_id;
        """, (media_id, filepath, media_type, project_id))

@contextmanager
def connection():
    # SQLite connection with FK enforcement, synchronous normal mode
    db_path = GetDatabasePath()
    conn = sqlite3.connect(db_path)
    try:
        conn.execute("PRAGMA foreign_keys = ON;")
        yield conn
        conn.commit()
    finally:
        conn.close()

def _norm(s: str) -> str:
    return s.strip().lower()

def _hash_to_int63(seed: str) -> int:
    h = hashlib.sha256(seed.encode("utf-8")).hexdigest()
    return int(h[:16], 16) & ((1 << 63) - 1)

def GetDatabasePath():
    dbDirectory = Path(os.getenv('OUTPUT_PATH')) / "Database"
    dbDirectory.mkdir(parents=True, exist_ok=True)
    return dbDirectory / "artwork.db"

def InitializeDatabase():
    # create tables and indexes if they don't exist.
    with connection() as conn:
        cursor = conn.cursor()

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS artists (
                artist_id INTEGER PRIMARY KEY,
                name      TEXT NOT NULL,
                major     TEXT
            );
        """)

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS projects (
                project_id   INTEGER PRIMARY KEY,
                title        TEXT NOT NULL,
                description  TEXT,
                date         TEXT,           -- ISO (YYYY-MM-DD) recommended
                genre_medium TEXT,
                artist_id    INTEGER NOT NULL,
                FOREIGN KEY (artist_id)
                    REFERENCES artists(artist_id)
                    ON UPDATE CASCADE
                    ON DELETE CASCADE
            );
        """)

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS project_media (
                media_id   INTEGER PRIMARY KEY,
                filepath   TEXT NOT NULL,
                media_type TEXT NOT NULL CHECK (media_type IN ('image','video','audio')),
                project_id INTEGER NOT NULL,
                FOREIGN KEY (project_id)
                    REFERENCES projects(project_id)
                    ON UPDATE CASCADE
                    ON DELETE CASCADE
            );
        """)

        cursor.execute("CREATE INDEX IF NOT EXISTS idx_projects_artist ON projects(artist_id);")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_media_project  ON project_media(project_id);")