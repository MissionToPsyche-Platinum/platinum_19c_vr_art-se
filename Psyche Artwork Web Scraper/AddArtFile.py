import os
import shutil
from pathlib import Path

from PsycheScraper import connection, upsert_media, make_media_id, detect_media_type


def add_art_file():
    # open connection
    with connection() as conn:
        # open cursor to make queries
        cursor = conn.cursor()
        cursor.execute("select title from projects")

        # get list of art project titles and list them
        title_list = list(cursor)
        for i in range(0, len(title_list)):
            print(f"{i + 1}. {title_list[i][0]}")

        # get user's input on which art project to delete from
        print("Enter the number of the art project to which you would you like to add an art file (q to quit): ", end="")
        title_selection = input()

        # quit
        if title_selection == "q":
            print("Quitting addition.")
        # invalid number
        elif not title_selection.isdigit() or int(title_selection) < 1 or int(title_selection) > len(title_list):
            print("That is not a valid number of an art project.  Quitting addition.")
        # proceed with deleting from selected project
        else:
            title_selection = int(title_selection) - 1
            art_title = title_list[title_selection][0]
            print("You are adding a file to the project " + art_title + ".")
            print("Warning: if there is already a file of the same name in the art project's directory, it will be overwritten.")
            print("Enter the absolute file path of the art file you wish to add.  (To get the absolute path, right click on the desired file in your file explorer and click \"Copy as path\".  Paste the copied text into the command line.) (q to quit): ", end="")

            # get path of file to add
            new_file_path = input()
            new_file_path = str(new_file_path).replace('\\',', ')

            # quit
            if new_file_path == "q":
                print("Quitting deletion.")
            # check whether file path is valid/exists
            elif not os.path.isfile(new_file_path):
                print("That is not a valid file path.  Quitting addition.")
            # check if file type is allowed
            elif Path(new_file_path).suffix.lower() not in [".bmp", ".exr", ".gif", ".hdr", ".iff", ".jpeg", ".jpg", ".pct", ".pic", ".pict", ".png", ".psd", ".tga", ".tif", ".tiff"]:
                print("That is not an allowed file type.  Quitting addition.")
            # add file
            else:
                # get project id of selected art project
                cursor.execute("select project_id from projects where title = ?", (title_list[title_selection][0],))
                project_id = cursor.fetchone()[0]

                # create absolute path and relative path
                dst_abs_path = (Path(__file__).resolve().parent / ".." / "Psyche VR Experience" / "Assets" / "Artwork").resolve() / str(project_id) / new_file_path.split("\\")[-1]
                dst_rel_path = Path("Assets") / "Artwork" / str(project_id) / new_file_path.split("\\")[-1]
                # copy file to new destination
                shutil.copy(new_file_path, dst_abs_path)

                # add file path to sql database
                artist_name = cursor.execute("select name from artists, projects where projects.project_id = ? and projects.artist_id = artists.artist_id", (project_id,)).fetchone()[0]
                new_media_id = make_media_id(artist_name, art_title, new_file_path)
                upsert_media(new_media_id, str(dst_rel_path), detect_media_type(str(dst_rel_path)), project_id)

                print("Added file successfully!")