import requests
from bs4 import BeautifulSoup

# returns a list of [artTitle, artistName, date (returned as *month day, year*), artistMajor, genre, description]
def getArtInfo(url):
    results = []

    # grab the html and create a beautiful soup object of parsed HTML
    artPage = requests.get(url)
    content = BeautifulSoup(artPage.text, "html.parser")

    # art title is contained in the first (and only) h2 tag with class pt-3 m-0
    artTitle = content.find("h2", class_="pt-3 m-0").text
    results.append(artTitle)

    # artist name is contained in the first h3 tag
    artistName = content.find("h3").text
    results.append(artistName)

    h4Tags = content.find_all("h4")

    # The more modern pages use h4 tags
    if len(h4Tags) > 0:
        # date is contained in the first h4 tag
        date = h4Tags[0].text
        results.append(date)

        # major is contained in the second h4 tag (excluding the first 7 characters, which say "Major: ")
        artistMajor = h4Tags[1].text[7:]
        results.append(artistMajor)

        # genre is contained in the third h4 tag (excluding the first 14 characters, which say "Genre/Medium: ")
        genre = h4Tags[2].text[14:]
        results.append(genre)

        # description is contained in the first p tag
        description = content.find("p").text
        results.append(description)
    else:
        pTags = content.find_all("p")

        # date is contained in the first p tag (excluding the first 6 characters, which say "Date: ", and a trailing space)
        date = pTags[0].text[6:-1]
        results.append(date)
        print(date[-1])

        # major is contained in the second p tag (excluding the first 7 characters, which say "Major: ", and a trailing space)
        artistMajor = pTags[1].text[7:-1]
        results.append(artistMajor)

        # genre is contained in the third p tag (excluding the first 14 characters, which say "Genre/Medium: ", and a trailing space)
        genre = pTags[2].text[14:-1]
        results.append(genre)

        # description is contained in the fourth p tag (excluding the first 16 characters, which say "About the work: ")
        description = pTags[3].text[16:]
        results.append(description)


    return results



artURL = "https://psyche.ssl.berkeley.edu/gallery/light-curves/"
getArtInfo(artURL)

