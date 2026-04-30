import os
import shutil
from pathlib import Path
from datetime import datetime

from src.db_modification.DatabaseUpdater import connection, UpsertMedia, CreateMediaId, GetMediaType, UpsertArtist, UpsertProject
from src.db_modification.GetInputFiles import GetValidFilesInInputDirectory

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
                        print(f"Added to list: {all_file_names[index].name}")
                    else:
                        print("Invalid number, please try again.")
                except ValueError:
                    print("Invalid input, please enter a number or 'q'.")

            # add file
            for file_path in chosen_files:
                file_name = Path(file_path).name
                # get project id of selected art project
                cursor.execute("select project_id from projects where title = ?", (title_list[title_selection][0],))
                project_id = cursor.fetchone()[0]

                # create absolute path and relative path
                dst_abs_path = Path(os.getenv('OUTPUT_PATH')) / "Artwork" / str(project_id) / file_name
                dst_rel_path = Path("Artwork") / str(project_id) / file_name
                # copy file to new destination
                shutil.copy(file_path, dst_abs_path)

                # add file path to sql database
                artist_name = cursor.execute("select name from artists, projects where projects.project_id = ? and projects.artist_id = artists.artist_id", (project_id,)).fetchone()[0]
                new_media_id = CreateMediaId(artist_name, art_title, file_path)
                UpsertMedia(new_media_id, str(dst_rel_path), GetMediaType(str(dst_rel_path)), project_id)

                print(f"Added {file_name} successfully!")

def delete_art_file():
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
        print("Enter the number of the art project to which you would you like to delete an art file (q to quit): ", end="")
        title_selection = input()

        # quit
        if title_selection == "q":
            print("Quitting deletion.")
        # invalid number
        elif not title_selection.isdigit() or int(title_selection) < 1 or int(title_selection) > len(title_list):
            print("That is not a valid number of an art project.  Quitting deletion.")
        # proceed with deleting from selected project
        else:
            # find selected project's id
            title_selection = int(title_selection) - 1
            cursor.execute("select project_id from projects where title = ?", (title_list[title_selection][0],))
            project_id = cursor.fetchone()[0]

            # find all project media belonging to selected project
            cursor.execute("select filepath from project_media where project_id = ?", (project_id,))
            media_list = list(cursor)
            print("\nYou are deleting a file from the project: " + title_list[title_selection][0] + ".")
            for i in range(0, len(media_list)):
                print(f"{i + 1}. {media_list[i][0]}")

            # get user's input on which file to delete
            print("Enter the number of the art file you would like to delete (q to quit): ", end="")
            file_selection = input()

            # quit
            if file_selection == "q":
                print("Quitting deletion.")
            # invalid number
            elif not file_selection.isdigit():
                print("That is not a valid number of an art file.  Quitting deletion.")
            # delete filepath from database and corresponding file
            else:
                file_selection = int(file_selection) - 1

                abs_art_path = Path(os.getenv('OUTPUT_PATH')) / media_list[file_selection][0]
                rel_art_path = media_list[file_selection][0]

                cursor.execute("delete from project_media where filepath = ?", (str(rel_art_path),))
                os.remove(abs_art_path)
                print("Deleted file successfully!")

        # close cursor object
        cursor.close()

def modify_art_project():
    artist = False
    # are we modifying an artist or a project
    while True:
        in_str = input("""Would you like to modify an artist or an art project?
        1: Artist (Modify Name or Major)
        2: Art Project (Modify Title, Description, Date, or Genre\n""")

        if in_str == "1":
            artist = True
            break
        elif in_str == "2":
            break
        else:
            print("That is not a valid option. Please input 1 or 2.")

    table = "projects"
    attr = "title"
    id_name = "project_id"
    if artist:
        table = "artists"
        attr = "name"
        id_name = "artist_id"

    with connection() as conn:
        # open cursor to make queries
        cursor = conn.cursor()
        cursor.execute(f"select {attr} from {table}")

        # get list of art project artists/titles
        items = list(cursor)
        for i in range(0, len(items)):
            print(f"{i + 1}. {items[i][0]}")

        # get user's input on which art project to delete from
        selection = ""
        while True:
            selection = input(f"Enter the number of the {table[:-1]} that you would you like to modify: ")
            if not selection.isdigit() or int(selection) < 1 or int(selection) > len(items):
                print("That is not a valid number, please try again.")
            else:
                break
        selection = int(selection) - 1

        # get id of selected project/artist
        cursor.execute(f"select {id_name} from {table} where {attr} = ?", (items[selection][0],))
        db_id = cursor.fetchone()[0]

        if artist:
            while True:
                cursor.execute("select name, major from artists where artist_id = ?", (db_id,))
                name, major = cursor.fetchone()

                in_str = input(f"""Would you like to modify the artist's name or major?
                    1: Name (Currently: {name})
                    2: Major (Currently: {major})\n""")

                if in_str == "1":
                    name = input("Enter the new name of the artist: ")
                    break
                elif in_str == "2":
                    major = input("Enter the new major of the artist: ")
                    break
                else:
                    print("That is not a valid option. Please input 1 or 2.")

            UpsertArtist(db_id, name, major)

        else:
            while True:
                cursor.execute("select title, description, date, genre_medium, artist_id from projects where project_id = ?", (db_id,))
                title, description, date, genre, artist_id = cursor.fetchone()

                in_str = input(f"""Would you like to modify the artist's name or major?
                    1: Title (Currently: {title})
                    2: Description (Currently: {description})
                    3: Date (Currently: {date})
                    4: Genre (Currently: {genre})\n""")

                if in_str == "1":
                    title = input("Enter the new title of the project: ")
                    break
                elif in_str == "2":
                    description = input("Enter the new description of the project: ")
                    break
                elif in_str == "3":
                    date = ""
                    while True:
                        date = input("Enter Project Date (Format as YYYY-MM-DD): ")
                        try:
                            datetime.strptime(date, "%Y-%m-%d")
                            break  # valid format
                        except ValueError:
                            print("Invalid format. Please enter date as YYYY-MM-DD.")
                    break
                elif in_str == "4":
                    genre = input("Enter the new genre of the project: ")
                    break
                else:
                    print("That is not a valid option. Please input 1, 2, 3, or 4.")

            UpsertProject(db_id, title, description, date, genre, artist_id)

    print("Successfully modified!")

def delete_art_project():
    with connection() as conn:
        # open cursor to make queries
        cursor = conn.cursor()
        cursor.execute("select title from projects")

        # get list of art project artists/titles
        items = list(cursor)
        for i in range(0, len(items)):
            print(f"{i + 1}. {items[i][0]}")

        # get user's input on which art project to delete from
        selection = ""
        while True:
            selection = input(f"Enter the number of the project that you would you like to modify: ")
            if not selection.isdigit() or int(selection) < 1 or int(selection) > len(items):
                print("That is not a valid number, please try again.")
            else:
                break
        selection = int(selection) - 1

        # get id of selected project/artist
        cursor.execute(f"select project_id from projects where title = ?", (items[selection][0],))
        db_id = cursor.fetchone()[0]

        confirm = input("Are you sure you want to delete this project? (Type \"Yes, I am sure\" to confirm): ")
        if confirm != "Yes, I am sure":
            print("Cancelling deletion.")
            return

        cursor.execute("delete from projects where project_id = ?", (db_id,))

    print("Successfully deleted!")
