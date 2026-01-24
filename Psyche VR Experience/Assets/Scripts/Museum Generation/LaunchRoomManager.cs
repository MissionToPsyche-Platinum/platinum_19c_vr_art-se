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

    public async void startExpoExperience()
    {
        // generate the museum if we have the generate every time setting or if it hasn't been generated before
        if (true || !museumGeneratedAtLeastOnce) // TODO US89: replace true with: ExpoSettings.GenerateEveryTime or the equivalent
        {
            await museumManager.GenerateMuseum(50); // TODO US89: replace 50 with: ExpoSettings.NumArtPieces or the equivalent
            museumGeneratedAtLeastOnce = true;
        }
        // else, just switch the art
        else
        {
            await museumManager.AssignArt(50); // TODO US89: replace 50 with: ExpoSettings.NumArtPieces or the equivalent
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
            Vector3 playerSpawnPosition = new Vector3(spawnPosition.x, 1f, spawnPosition.z);
            playerTransform.position = playerSpawnPosition;
        }
    }

    public void ResetExpo()
    {
        // if we generate every time, simply reload the scene
        if (true)   // TODO US89: replace true with: ExpoSettings.GenerateEveryTime or the equivalent
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        // else, bring the player back to the launch room 
        else
        {
            playerTransform.position = playerSpawnPosition;
        }
    }
}
