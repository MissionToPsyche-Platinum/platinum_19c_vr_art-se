using UnityEngine;

[CreateAssetMenu(fileName = "ArtworkData", menuName = "Scriptable Objects/ArtworkData")]
public class ArtworkData : ScriptableObject
{
    [SerializedField] public String artworkName; //Use this field for the artwork's name
    [SerializedField] public String artistName; //Use this field for the artist's name
    [SerializedField] public String artworkDescription; //Use this field for the artwork description
    [SerializedField] public String genre; //Use this field for the artwork genre
    [SerializedField] public String artistInformation; //Use this field for any extra information about the artist
    [SerializedField] public String artworkDate; //Use this field for MONTH - DAY - YEAR (As seen on Website)
    [SerializedField] public List<String> artworkURLs; //Temporary, may change this logic later. Used for multiple urls for artwork
    [SerializedField] public int artworkCount; //Use this field to display the number of pieces of artwork
    [SerializedField] public int artworkID; //Use this field to track the ID number of the artwork (This is for our use)
}
