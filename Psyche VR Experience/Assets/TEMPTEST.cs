using PsycheDB;
using SQLite4Unity3d;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TEMPTEST : MonoBehaviour
{
    public Texture2D texture;
    public string key = "Assets/Artwork/1020220650717530004/Psyche_Inspired_25-26_AWangInspiration_1_11-07-25.png";
    AsyncOperationHandle<Texture2D> opHandle;
    public Material material;

    public TextMeshProUGUI text;

    float timer = 0;

    private void Start()
    {
        material.mainTexture = null;
        LoadByKey();
    }

    private void Update()
    {
        if (timer > 1)
        {
            LoadByKey();
            timer = 0;
        }
        timer += Time.deltaTime;
    }

    public class ProjectArtistResult
    {
        public int project_id { get; set; }
        public string title { get; set; }
        public string artist_name { get; set; }
        public string artist_major { get; set; }
    }

    public void LoadByKey()
    {
        text.text = "";

        //try {
            ArtworkData data = (PsycheDBMiddleware.LoadRandomArtworkData(1))[0];

            text.text += data.artistName + "\n";

            text.text += data.artworkURLs[0] + "\n";

            opHandle = Addressables.LoadAssetAsync<Texture2D>(data.artworkURLs[0].Replace("\\", "/"));
            opHandle.Completed += FinishLoad;
        //}
        //catch (Exception e)
        //{
        //    opHandle = Addressables.LoadAssetAsync<Texture2D>(key);
        //    opHandle.Completed += FinishLoad;
        //    text.text = $"Tried path at {PsycheDB.DatabaseManager.DatabasePath} but got ERROR:\n{e.Message}\n{e.Data}\n{e.Source}\n{e.InnerException}\n{e.TargetSite}\n{e.StackTrace}";
        //}
    }

    void FinishLoad(AsyncOperationHandle<Texture2D> opHandle)
    {
        if (opHandle.Status == AsyncOperationStatus.Succeeded)
        {
            texture = opHandle.Result;
        }

        material.mainTexture = texture;
    }
}
