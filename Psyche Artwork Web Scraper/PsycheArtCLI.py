import PsycheScraper
from AddArtProject import *
from ModifyArtProject import *

if __name__ == '__main__':
    print("""Welcome to the Psyche VR Museum CLI Interface!  What would you like to do?
1: Run the web scraper
2: Add an art file to an existing art project
3: Remove an art file from an existing art project
4: Add a completely new art project
5: Modify a non-file attribute of an existing art project or artist
6: Delete an existing art project
q: Quit""")

    while True:

        in_str = input()
        if in_str == '1':
            PsycheScraper.init_db()
            PsycheScraper.scrapePsyche()
            PsycheScraper.repair_media()
        elif in_str == '2':
            add_art_file()
            pass
        elif in_str == '3':
            delete_art_file()
            pass
        elif in_str == '4':
            artInfo = getArtProjectInfo()
            addArtProject(artInfo)
            pass
        elif in_str == '5':
            modify_art_project()
            pass
        elif in_str == '6':
            delete_art_project()
            pass
        elif in_str == 'q' or in_str == 'Q':
            print("Goodbye!")
            exit(0)
        else:
            print("That is not one of the options.  Please try again.")

        print("""What would you like to do?
1: Run the web scraper
2: Add an art file to an existing art project
3: Remove an art file from an existing art project
4: Add a completely new art project
5: Modify a non-file attribute of an existing art project or artist
6: Delete an existing art project
q: Quit""")