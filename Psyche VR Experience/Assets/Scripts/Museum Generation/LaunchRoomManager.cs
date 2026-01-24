using UnityEngine;

public class LaunchRoomManager : MonoBehaviour
{
    [SerializeField] private MuseumManager museumManager;
    [SerializeField] private Transform playerTransform;

    private bool museumGeneratedAtLeastOnce = false;

    public async void startExpoExperience()
    {
        // generate the museum if we have the generate every time setting or if it hasn't been generated before
        if (!museumGeneratedAtLeastOnce || true) // TODO US89: replace true with: ExpoSettings.GenerateEveryTime or the equivalent
        {
            await museumManager.GenerateMuseum(50); // TODO US89: replace 50 with: ExpoSettings.NumArtPieces or the equivalent
            museumGeneratedAtLeastOnce = true;
        }
        // else, just switch the art
        else
        {
            await museumManager.AssignArt(50); // TODO US89: replace 50 with: ExpoSettings.NumArtPieces or the equivalent
        }

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
}
