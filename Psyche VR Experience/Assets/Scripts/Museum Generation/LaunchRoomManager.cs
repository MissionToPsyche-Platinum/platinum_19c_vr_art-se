using Unity.VisualScripting;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LaunchRoomManager : MonoBehaviour
{
    [SerializeField] private ExpoTimer expoTimer;
    [SerializeField] private MuseumManager museumManager;
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform domeTransform;

    [Tooltip("Where the player will spawn")]
    [SerializeField] private Vector3 playerSpawnPosition;

    private bool museumGeneratedAtLeastOnce = false;
    private bool museumPrepared = false;
    private bool InMuseum = false;
    private bool preparationInProgress = false;

    // survives scene reload because static
    public static bool PrepareMuseumAfterReload = false;

    public async void Start()
    {
        // if previous scene requested a preload, do it now
        if (PrepareMuseumAfterReload)
        {
            PrepareMuseumAfterReload = false;
            await PrepareNextMuseum();
        }
    }


    public async void startExpoExperience()
    {
        if (InMuseum || preparationInProgress)
        {
            return;
        }

        InMuseum = true;

        // generate the museum if we have the generate every time setting or if it hasn't been generated before
        if (!museumPrepared)
        {
            await PrepareNextMuseum();
        }

        expoTimer.startTimer();
        teleportPlayerToMuseum();
    }

    public async Awaitable PrepareNextMuseum()
    {
        if (preparationInProgress)
            return;

        preparationInProgress = true;
        museumPrepared = false;

        // first ever generation
        if (!museumGeneratedAtLeastOnce)
        {
            await museumManager.GenerateMuseum(ExpoSettings.ART_PIECE_COUNT);
            museumGeneratedAtLeastOnce = true;
        }
        else
        {
            // regenerate layout if setting says so, otherwise just refresh art
            if (ExpoSettings.REGENERATE_MUSEUM)
            {
                await museumManager.GenerateMuseum(ExpoSettings.ART_PIECE_COUNT);
            }
            else
            {
                await museumManager.AssignArt(ExpoSettings.ART_PIECE_COUNT);
            }
        }

        museumPrepared = true;
        preparationInProgress = false;
    }
    // stolen code from SpawnController.cs >:)
    private void teleportPlayerToMuseum()
    {
        Vector3 spawnPosition = museumManager.firstRoom.position;
        if (spawnPosition != null)
        {
            Vector3 playerSpawnPosition = new Vector3(spawnPosition.x, 0, spawnPosition.z);
            playerTransform.position = playerSpawnPosition;
            
            domeTransform.position = new Vector3(spawnPosition.x, 0, spawnPosition.z);
            SetMusicPlaylistActive();
            musicManager.setInMenu(false);
        }
    }

    public void ReloadSceneAndPrepareMuseum()
    {
        PrepareMuseumAfterReload = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Hello World");
        domeTransform.position = new Vector3(0, -50, 0);
        musicManager.PlayTannernetSpace();
        musicManager.setInMenu(true);
    }

    public void SetMusicPlaylistActive()
    {
        musicManager.StartMuseumPlaylist();
    }
}
