using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONHandler : MonoBehaviour
{
    string artworkName;
    string artistName;
    string artworkDescription;
    string genre;
    string artistInformation;
    string artworkDate;
    //List<string> artworkURLs;
    int artworkCount;
    int artworkID;

    [System.Serializable]
    public class ArtObject 
    {
        public string artworkName;
        public string artistName;
        public string artworkDescription;
        public string genre;
        public string artistInformation;
        public string artworkDate;
        //public List<string> artworkURLs;
        public int artworkCount;
        public int artworkID;
    }

    // Theoretically if we wanted to save stuff back to JSON... probably won't be used a ton
    public void Save()
    {
        Debug.Log("Saving to " + Application.dataPath);
        
        artworkName = "Test Artwork #1";
        artistName = "Sir Foo of Bar";
        artworkDescription = "This is a test of the save functionality";
        genre = "Foo";
        artistInformation = "Graduated from PEBKAC in May of 2026";
        artworkDate = "10-06-2025";
        //artworkURLs = artworkURLs.Add("Hello World");
        artworkCount = 13;
        artworkID = 67;
        ArtObject artObject = new ArtObject 
        {
            artworkName = artworkName,
            artistName = artistName,
            artworkDescription = artworkDescription,
            genre = genre,
            artistInformation = artistInformation,
            artworkDate = artworkDate,
            //artworkURLs = artworkURLs, //May need to check this one...
            artworkCount = artworkCount,
            artworkID = artworkID
        };

        string json = JsonUtility.ToJson(artObject);

        File.WriteAllText(Application.dataPath + "/artworkData.txt", json);    
    }

    // Used to load our data from a JSON text file
    public void Load()
    {
        if (File.Exists(Application.dataPath + "/artworkData.txt"))
        {
            string artString = File.ReadAllText(Application.dataPath + "/artworkData.txt");
            Debug.Log("Loaded: " + artString);

            ArtObject artObject = JsonUtility.FromJson<ArtObject>(artString);
            
            Debug.Log(artObject.artworkName);
            Debug.Log(artObject.artistName);
            Debug.Log(artObject.artworkDescription);
            Debug.Log(artObject.genre);
            Debug.Log(artObject.artistInformation);
            Debug.Log(artObject.artworkDate);
            //Debug.Log(artObject.artWorkURLs);
            Debug.Log(artObject.artworkCount);
            Debug.Log(artObject.artworkID);
        }
    }
}
