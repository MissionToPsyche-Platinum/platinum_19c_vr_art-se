using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ArtworkData", menuName = "Scriptable Objects/ArtworkData")]
public class ArtworkData : ScriptableObject
{
    [SerializeField] public String artworkName; //Use this field for the artwork's name
    [SerializeField] public String artistName; //Use this field for the artist's name
    [SerializeField] public String artworkDescription; //Use this field for the artwork description
    [SerializeField] public String genre; //Use this field for the artwork genre
    [SerializeField] public String artistInformation; //Use this field for any extra information about the artist
    [SerializeField] public String artworkDate; //Use this field for MONTH - DAY - YEAR (As seen on Website)
    [SerializeField] public List<String> artworkURLs; //Temporary, may change this logic later. Used for multiple urls for artwork
    [SerializeField] public int artworkCount; //Use this field to display the number of pieces of artwork
    [SerializeField] public int artworkID; //Use this field to track the ID number of the artwork (This is for our use)
}
