using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ArtworkData", menuName = "Scriptable Objects/ArtworkData")]
public class ArtworkData : ScriptableObject
{
    [SerializeField] public String artworkName;                 //artwork's name
    [SerializeField] public String artistName;                  //artist's name
    [SerializeField] public String artworkDescription;          //artwork description
    [SerializeField] public String genre;                       //artwork genre/medium
    [SerializeField] public String artistMajor;                 //artist major
    [SerializeField] public String artworkDate;                 //MONTH - DAY - YEAR (As seen on Website)
    [SerializeField] public List<String> artworkURLs;           //urls for artwork
    [SerializeField] public int artworkCount;                   //number of pieces of artwork
    [SerializeField] public int artworkID;                      //ID number of the artwork (This is for our use)
}
