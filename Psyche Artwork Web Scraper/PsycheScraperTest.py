import os
import unittest
import PsycheScraper
from pathlib import Path

from dotenv import load_dotenv

class PsycheScraperTest(unittest.TestCase):
    def testEnvironmentVariablesExist(self):
        load_dotenv()
        
        self.assertIsNotNone(os.getenv("OUTPUT_DIR"))

    def testEnvironmentVariableValuesInPsycheScraper(self):
        load_dotenv()
        
        self.assertEqual(Path(os.getenv('OUTPUT_DIR')) / "Artwork", PsycheScraper.ARTWORK_DIR)
        self.assertEqual(Path(os.getenv('OUTPUT_DIR')) / "Database", PsycheScraper.DB_DIR)

    # test that it works on newer art
    def testGetArtInfo2025(self):
        info = PsycheScraper.getArtInfo("https://psyche.ssl.berkeley.edu/gallery/the-launch/", False)
        self.assertEqual(info["artTitle"], "The Launch")
        self.assertEqual(info["artistName"], "Ash Soriano")
        self.assertEqual(info["date"], "2025-07-02")
        self.assertEqual(info["artistMajor"], "Media Arts & Sciences")
        self.assertEqual(info["genre"], "Digital (Procreate)")

        description = "“The Launch” is a short poster comic as well as my fourth and final project for Psyche Inspired! After spending an academic year learning more about the Psyche mission, I was really inspired (if you will) by the story of how Psyche came to be, particularly the launch! I loved hearing the perspectives from the folks who were there to witness liftoff in person, and I wanted to pay homage to that experience. There has been so much talk about the potential of Psyche (both the spacecraft and the asteroid) but sometimes it is easy to forget about what humans were already able to accomplish in getting the spacecraft out there in the first place. What a feat of curiosity, intelligence, and, truly, imagination! I wanted to capture that in “The Launch” as much as I could, especially in depicting the beginning (building and preparation), middle (the launch itself), and the ongoing story (the spacecraft on its journey) of Psyche. I created this, much like my other pieces, digitally using Procreate. I opted for a dominant blue palette with some textured brushes to give it more of a “storybook” feel, almost like a nostalgic memory. I also tied in some textured brushes I used in my very first project to kind of bring it full circle as a nod to where I started at the beginning of this internship— much like this piece pays homage to the beginnings of the mission!"
        self.assertEqual(info["description"], description)

        self.assertEqual(info["file_paths"], [os.path.join("6621986371799769726", "Psyche_Inspired_24-25_ASoriano_TheLaunch_4.1_4.9.25-Ash-Soriano.png")])

    # test that it works on older art
    def testGetArtInfo2017(self):
        info = PsycheScraper.getArtInfo("https://psyche.ssl.berkeley.edu/gallery/ideas/", False)
        self.assertEqual(info["artTitle"], "Ideas")
        self.assertEqual(info["artistName"], "Isaac Wisdom")
        self.assertEqual(info["date"], "2017-11-18")
        self.assertEqual(info["artistMajor"], "Music and culture")
        self.assertEqual(info["genre"], "Composition")

        description = "Ideas is a piece I wrote for a mallet ensemble that was inspired by the feelings and emotions that might have been felt by the person or group of people as they were first conceptualizing Psyche. In addition to writing out each part, I also specified how the ensemble should be set up. Certain instruments are meant to be on either the left or the right side, which allowed me to write the music with interest to the spatial relation between instruments. Interpretation of music is always subjective, of course, but hopefully you will feel the curiosity, wonder, and excitement that was present during the conception of Psyche, and that I convey through this piece."
        self.assertEqual(info["description"], description)

        self.assertEqual(info["file_paths"], [os.path.join("6587082944754327409", "Psyche_Inspired_17-18_IWisdom_Project1_Ideas_171118.png")])


if __name__ == '__main__':
    unittest.main()
