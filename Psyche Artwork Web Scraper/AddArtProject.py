import shutil
from pathlib import Path

from src.db_modification.DatabaseUpdater import UpsertArtist, UpsertProject, UpsertMedia, CreateArtistId, CreateProjectId, GetMediaType, CreateMediaId
from src.db_modification.GetInputFiles import GetValidFilesInInputDirectory

from datetime import datetime
import os

def addArtProject(artInfo):
    artist_name = artInfo["artistName"]
    art_title = artInfo["artTitle"]
    date_iso = artInfo["date"]
    artist_major = artInfo["artistMajor"]
    genre_medium = artInfo["genre"]
    description = artInfo["description"]
    temp_files = artInfo["file_paths"]

    # artist and project hash and upsert
    artist_id = make_artist_id(artist_name)
    project_id = make_project_id(artist_name,art_title)
    upsert_artist(artist_id, artist_name, artist_major)
    upsert_project(project_id, art_title, description, date_iso,genre_medium, artist_id)

    # create project directory
    dst_dir_abs_path = ARTWORK_DIR / str(project_id)
    os.makedirs(dst_dir_abs_path, exist_ok=True)

    # media hash and upsert
    for filepath in temp_files:
        # create absolute path and relative path
        file_name = Path(filepath).name
        dst_abs_path = dst_dir_abs_path / file_name
        dst_rel_path = Path("Artwork") / str(project_id) / file_name
        # copy file to new destination
        shutil.copy(filepath, dst_abs_path)

        # add media to database
        media_type = detect_media_type(filepath)
        media_id = make_media_id(artist_name, art_title, filepath)
        upsert_media(media_id, str(dst_rel_path), media_type, project_id)

    print("Art project added successfully!\n")

def getArtProjectInfo():
    results = {}

    results["artistName"] = input("Enter Artist's Name: ")
    results["artTitle"] = input("Enter Project Title: ")
    results["artistMajor"] = input("Enter Artist's Major: ")
    results["genre"] = input("Enter Art Project's Genre: ")
    results["description"] = input("Enter Project Description: ")

    while True:
        date = input("Enter Project Date (Format as YYYY-MM-DD): ")
        try:
            datetime.strptime(date, "%Y-%m-%d")
            results["date"] = date
            break  # valid format
        except ValueError:
            print("Invalid format. Please enter date as YYYY-MM-DD.")

    all_file_names = GetValidFilesInInputDirectory()
    chosen_files = []
    while True:
        print("\nAvailable files:")
        for i, file in enumerate(all_file_names):
            print(f"  {i + 1}. {file.name}")
        
        print("\nIf you are not seeing the file you expect, make sure it is one of the allowed file types in the README")
        user_input = input("\nEnter file number to select (or 'q' to quit): ").strip()
        
        if user_input.lower() == 'q':
            break
        
        try:
            index = int(user_input) - 1
            if 0 <= index < len(all_file_names):
                chosen_files.append(str(all_file_names[index]))
                print(f"Added: {all_file_names[index].name}")
            else:
                print("Invalid number, please try again.")
        except ValueError:
            print("Invalid input, please enter a number or 'q'.")

    results["file_paths"] = chosen_files
    return results