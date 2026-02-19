using UnityEngine;
using UnityEngine.SceneManagement;

public class LaunchRoomManager : MonoBehaviour
{
    [SerializeField] private ExpoTimer expoTimer;
    [SerializeField] private MuseumManager museumManager;
    [SerializeField] private Transform playerTransform;

    [Tooltip("Where the player will spawn")]
    [SerializeField] private Vector3 playerSpawnPosition;

    private bool museumGeneratedAtLeastOnce = false;

    private bool InMuseum = false;

    public async void startExpoExperience()
    {
        if (InMuseum)
        {
            return;
        }

        InMuseum = true;

        // generate the museum if we have the generate every time setting or if it hasn't been generated before
        if (!museumGeneratedAtLeastOnce)
        {
            await museumManager.GenerateMuseum(ExpoSettings.ART_PIECE_COUNT);
            museumGeneratedAtLeastOnce = true;
        }
        // else, just switch the art
        else
        {
            await museumManager.AssignArt(ExpoSettings.ART_PIECE_COUNT);
        }

        expoTimer.startTimer();
        teleportPlayerToMuseum();
    }

    // stolen code from SpawnController.cs >:)
    private void teleportPlayerToMuseum()
    {
        Vector3 spawnPosition = museumManager.firstRoom.position;
        if (spawnPosition != null)
        {
            Vector3 playerSpawnPosition = new Vector3(spawnPosition.x, 0, spawnPosition.z);
            playerTransform.position = playerSpawnPosition;
        }
    }

    public void ResetExpo()
    {
        // if we generate every time, simply reload the scene
        if (ExpoSettings.REGENERATE_MUSEUM)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        // else, bring the player back to the launch room 
        else
        {
            playerTransform.position = playerSpawnPosition;
        }

        InMuseum = false;
    }
}
