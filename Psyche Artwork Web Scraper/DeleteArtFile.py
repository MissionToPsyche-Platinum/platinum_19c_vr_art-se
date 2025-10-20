import os
from pathlib import Path

from PsycheScraper import connection


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
            print("\nYou are deleting a file from the project " + title_list[title_selection][0] + ".")
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

                abs_art_path = (Path(__file__).resolve().parent / ".." / "Psyche VR Experience" / media_list[file_selection][0]).resolve()

                cursor.execute("delete from project_media where filepath = ?", (str(abs_art_path),))
                os.remove(abs_art_path)

        # close cursor object
        cursor.close()