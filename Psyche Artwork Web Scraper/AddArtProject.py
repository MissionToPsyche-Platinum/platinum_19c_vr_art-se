from PsycheScraper import upsert_artist, upsert_project, upsert_media, make_artist_id, make_project_id, detect_media_type, make_media_id

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
    # media hash and upsert
    for filepath in temp_files:
        media_type = detect_media_type(filepath)
        media_id = make_media_id(artist_name, art_title, filepath)
        upsert_media(media_id,filepath, media_type, project_id)
