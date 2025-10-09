import os.path
from datetime import datetime
import re

import bs4
import requests
from bs4 import BeautifulSoup
from pytubefix import YouTube

# returns a dictionary with keys [artTitle, artistName, date (returned as *month day, year*), artistMajor, genre, description]
def getArtInfo(url):
    results = {}

    # grab the html and create a beautiful soup object of parsed HTML
    artPage = requests.get(url)
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
        results["date"] = standardizeDate(dateAndMajor[0])

        artistMajor = cleanString(dateAndMajor[1])
        results["artistMajor"] = artistMajor
    else:
        results["date"] = standardizeDate(date)

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

    # create media folder if it doesn't already exist
    os.makedirs(os.path.join(os.getcwd(), "psyche_media"), exist_ok=True)

    # list to hold all paths to generated files
    file_paths = []
    # download video if there is one embedded
    if type(iframeTag) is bs4.Tag and iframeTag.has_attr("src"):
        # create YouTube link from embedded source
        link = "https://www.youtube.com/watch?v=" + iframeTag["src"].split("/")[-1].split("?")[0]
        yt_link = YouTube(link)

        # download video
        try:
            yt_link.streams.filter(progressive=True, file_extension="mp4").first().download(output_path= os.path.join(os.getcwd(), "psyche_media"), filename = results["artistName"] + results["artTitle"] + ".mp4")
            file_paths.append(os.path.join("psyche_media", results["artistName"].replace(" ", "") + results["artTitle"].replace(" ", "") + ".mp4"))
        except Exception as e:
            print("Error downloading video from link " + link)

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
                # Send GET request to the URL
                response = requests.get(link)

                if(response.status_code == 200):
                    with open(os.path.join(os.getcwd(), "psyche_media", link.split("/")[-1]), 'wb') as file:
                        file.write(response.content)
                        file_paths.append(os.path.join("psyche_media", link.split("/")[-1]))
                else:
                    file_paths.append("ERROR: " + link)

            except requests.exceptions.RequestException as e:
                print("There was an error downloading the link " + link)
                print(e)

                file_paths.append("ERROR: " + link)

    results["file_paths"] = file_paths


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
            dateTime = datetime.strptime(clean_date, "%B %d, %Y")

    # Format the datetime object as "YYYY-MM-DD" string (that is how SQL date is)
    return dateTime.strftime("%Y-%m-%d")

def scrapePsyche():
    pageURL = "https://psyche.ssl.berkeley.edu/galleries/artwork/page/"
    pageNum = 1
    projectID = 0

    # Get the page with up to 16 art projects
    psychePage = requests.get(pageURL + str(pageNum))
    content = BeautifulSoup(psychePage.text, "html.parser")

    # Art project titles are held in span tags with the "caption title" - this while loop goes until none are found on the current page
    while artCaptions := content.find_all("a", class_="excerpt"):
        # for every title on the page ...
        for caption in artCaptions:
            # href has the link to the project page
            print(str(projectID) + ": " + caption["href"])      # TODO: delete this, it's for debugging
            artInfo = getArtInfo(caption["href"])
            projectID += 1
            printArtProject(artInfo)        # TODO: delete this, it's for debugging

        # Move on and grab the content on the next page
        pageNum += 1
        psychePage = requests.get(pageURL + str(pageNum))
        content = BeautifulSoup(psychePage.text, "html.parser")
        print("Starting page number: " + str(pageNum))       # TODO: delete this, it's for debugging

scrapePsyche()