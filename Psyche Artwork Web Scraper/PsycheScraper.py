import os.path
from datetime import datetime
import re
import sys
import subprocess

import fitz # PyMuPDF
from PIL import Image
from io import BytesIO
import bs4
import requests
from bs4 import BeautifulSoup
from pytubefix import YouTube
from vimeo_downloader import Vimeo
from concurrent.futures import ThreadPoolExecutor

import moviepy.editor as mp
import sqlite3
import hashlib
import shutil
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

# global list to collect failed download URLs
SUCCESS_DOWNLOADS = []
FAILED_DOWNLOADS = []

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

def safe_filename(s: str) -> str:
    # replace illegal filename characters with underscores.
    # windows forbidden(FORBIDDEN I SAY) chars:  \ / : * ? " < > |

    return re.sub(r'[\\/:*?"<>|]', "_", s)


# returns a dictionary with keys [artTitle, artistName, date (returned as *month day, year*), artistMajor, genre, description]
def getArtInfo(url, verbose):
    if verbose:
        print("Starting to scrape: " + url)

    results = {}

    # grab the html and create a beautiful soup object of parsed HTML
    try:
        artPage = requests.get(url)
    except requests.exceptions.RequestException as e:
        if verbose:
            print(RED + "[ERROR] There was an error accessing this art project: " + url + "[ERROR]" + RESET)
            print(RED + str(e) + RESET)
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
    results["artTitle"] = safe_filename(artTitle)
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
        if(cleanString(h4Tags[0].text) == "Riley Perry"):
            h4Tags.pop(0)
        pageTags = h4Tags
        pTagCounter = 0

    else:
        pageTags = pTags
        pTagCounter = 3

    # date is always contained in the first h4 tag
    date = cleanString(pageTags[0].text)
    # Special-case fix: some projects incorrectly put the artist name in the first <h4>.
    bad_h4 = cleanString(pageTags[0].text).lower() if pageTags else ""

    if bad_h4 == "riley perry":
        # Remove the incorrect <h4> that contains the artist name
        h4Tags.pop(0)

    # A small amount of art entries combine art and major in the first p tag, so they will have a newline
    if date.find("\n") != -1:
        dateAndMajor = date.split("\n")
        try:
            results["date"] = standardizeDate(dateAndMajor[0])
        except ValueError as e:
            if verbose:
                print(RED + "[ERROR] There was an error converting the date for project with title: " + results["title"] + ". Please change it manually [ERROR]" + YELLOW + "Continuing with incorrect date." + RESET)
                print(RED + str(e) + RESET)

        artistMajor = cleanString(dateAndMajor[1])
        results["artistMajor"] = artistMajor
    else:
        try:
            results["date"] = standardizeDate(date)
        except ValueError as e:
            if verbose:
                print(RED + "[ERROR] There was an error converting the date for project with title: " + results["title"] + ". Please change it manually [ERROR]" + YELLOW + "Continuing with incorrect date." + RESET)
                print(RED + str(e) + RESET)

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
            if verbose:
                print("Found a Youtube video")
            link = "https://www.youtube.com/watch?v=" + iframeTag["src"].split("/")[-1].split("?")[0]
            yt_link = YouTube(link)

            # names for video/audio
            safe_artist = safe_filename(results["artistName"].replace(" ", ""))
            safe_title = safe_filename(results["artTitle"].replace(" ", ""))

            base_video = f"{safe_artist}_{safe_title}_FINAL.mp4"
            base_video_only = f"{safe_artist}_{safe_title}_VIDONLY.mp4"
            base_audio = f"{safe_artist}_{safe_title}_AUDIO.mp4"

            # download highest quality precombined mp4
            try:
                # absolute path for us, relative path for database and unity
                absolute_destination_video = _safe_destination(project_dir, base_video)
                relative_destination_video = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video.name
            except Exception as e:
                print("Error setting the path for final video: ", e)
            
             #Download HIGHEST QUALITY VIDEO ONLY
            try:
                if verbose:
                    print("Getting Youtube VIDEO ONLY")
                # absolute path for us, relative path for database and unity
                absolute_destination_video_only = _safe_destination(project_dir, base_video_only)
                relative_destination_video_only = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video_only.name
                #Find the highest quality non-progressive mp4

                ydl_video_opts = {
                    'format' : 'bv/b',
                    'outtmpl' : str(absolute_destination_video_only),
                    'quiet': False,
                    'retries': 5,
                    'ignoreerrors': False,
                    'noplaylist': True,
                }

                with yt_dlp.YoutubeDL(ydl_video_opts) as ydl:
                    ydl.download(link)
                file_paths.append(str(relative_destination_video_only))
                print("Successfully downloaded Youtube VIDEO ONLY from " + link)
                if verbose:
                    print("Successfully downloaded Youtube VIDEO ONLY from " + link)  
                    SUCCESS_DOWNLOADS.append({'url': link, 'stage': 'video', 'dest': file_paths})
            except Exception as e: 
                if verbose:
                    print(RED + "[ERROR] There was an error getting video from: " + link + "[ERROR]. " + YELLOW + "Video NOT added." + RESET)
                    print(RED + str(e) + RESET)
                    FAILED_DOWNLOADS.append({'url': link, 'stage': 'video', 'error': str(e)})

            # Force video color primaries to Rec.709 to avoid Unity warnings
            try:
                video_input_path = Path(absolute_destination_video_only).resolve()
                corrected_path = absolute_destination_video_only.with_suffix(".temp.mp4")

                (ffmpeg.input(video_input_path)).output(
                    str(corrected_path),
                    vcodec="libx264",
                    pix_fmt="yuv420p",
                    color_primaries="bt709",
                    color_trc="bt709",
                    colorspace="bt709",
                    acodec="copy"  # video-only, so no audio
                ).overwrite_output().run()
                

                # Replace original with corrected version
                os.remove(absolute_destination_video_only)
                corrected_path.rename(absolute_destination_video_only)

                if verbose:
                    print("Corrected video to Rec.709 for Unity compatibility.")

            except Exception as e:
                print("[WARNING] Failed to convert video to Rec.709:", e)
                print(ffmpeg.__file__)
                print(ffmpeg.__path__)

            #Download HIGHEST QUALITY AUDIO ONLY
            try:
                if verbose:
                    print("Getting Youtube AUDIO ONLY")
                # absolute path for us, relative path for database and unity
                absolute_destination_audio = _safe_destination(project_dir, base_audio)
                relative_destination_audio = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_audio.name

                #Finds the Highest Quality Audio
                ydl_audio_opts = {
                    'format' : 'ba/b',
                    'quiet' : False,
                    'outtmpl' : str(absolute_destination_audio),
                    'retries': 5,
                    'noplaylist': True,
                    'ignoreerrors': False
                }

                with yt_dlp.YoutubeDL(ydl_audio_opts) as ydl:
                    ydl.download(link)
                    # converting audio only from mp4 to mp3
                    try:
                        mp3_path = absolute_destination_audio.with_suffix(".mp3")

                        (
                            ffmpeg
                            .input(str(absolute_destination_audio))
                            .output(str(mp3_path), acodec="mp3", audio_bitrate="192k",
                                    af="loudnorm=I=-14:TP=-1.5:LRA=11")
                            .overwrite_output()
                            .run()
                        )

                        # remove original mp4 audio file after successful conversion
                        os.remove(absolute_destination_audio)

                        # update absolute/relative path (Unity + DB expect/can handle mp3)
                        relative_mp3 = relative_destination_audio.with_suffix(".mp3")
                        absolute_destination_audio = mp3_path
                        file_paths.append(str(relative_mp3))
                        if verbose:
                            print("Converted audio to MP3:", mp3_path)

                        SUCCESS_DOWNLOADS.append({'url': link, 'stage': 'audio-mp3', 'dest': file_paths})

                    except Exception as e:
                        print("Error converting audio to MP3:", e)
                        # fallback: keep original mp4 audio
                        file_paths.append(str(relative_destination_audio))

            except Exception as e:
                 if verbose:
                    print(RED + "[ERROR] There was an error getting audio from: " + link + "[ERROR]. " + YELLOW + "Audio NOT added." + RESET)
                    print(RED + str(e) + RESET)
                    FAILED_DOWNLOADS.append({'url': link, 'stage': 'audio', 'error': str(e)})

            # combining the video and audio
            try:
                print("Preparing to combine video and audio together")
                #Get the input paths from the yt-dlp downloads
                video_input_path = Path(absolute_destination_video_only).resolve()
                final_output_path = Path(absolute_destination_video).resolve()
                # determine best available audio file (MP3 preferred, else MP4)
                audio_mp3 = absolute_destination_audio if absolute_destination_audio.suffix == ".mp3" else absolute_destination_audio.with_suffix(
                    ".mp3")
                audio_mp4 = absolute_destination_audio if absolute_destination_audio.suffix == ".mp4" else absolute_destination_audio.with_suffix(
                    ".mp4")

                audio_input_path = None

                if audio_mp3.exists():
                    audio_input_path = audio_mp3.resolve()
                elif audio_mp4.exists():
                    audio_input_path = audio_mp4.resolve()
                
                if(video_input_path.exists() and audio_input_path and audio_input_path.exists()):
                    #Tries to combine the two files together using FFMPEG
                    try:
                        # Create the audio and video inputs
                        video_input = ffmpeg.input(str(video_input_path))
                        # SUCCESS_DOWNLOADS.append({'url':link,'stage':'2combineVIDEO2','dest': video_input})
                        audio_input = ffmpeg.input(str(audio_input_path))
                        # SUCCESS_DOWNLOADS.append({'url':link,'stage':'2combineAUDIO2','dest': audio_input})
                        ffmpeg.output(audio_input, video_input, str(final_output_path),
                                      vcodec='copy', color_primaries="bt709", color_trc="bt709",
                                      colorspace="bt709", acodec='copy', format='mp4').run(overwrite_output=True)
                        SUCCESS_DOWNLOADS.append({'url': link, 'stage': 'combine', 'dest': file_paths})
                        # remove source files ONLY if final output exists and is non-zero
                        if final_output_path.exists() and final_output_path.stat().st_size > 0:
                            try:
                                if absolute_destination_video_only.exists():
                                    os.remove(absolute_destination_video_only)
                                if audio_mp3.exists():
                                    os.remove(audio_mp3)
                                if audio_mp4.exists():
                                    os.remove(audio_mp4)
                            except Exception as e:
                                print("Warning: Could not remove intermediate files:", e)
                        else:
                            print("WARNING: Final combined video missing or empty — keeping source video/audio.")
                    except Exception as e:
                        print("Error when trying to Combine the videos" , e)
                        FAILED_DOWNLOADS.append({'url': link, 'stage': 'Combination', 'error': str(e)})
                    
                    #Adds the new video to the file_paths   
                    file_paths.append(str(relative_destination_video))
                    print("Successfully combined Video and Audio into one finalized version")
                    #DEBUG: Tells you which videos succeedded
                    #SUCCESS_DOWNLOADS.append({'url': link, 'stage': '2combine', 'dest': file_paths})
                else:
                    ydl_final_opts = {
                        'format' : 'b',
                        'quiet' : False,
                        'outtmpl' : absolute_destination_video,
                        'retries': 5,
                        'noplaylist': True,
                        'ignoreerrors': False
                        }
                   
                    try: 
                        with yt_dlp.YoutubeDL(ydl_final_opts) as ydl:
                            ydl.download(link)
                            print("Successfully downloaded the best precombined version of ", link)
                            SUCCESS_DOWNLOADS.append({'url': link, 'stage': 'precombined', 'dest': file_paths})
                    except Exception as e:
                        print("There was an issue downloading the best COMBINED video file ", e, link)
                    # SAFE DELETE only if fallback final exists
                    if final_output_path.exists() and final_output_path.stat().st_size > 0:
                        try:
                            if absolute_destination_video_only.exists():
                                os.remove(absolute_destination_video_only)
                            if absolute_destination_audio.exists():
                                os.remove(absolute_destination_audio)
                        except Exception as e:
                            print("Warning: Could not remove intermediate files:", e)
                    else:
                        print("WARNING: Fallback final file missing — keeping VIDONLY and AUDIO sources.")
                    file_paths.append(str(relative_destination_video))
            except Exception as e:
                print("Error combining audio and video files together into one")
                try:
                    FAILED_DOWNLOADS.append({'url': link, 'stage': 'precombined', 'error': str(e)})
                except Exception:
                    pass
            
        #Catch a vimeo video and convert it into an mp4 file.
        elif "vimeo" in iframeTag["src"]:
            if verbose:
                print("Found a Vimeo video")
            try:
                if verbose:
                    print(url)
                    print(iframeTag["src"])
                    print("Attempting to download vimeo file")
                v = Vimeo(iframeTag["src"], embedded_on=url)
                print("Attempting to download vimeo file")
                base_video= f"{results['artistName'].replace(' ','')}_{results['artTitle'].replace(' ','')}.mp4"
                # absolute path for us, relative path for database and unity
                absolute_destination_video = _safe_destination(project_dir, base_video)
                relative_destination_video = Path("Assets") / "Artwork" / str(project_id) / absolute_destination_video.name

                #v.streams[-1].download(download_directory=str(absolute_destination_video.parent),filename=absolute_destination_video.name)
                try:
                    with yt_dlp(ydl_video_opts) as ydl:
                        ydl.download(iframeTag["src"])
                        print("Downloading VIMEO video was a success!")
                except Exception as e:
                    print("There was an error downloading the vimeo file")
                file_paths.append(str(relative_destination_video))
                SUCCESS_DOWNLOADS.append({'url': link, 'stage': 'vimeo', 'dest': file_paths})
            except Exception as e:
                if verbose:
                    print(RED + "[ERROR] There was an error downloading the vimeo file from link: " + iframeTag["src"] + "[ERROR]. " + YELLOW + "Video NOT added." + RESET)
                    print(RED + str(e) + RESET)
                    FAILED_DOWNLOADS.append({'url': iframeTag["src"], 'stage': 'vimeo', 'error': str(e)})
        #For now this catches anything that isn't Youtube or Vimeo, we could add extra stuff here is something blows up.
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
                        convertedFilePath = convertAndDownloadPDF(response, absolute_destination, verbose)
                        if not convertedFilePath is None:
                            file_paths.append(convertedFilePath)
                    else:
                        with open(absolute_destination, "wb") as f:
                            f.write(response.content)

                        file_paths.append(str(relative_destination))
                else:
                    file_paths.append("ERROR: " + link)

            except requests.exceptions.RequestException as e:
                if verbose:
                    print(RED + "[ERROR] There was an error downloading from the link: " + link + "[ERROR]. " + YELLOW + "File NOT added." + RESET)
                    print(RED + str(e) + RESET)
                continue
    # remove any file paths pointing to files that no longer exist(video_only/audio/.mp4)
    cleaned_paths = []
    for p in file_paths:
        abs_path = ARTWORK_DIR.parent.parent / p  # convert relative to absolute
        if abs_path.exists():
            cleaned_paths.append(p)
        else:
            if verbose:
                print(f"[CLEANUP] Removing missing file from DB entry: {p}")

    file_paths = cleaned_paths

    results["file_paths"] = file_paths

    # if verbose:
    #     print("Results of " + url + ":")
    #     printArtProject(results)

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

#     converts a GIF to MP4 for Unity VideoPlayer compatibility. Returns path to the MP4.
def convertGIFtoMp4(gif_path: Path, verbose=True) -> Path:
    mp4_path = gif_path.with_name(gif_path.stem+"_GIF.mp4")

    try:
        if verbose:
            print(f"[GIF -> MP4] Converting {gif_path.name}")

        clip = mp.VideoFileClip(str(gif_path))
        clip.write_videofile(
            str(mp4_path),
            codec="libx264",
            audio=False,
            fps=clip.fps or 24
        )
        clip.close()

        if mp4_path.exists() and mp4_path.stat().st_size > 0:
            gif_path.unlink()  # delete original gif
            return mp4_path

    except Exception as e:
        if verbose:
            print(f"[GIF → MP4 ERROR] {gif_path}: {e}")

    return gif_path  # fallback


# combines all pages of a pdf into one image (png)
def convertAndDownloadPDF(response, destination, verbose):
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
        if verbose:
            print(RED + "[ERROR] There was an error converting a pdf to a png. [ERROR]. " + YELLOW + "File NOT added." + RESET)
            print(RED + str(e) + RESET)
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

#This verifies key packages exist on a users computer before running any of the scrapers functionalities.
def verify_packages():
    try:
        ffmpeg_result = subprocess.run(['ffmpeg', '-version'], capture_output = True, text = True, check = True)
        print(ffmpeg_result)
    except subprocess.CalledProcessError as e:
        print("FFMPEG verification check failed! ", e)
    except FileNotFoundError as e:
        print("FFMPEG is not installed or is not within the system PATH. Please see README for help!")
        sys.exit(-1)
    try:
        yt_dlp_result = subprocess.run(['yt-dlp', '--version'], capture_output = True, text = True, check = True)
        print(yt_dlp_result)
    except subprocess.CalledProcessError as e:
        print("yt_dlp verification check failed! ", e)
    except FileNotFoundError as e:
        print("yt_dlp is not installed or is out of date. Please see README for help!")
        sys.exit(-1)


def scrapePsyche(verbose=False):

    #Call up to the package verification function to ensure that key packages are up to date
    verify_packages()

    #Grab the page URL for the scraper
    pageURL = "https://psyche.ssl.berkeley.edu/galleries/artwork/page/"
    pageNum = 1

    # Get the page with up to 16 art projects
    psychePage = requests.get(pageURL + str(pageNum))
    content = BeautifulSoup(psychePage.text, "html.parser")

    # Art project titles are held in span tags with the "caption title" - this while loop goes until none are found on the current page
    while artCaptions := content.find_all("a", class_="excerpt"):
        if verbose:
            print("Starting page number: " + str(pageNum))

        projectLinks = []
        # for every title on the page ...
        for caption in artCaptions:
            # href has the link to the project page
            projectLinks.append(caption["href"])

        scraped_results = []
        with ThreadPoolExecutor() as executor:
            verbose_list = [verbose] * len(projectLinks)
            scraped_results = list(executor.map(getArtInfo, projectLinks, verbose_list))

        for artInfo in scraped_results:
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

    # For debugging purposes, after scraping completes, print any failed downloads
    if FAILED_DOWNLOADS:
        print("\nThe following video downloads failed:")
        for failed in FAILED_DOWNLOADS:
            url = failed.get('url') if isinstance(failed, dict) else str(failed)
            stage = failed.get('stage') if isinstance(failed, dict) else 'unknown'
            err = failed.get('error') if isinstance(failed, dict) else ''
            print(f" - [{stage}] {url} {(' - ' + err) if err else ''}")
    else:
        print("\nAll video downloads completed successfully.")

    #For debugging purposes, this lists all the successful downloads
    if SUCCESS_DOWNLOADS:
        print("\nThe following video downloads succeeded:")
        for succeeded in SUCCESS_DOWNLOADS:
            url = succeeded.get('url') if isinstance(succeeded, dict) else str(succeeded)
            stage = succeeded.get('stage') if isinstance(succeeded, dict) else 'unknown'
            #dest = succeeded.get('dest') if isinstance(succeeded, dict) else ''
            print(f" - [{stage}] {url}")
    else:
        print("\n There were no successful video downloads...")
# post-scrape repair pass for any media files left un-affected by the necessary conversions
# converts leftover AUDIO.mp4 → .mp3
# logs missing video/audio pairs
# logs failed combinations
# logs successful combinations
def repair_media(verbose=True):

    print("\n*** Running Media Repair Pass ***")

    orphan_audio = []
    orphan_video = []
    converted_audio = []
    missing_final_video = []
    combined_success = []
    combined_failed = []
    errors = []

    for project_dir in ARTWORK_DIR.iterdir():
        if not project_dir.is_dir():
            continue

        media_files = list(project_dir.glob("*"))
        base_map = {}

        # group files by base name (before suffixes like _VIDONLY,_AUDIO, etc.)
        for f in media_files:
            name = f.name
            base = name.replace("_VIDONLY", "").replace("_AUDIO", "").replace("_FINAL", "").replace("_rec709","")
            base = Path(base).stem
            base_map.setdefault(base, []).append(f)

        # analyze each media group to determine status
        for base, group in base_map.items():

            has_vidonly = any("_VIDONLY" in f.stem for f in group)
            has_audio = any("_AUDIO" in f.stem for f in group)
            has_final = any("_FINAL" in f.stem for f in group)

            audio_file = next((f for f in group if "_AUDIO" in f.stem), None)
            video_file = next((f for f in group if "_VIDONLY" in f.stem), None)

            # repair type 1- convert leftover audio-only .mp4 files
            if audio_file and audio_file.suffix.lower() == ".mp4":
                try:
                    mp3_path = audio_file.with_suffix(".mp3")

                    (
                        ffmpeg
                        .input(str(audio_file))
                        .output(str(mp3_path), acodec="mp3", audio_bitrate="192k")
                        .overwrite_output()
                        .run()
                    )

                    os.remove(audio_file)
                    converted_audio.append(str(mp3_path))

                    if verbose:
                        print(f"[MEDIA REPAIR : AUDIO FIX] Converted: {audio_file} → {mp3_path}")

                except Exception as e:
                    errors.append(f"{audio_file}: {e}")
                    if verbose:
                        print(f"[ERROR : MEDIA REPAIR : AUDIO FIX] Failed to convert audio {audio_file}: {e}")

            # repair type 2- orphaned audio (missing corresponding video(main culprit of issues in unity end))
            if has_audio and not has_vidonly and not has_final:
                orphan_audio.append(base)

            # repair type 3- orphaned video (missing corresponding audio(not frequent, haven't seen it actually))
            if has_vidonly and not has_audio and not has_final:
                orphan_video.append(base)

            # repair type 4- combined video never created(This is just here in the case of ffmpeg errors , not frequent)
            if has_vidonly and has_audio and not has_final:
                missing_final_video.append(base)

            # repair type 5- verify & enforce Rec.709(color primaries encoding)
            final_file = next((f for f in group if "_FINAL" in f.stem), None)

            if final_file:
                try:
                    # create corrected file name (safe overwrite)
                    corrected_final = final_file.with_name(final_file.stem + "_temp.mp4")

                    (
                        ffmpeg
                        .input(str(final_file))
                        .output(
                            str(corrected_final),
                            vcodec="libx264",
                            pix_fmt="yuv420p",
                            color_primaries="bt709",
                            color_trc="bt709",
                            colorspace="bt709",
                            acodec="copy"
                        )
                        .overwrite_output()
                        .run()
                    )

                    # replace original file with corrected version
                    os.remove(final_file)
                    corrected_final.rename(final_file)

                    combined_success.append(base)
                    if verbose:
                        print(f"[MEDIA REPAIR : REC709 FIX] Corrected final video: {final_file}")

                except Exception as e:
                    combined_success.append(base)
                    errors.append(f"[MEDIA REPAIR : REC709 : ERROR] {final_file}: {e}")
                    if verbose:
                        print(f"[MEDIA REPAIR : REC709 : ERROR] Failed Rec.709 correction for {final_file}: {e}")

    # *** MEDIA REPAIR : SUMMARY OUTPUT ***
    print("\n*** MEDIA REPAIR : SUMMARY ***")

    if converted_audio:
        print("\n✔ Converted Audio Files:")
        for a in converted_audio:
            print("   -", a)

    if combined_success:
        print("\nCombined Videos Present:")
        for c in combined_success:
            print("   -", c)

    if missing_final_video:
        print("\nMissing Combined Videos (VIDONLY + AUDIO exist but _FINAL missing):")
        for m in missing_final_video:
            print("   -", m)

    if orphan_audio:
        print("\nAUDIO Orphans (no matching video):")
        for o in orphan_audio:
            print("   -", o)

    if orphan_video:
        print("\nVIDEO Orphans (no matching audio):")
        for o in orphan_video:
            print("   -", o)

    if errors:
        print("\nErrors:")
        for e in errors:
            print("   -", e)

    print("\n*** MEDIA REPAIR : COMPLETE ***\n")
        
init_db()
scrapePsyche(verbose=True)
repair_media(verbose=True)