import shutil
from pathlib import Path

from PsycheScraper import upsert_artist, upsert_project, upsert_media, make_artist_id, make_project_id, detect_media_type, make_media_id, ARTWORK_DIR
from datetime import datetime
import os

def addArtProject(artInfo):
    artist_name = artInfo["artistName"]
    art_title = artInfo["artTitle"]
    date_iso = artInfo["date"]
    artist_major = artInfo["artistMajor"]
    genre_medium = artInfo["genre"]
    description = artInfo["description"]
    temp_files = artInfo["file_paths"]  # downloaded to ./psyche_media

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
        file_name = filepath.split("\\")[-1]
        dst_abs_path = dst_dir_abs_path / file_name
        dst_rel_path = Path(str(project_id)) / file_name
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

    print("Enter the absolute file path of each art file you wish to add, pressing enter in between each file.  When you have entered the last file, enter \"f\".  (To get the absolute path, right click on the desired file in your file explorer and click \"Copy as path\".  Paste the copied text into the command line.)")
    file_paths = []
    while True:
        file_path = input()
        file_path = file_path.replace("\"", "")

        if file_path.lower() == "f":
            print("Ending file collection.")
            break
        if os.path.isfile(file_path):
            file_paths.append(file_path)
        else:
            print("That is not a valid file path. Please enter a new one.")

    results["file_paths"] = file_paths
    return results