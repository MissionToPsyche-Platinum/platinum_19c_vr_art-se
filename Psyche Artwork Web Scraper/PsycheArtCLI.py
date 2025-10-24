import PsycheScraper
from AddArtFile import add_art_file
from DeleteArtFile import delete_art_file
from PsycheTestRun import testRunYTDLP

if __name__ == '__main__':
    print("""Welcome to the Psyche VR Museum CLI Interface!  What would you like to do?
1: Run the web scraper
2: Add an art file to an existing art project
3: Remove an art file from an existing art project
q: Quit""")

    while True:

        in_str = input()
        if in_str == '1':
            PsycheScraper.init_db()
            PsycheScraper.scrapePsyche()
        elif in_str == '2':
            add_art_file()
            pass
        elif in_str == '3':
            delete_art_file()
            pass
        elif in_str =='4':
            #This is a hidden tester for debugging purposes, leave in for now
            testRunYTDLP()
            pass
        elif in_str == 'q':
            print("Goodbye!")
            exit(0)
        else:
            print("That is not one of the options.  Please try again.")

        print("""What would you like to do?
1: Run the web scraper
2: Add an art file to an existing art project
3: Remove an art file from an existing art project
q: Quit""")