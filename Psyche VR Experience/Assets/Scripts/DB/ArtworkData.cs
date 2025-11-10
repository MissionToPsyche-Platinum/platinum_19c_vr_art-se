using UnityEngine;
using System;
using System.IO;
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
    [SerializeField] public long artworkID;                      //ID number of the artwork (This is for our use)

    
    // Cache to avoid reloading files multiple times
    private List<Texture2D> _loadedTextures = null;

    // loads textures from the paths
    // keeps the 'data' within the SO
    // until requested by the framecontroller
    // paths are expected to be project relative("Assets/Artwork/.../file.png   
    public List<Texture2D> LoadTextures()
    {
        // return cached if already loaded
        if (_loadedTextures != null)
            return _loadedTextures;

        _loadedTextures = new List<Texture2D>();
        if (artworkURLs == null || artworkURLs.Count == 0)
            return _loadedTextures;

        foreach (var url in artworkURLs)
        {
            if (string.IsNullOrEmpty(url)) continue;

            // only load images (skip video/audio)
            // @TODO 
            // get video working.(conversion of video to 2d texture format)
            var ext = Path.GetExtension(url).ToLowerInvariant();
            if (!String.Equals(ext,".png") && !String.Equals(ext,".jpg") && !String.Equals(ext,".jpeg") && !String.Equals(ext,".tga") && !String.Equals(ext,".bmp"))
                continue;

            // convert "Assets/Artwork/..." to a full system path
            string fullPath = Path.Combine(
                Application.dataPath,
                url.Substring("Assets/".Length)
            );

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"ArtworkData: File not found at {fullPath}");
                continue;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = Path.GetFileNameWithoutExtension(fullPath);
                    _loadedTextures.Add(tex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ArtworkData: Failed loading {fullPath} -> {ex}");
            }
        }

        return _loadedTextures;
    }

    // clears cached textures (for use after despawning exhibit or unloading rooms(future functionality))
    public void ClearLoadedTextures()
    {
        if (_loadedTextures != null)
        {
            foreach (var tex in _loadedTextures)
                Destroy(tex);

            _loadedTextures = null;
        }
    }
}
