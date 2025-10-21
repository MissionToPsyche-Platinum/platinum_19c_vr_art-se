import os.path
from datetime import datetime
import re
import fitz # PyMuPDF
from PIL import Image
from io import BytesIO
import bs4
import requests
from bs4 import BeautifulSoup
from pytubefix import YouTube
from vimeo_downloader import Vimeo
from concurrent.futures import ThreadPoolExecutor

import sqlite3
import hashlib
import ffmpeg
import yt_dlp
from pathlib import Path
from contextlib import contextmanager
from typing import Optional

# art folder director
HERE = Path(__file__).resolve().parent
ARTWORK_DIR = (HERE / ".." / "Psyche VR Experience" / "Assets" / "Artwork").resolve()
ARTWORK_DIR.mkdir(parents=True, exist_ok=True)
ALLOWED_FILE_EXTENSIONS = [".bmp", ".exr", ".gif", ".hdr", ".iff", ".jpeg", ".jpg", ".pct", ".pic", ".pict", ".png", ".psd", ".tga", ".tif", ".tiff"]
HANDLED_FILE_EXTENSIONS = [".pdf"]
# ART_PATH = ART_DIR / "psyche.db"

# Used for printing errors
RED = "\033[31m"
YELLOW = "\033[33m"
RESET = "\033[0m"

def _safe_destination(dest_dir: Path, filename: str) -> Path:
    # avoids overwriting existing files by appending -1, -2, ... before the extension.
    dest_dir.mkdir(parents=True, exist_ok=True)
    base = Path(filename).stem
    ext  = Path(filename).suffix
    candidate = dest_dir / f"{base}{ext}"
    i = 1
    while candidate.exists():
        candidate = dest_dir / f"{base}-{i}{ext}"
        i += 1
    return candidate


# returns a dictionary with keys [artTitle, artistName, date (returned as *month day, year*), artistMajor, genre, description]
def getArtInfo(url):
    # TODO: Make this part of a verbose option
    print("Starting to scrape: " + url)

    results = {}

    # grab the html and create a beautiful soup object of parsed HTML
    try:
        artPage = requests.get(url)
    except requests.exceptions.RequestException as e:
        print(RED + "[ERROR] There was an error accessing this art project: " + url + "[ERROR]" + RESET)
        print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option
        return None
    pageContent = BeautifulSoup(artPage.text, "html.parser")
    artContent = pageContent.find("div", class_="row justify-content-center")

    # grab sets of tags that are useful
    h2Tags = artContent.find_all("h2")
    h3Tags = artContent.find_all("h3")
    h4Tags = artContent.find_all("h4")
    pTags = artContent.find_all("p")
    aTags = artContent.find_all("a")
    iframeTag = pageContent.find("div", class_="gallery-slide").find("iframe")

    # art title is contained in the first h2 tag
    artTitle = h2Tags[0].text.strip()
    results["artTitle"] = artTitle
    # art title is in the first h3 tag without a class, or in the second h2Tag if there are none/only the h3 tag for the slides
    if len(h3Tags) == 0 or (len(h3Tags) == 1 and h3Tags[0].has_attr("class")):
        # There is an exception where the first p tag contains the artist name rather than the date
        if not cleanString(pTags[0].text)[-1].isdigit():
            results["artistName"] = standardizeName(pTags[0].text)
            # pretend the exception of the first p tag being the artist name never happened
            pTags.remove(pTags[0])
        else:
            results["artistName"] = standardizeName(h2Tags[1].text)
    else:
        artistName = artContent.find(lambda tag: tag.name == "h3" and not tag.has_attr("class")).text.strip()
        results["artistName"] = standardizeName(artistName)

    # compute project folder for name and title(project_id hash)
    project_id = make_project_id(results["artistName"], results["artTitle"])
    project_dir = ARTWORK_DIR / str(project_id)
    project_dir.mkdir(parents=True, exist_ok=True)

    # This will be how many p tags we have gone through before grabbing the description
    pTagCounter = -1
    # Rarely, there are links within the description in <a> tags, which is how we tell how we tell when the description is over, this variable always reads the first part of the description
    firstDescriptionTag = True

    # The more modern pages use h4 tags, the older ones use p tags
    pageTags = []
    if len(h4Tags) > 0:
        pageTags = h4Tags
        pTagCounter = 0

    else:
        pageTags = pTags
        pTagCounter = 3

    # date is always contained in the first h4 tag
    date = cleanString(pageTags[0].text)

    # A small amount of art entries combine art and major in the first p tag, so they will have a newline
    if date.find("\n") != -1:
        dateAndMajor = date.split("\n")
        try:
            results["date"] = standardizeDate(dateAndMajor[0])
        except ValueError as e:
            print(RED + "[ERROR] There was an error converting the date for project with title: " + results["title"] + ". Please change it manually [ERROR]" + YELLOW + "Continuing with incorrect date." + RESET)
            print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

        artistMajor = cleanString(dateAndMajor[1])
        results["artistMajor"] = artistMajor
    else:
        try:
            results["date"] = standardizeDate(date)
        except ValueError as e:
            print(RED + "[ERROR] There was an error converting the date for project with title: " + results["title"] + ". Please change it manually [ERROR]" + YELLOW + "Continuing with incorrect date." + RESET)
            print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

        # major is contained in the second h4 tag
        artistMajor = cleanString(pageTags[1].text)
        results["artistMajor"] = artistMajor

    # genre is contained in the third h4 tag
    genre = cleanString(pageTags[2].text)
    results["genre"] = genre

    description = ""
    currentPTag = pTags[pTagCounter]

    # read the description until we find an <a> tag, which means it is the end of the description
    while firstDescriptionTag or not currentPTag.find("a"):
        description += " " + currentPTag.text.strip()

        firstDescriptionTag = False
        pTagCounter += 1
        currentPTag = pTags[pTagCounter]
    results["description"] = standardizeDescription(cleanString(description))

    # list to hold all paths to generated files
    file_paths = []
    # download video if there is one embedded
    if type(iframeTag) is bs4.Tag and iframeTag.has_attr("src"):
        # create YouTube link from embedded source
        if "youtube" in iframeTag["src"]:
            print("Found a Youtube video")
            link = "https://www.youtube.com/watch?v=" + iframeTag["src"].split("/")[-1].split("?")[0]
            yt_link = YouTube(link)

            # names for video/audio
            base_video = f"{results['artistName'].replace(' ', '')}_{results['artTitle'].replace(' ', '')}.mp4"
            base_video_only = f"{results['artistName'].replace(' ', '')}_{results['artTitle'].replace(' ', '')}_VIDONLY.mp4"
            base_audio = f"{results['artistName'].replace(' ', '')}_{results['artTitle'].replace(' ', '')}_AUDIO.mp4"

            # download highest quality precombined mp4
            try:
                print ("Getting Youtube mp4 highest resolution (PreCombined)") # TODO: Make this part of a verbose option
                # absolute path for us, relative path for database and unity
                absolute_destination_video = _safe_destination(project_dir, base_video)
                relative_destination_video = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video.name

                yt_link.streams.get_highest_resolution().download(output_path=str(absolute_destination_video.parent), filename= absolute_destination_video.name)
                file_paths.append(str(relative_destination_video))
                print("Successfully downloaded Youtube video (PRECOMBINED) from " + link) # TODO: Make this part of a verbose option
            except Exception as e:
                print(YELLOW + "[ERROR] There was an error getting video + sound from: " + link + "[ERROR]. " + YELLOW + "Video + sound NOT added." + RESET)
                print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

             #Download HIGHEST QUALITY VIDEO ONLY
            try:
                print("Getting Youtube VIDEO ONLY")
                # absolute path for us, relative path for database and unity
                absolute_destination_video_only = _safe_destination(project_dir, base_video_only)
                relative_destination_video_only = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video_only.name
                #Find the highest quality non-progressive mp4
                #print("Video only Stream Prefind")
                #print(yt_link.streams.filter(adaptive=True,type='video'))
                ##yt_link.streams.get_by_itag(137).download(output_path=str(absolute_destination_video_only.parent), filename= absolute_destination_video_only.name)
                ##file_paths.append(str(relative_destination_video_only))
                #yt_test = yt_link.streams.filter(adaptive=True).order_by('res').desc().first()
                #print("Video only stream:")
                ##print("Successfully downloaded Youtube VIDEO ONLY from " + link)

                ydl_video_opts = {
                    'format' : 'bestvideo',
                    'outtmpl' : absolute_destination_video_only,
                    'quiet': 'False'
                }

                with yt_dlp.YoutubeDL(ydl_video_opts) as ydl:
                    error_code = ydl.download(link)
                file_paths.append(str(relative_destination_video_only))
                print("Successfully downloaded Youtube VIDEO ONLY from " + link)
            except Exception as e:
                print(RED + "[ERROR] There was an error getting video from: " + link + "[ERROR]. " + YELLOW + "Video NOT added." + RESET)
                print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

            #Download HIGHEST QUALITY AUDIO ONLY
            try:
                print("Getting Youtube AUDIO ONLY") # TODO: Make this part of a verbose option
                # absolute path for us, relative path for database and unity
                absolute_destination_audio = _safe_destination(project_dir, base_audio)
                relative_destination_audio = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_audio.name
                #Gets the highest quality audio stream
                ##yt_link.streams.get_audio_only().download(output_path=str(absolute_destination_audio.parent),filename=absolute_destination_audio.name)
                ##file_paths.append(str(relative_destination_audio))
                ##print("Successfully downoaded Youtube AUDIO ONLY from " + link)

                ydl_audio_opts = {
                    'format' : 'm4a/bestaudio/best',
                    'quiet' : False,
                    'outtmpl' : absolute_destination_audio
                }

                with yt_dlp.YoutubeDL(ydl_audio_opts) as ydl:
                    error_code = ydl.download(link)
                file_paths.append(str(relative_destination_audio))
                print("Successfully downloaded Youtube AUDIO ONLY from " + link) # TODO: Make this part of a verbose option
            except Exception as e:
                print(RED + "[ERROR] There was an error getting audio from: " + link + "[ERROR]. " + YELLOW + "Audio NOT added." + RESET)
                print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

        #Catch a vimeo video and convert it into an mp4 file.
        elif "vimeo" in iframeTag["src"]:
            print("Found a Vimeo video")
            try:
                print(url)
                print(iframeTag["src"])
                v = Vimeo(iframeTag["src"], embedded_on=url)
                print("Attempting to download vimeo file")
                base_video= f"{results['artistName'].replace(' ','')}_{results['artTitle'].replace(' ','')}.mp4"
                # absolute path for us, relative path for database and unity
                absolute_destination_video = _safe_destination(project_dir, base_video)
                relative_destination_video = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video.name

                v.streams[-1].download(download_directory=str(absolute_destination_video.parent),filename=absolute_destination_video.name)
                file_paths.append(str(relative_destination_video))
            except Exception as e:
                print(RED + "[ERROR] There was an error downloading the vimeo file from link: " + iframeTag["src"] + "[ERROR]. " + YELLOW + "Video NOT added." + RESET)
                print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option

        # For now this catches anything that isn't Youtube or Vimeo, we could add extra stuff here is something blows up.
        else:
            print(RED + "[ERROR] Something went wrong. We think we found a video but we do not recognize the host. [ERROR]. " + YELLOW + "Video NOT added." + RESET)


    # download regular art files if there is no video
    else:
        # find all download links
        download_link = []
        for tag in aTags:
            if tag.has_attr("class") and tag["class"] == ['link', 'mb-5', 'mr-sm']:
                if "," in tag["data-downloads"]:
                    download_link = tag["data-downloads"].split(",")
                else:
                    download_link = [tag["data-downloads"]]

        # download each link and store it in the media file
        for link in download_link:
            try:
                pdf = False

                # Send GET request to the URL
                response = requests.get(link)

                if response.status_code == 200:
                    orig_name = link.split("/")[-1]

                    fileExt = Path(orig_name).suffix
                    if not fileExt in ALLOWED_FILE_EXTENSIONS:
                        handleable = False
                        if fileExt in HANDLED_FILE_EXTENSIONS:
                            handleable = True
                        if fileExt == ".pdf":
                            pdf = True
                        if not handleable:
                            print(RED + "[ERROR] We cannot handle this type of file: " + fileExt +
                                  "\nPlease convert manually to one of the following + " + str(ALLOWED_FILE_EXTENSIONS) + "and add to the following project: " + results["title"] + "[ERROR]. " + YELLOW + "File NOT added." + RESET)
                            continue

                    # absolute path for us, relative path for database and unity
                    absolute_destination = _safe_destination(project_dir, orig_name)
                    relative_destination = Path("Assets") / "Artwork" / str(project_id) / absolute_destination.name
                    if pdf:
                        convertedFilePath = convertAndDownloadPDF(response, absolute_destination)
                        if not convertedFilePath is None:
                            file_paths.append(convertedFilePath)
                    else:
                        with open(absolute_destination, "wb") as f:
                            f.write(response.content)

                        file_paths.append(str(relative_destination))
                else:
                    file_paths.append("ERROR: " + link)

            except requests.exceptions.RequestException as e:
                print(RED + "[ERROR] There was an error downloading from the link: " + link + "[ERROR]. " + YELLOW + "Video NOT added." + RESET)
                print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option
                continue

    results["file_paths"] = file_paths

    # TODO: Make this part of a verbose option
    print("Results of " + url + ":")
    printArtProject(results)

    return results

# for debugging
def printArtProject(artInfo):
    print("-----------------------------------------------------------------------------------------------------------")
    print("Art Project Title:", artInfo["artTitle"])
    print("Artist:", artInfo["artistName"])
    print("Date:", artInfo["date"])
    print("Artist Major:", artInfo["artistMajor"])
    print("Genre:", artInfo["genre"])
    print("Description:", artInfo["description"])
    print("Art File Path(s):", artInfo["file_paths"])
    print("-----------------------------------------------------------------------------------------------------------")

# Get rid of trailing/leading whitespace and header (like "Date:") at the beginning of string (if it has it)
def cleanString(string):
    string = string.strip()
    colonIndex = string.find(":")
    if colonIndex != -1:
        string = string[colonIndex + 1:]
        string = string.strip()
    return string

# combines all pages of a pdf into one image (png)
def convertAndDownloadPDF(response, destination):
    try:
        destination = Path(destination).with_suffix(".png")

        # keep the pdf in memory rather than downloading it and having to change it later
        pdf = fitz.open(stream=response.content, filetype="pdf")
        images = []

        # make each page a PIL image
        for page_index in range(pdf.page_count):
            page = pdf.load_page(page_index)
            pix = page.get_pixmap()
            img = Image.open(BytesIO(pix.tobytes("png")))
            images.append(img)

        # stack images to combine all of them
        total_height = sum([img.height for img in images])
        max_width = max([img.width for img in images])
        combined = Image.new("RGB", (max_width, total_height), (255, 255, 255))

        yPos = 0
        for img in images:
            combined.paste(img, (0, yPos))
            yPos += img.height

        # save Image
        combined.save(destination)
        pdf.close()

        return str(destination)
    except Exception as e:
        print(RED + "[ERROR] There was an error converting a pdf to a png. [ERROR]. " + YELLOW + "File NOT added." + RESET)
        print(RED + str(e) + RESET)    # TODO: Make this part of a verbose option
        return None

# Make the first letter of each part of the name capitalized
def standardizeName(name):
    return name.title().strip()

# Get rid of any new lines
def standardizeDescription(description):
    if description.find("\n") != -1:
        return description.replace("\n", ". ")
    else:
        return description

# Takes a date in a form like "January 1st, 2025" or "January 1, 2025" and turns it into YYYY-MM-DD
def standardizeDate(date):
    # Remove suffixes (st, nd, rd, th)
    clean_date = re.sub(r'(\d+)(st|nd|rd|th)', r'\1', date)

    # Some month(s) are misspelled >:(
    dateTime = None
    try:
        # Put string into a datetime object
        dateTime = datetime.strptime(clean_date, "%B %d, %Y")
    except ValueError:
        # an instance where April is misspelled
        if clean_date.find("Arpil") != -1:
            index = clean_date.find("Arpil")
            clean_date = "April" + clean_date[index + 5:]
            dateTime = datetime.strptime(clean_date, "%B %d, %Y")
        # A couple instances of "month,date year"
        else:
            clean_date = clean_date.replace(",", " ")
            dateParts = clean_date.split(" ")
            clean_date = f"{dateParts[0]} {dateParts[1]}, {dateParts[2]}"
            try:
                dateTime = datetime.strptime(clean_date, "%B %d, %Y")
            except ValueError:
                raise ValueError

    # Format the datetime object as "YYYY-MM-DD" string (that is how SQL date is)
    return dateTime.strftime("%Y-%m-%d")

# *** DB path: one directory up - Psyche VR Experience/Assets/Database/psyche.db ***
HERE = Path(__file__).resolve().parent
DB_DIR = (HERE / ".." / "Psyche VR Experience" / "Assets" / "Database").resolve()
DB_DIR.mkdir(parents=True, exist_ok=True)
DB_PATH = DB_DIR / "psyche.db"

@contextmanager
def connection():
    # SQLite connection with FK enforcement, synchronous normal mode
    conn = sqlite3.connect(DB_PATH)
    try:
        conn.execute("PRAGMA foreign_keys = ON;")
        yield conn
        conn.commit()
    finally:
        conn.close()


def init_db():
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

# hash stuff to help generate hashes for project, artist, and media ids
def _norm(s: str) -> str:
    return s.strip().lower()

def _hash_to_int63(seed: str) -> int:
    h = hashlib.sha256(seed.encode("utf-8")).hexdigest()
    return int(h[:16], 16) & ((1 << 63) - 1)

def make_artist_id(artist_name: str) -> int:
    return _hash_to_int63(f"artist::{_norm(artist_name)}")

def make_project_id(artist_name: str, artwork_title: str) -> int:
    return _hash_to_int63(f"project::{_norm(artist_name)}::{_norm(artwork_title)}")

def make_media_id(artist_name: str, artwork_title: str, filepath: str) -> int:
    basename = Path(filepath).name
    return _hash_to_int63(f"media::{_norm(artist_name)}::{_norm(artwork_title)}::{_norm(basename)}")



# insert/update on artist table
def upsert_artist(artist_id: int, name: str, major: Optional[str] = None):
    with connection() as conn:
        conn.execute("""
            INSERT INTO artists (artist_id, name, major)
            VALUES (?, ?, ?)
            ON CONFLICT(artist_id) DO UPDATE SET
                name = excluded.name,
                major = excluded.major;
        """, (artist_id, name, major))

# insert/update on project table
def upsert_project(project_id: int, title: str, description: Optional[str],
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

# insert/update on media table
def upsert_media(media_id: int, filepath: str, media_type: str, project_id: int):
    with connection() as conn:
        conn.execute("""
            INSERT INTO project_media (media_id, filepath, media_type, project_id)
            VALUES (?, ?, ?, ?)
            ON CONFLICT(media_id) DO UPDATE SET
                filepath = excluded.filepath,
                media_type = excluded.media_type,
                project_id = excluded.project_id;
        """, (media_id, filepath, media_type, project_id))

# *should* detect media type. I am not sure all of the media types that get downloaded but all of the
# ones i am aware of are here. If they aren't correct or the lists aren't comprehensive for our use case,
# please update the areas lacking :)
def detect_media_type(filepath: str) -> str:
    ext = Path(filepath).suffix.lower()
    if ext in {".mp4", ".mov", ".m4v", ".avi", ".webm"}:
        return "video"
    if ext in {".mp3", ".wav", ".flac", ".ogg", ".m4a"}:
        return "audio"
    return "image"


def scrapePsyche():

    pageURL = "https://psyche.ssl.berkeley.edu/galleries/artwork/page/"
    pageNum = 1

    # Get the page with up to 16 art projects
    psychePage = requests.get(pageURL + str(pageNum))
    content = BeautifulSoup(psychePage.text, "html.parser")

    # Art project titles are held in span tags with the "caption title" - this while loop goes until none are found on the current page
    while artCaptions := content.find_all("a", class_="excerpt"):
        print("Starting page number: " + str(pageNum))       # TODO: Make this part of a verbose option

        projectLinks = []
        # for every title on the page ...
        for caption in artCaptions:
            # href has the link to the project page
            projectLinks.append(caption["href"])

        scrapedResults = []
        with ThreadPoolExecutor() as executor:
            scrapedResults = list(executor.map(getArtInfo, projectLinks))

        for artInfo in scrapedResults:
            if artInfo is None:
                continue

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
            # media hash and upsert
            for filepath in temp_files:
                media_type = detect_media_type(filepath)
                media_id = make_media_id(artist_name, art_title, filepath)
                upsert_media(media_id,filepath, media_type, project_id)

        # Move on and grab the content on the next page
        pageNum += 1
        psychePage = requests.get(pageURL + str(pageNum))
        content = BeautifulSoup(psychePage.text, "html.parser")

scrapePsyche()