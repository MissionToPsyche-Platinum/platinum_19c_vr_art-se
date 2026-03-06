using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LaunchRoomManager : MonoBehaviour
{
    [SerializeField] private ExpoTimer expoTimer;
    [SerializeField] private MuseumManager museumManager;
    [SerializeField] private Transform playerTransform;

    [Tooltip("Where the player will spawn")]
    [SerializeField] private Vector3 playerSpawnPosition;
    
    [Header("Settings Controls")]
    [SerializeField] private ToggleSwitch locomotionToggle;
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private MultipleChoice textSizeChoice;
    [SerializeField] private ToggleSwitch vignetteToggle;
    [SerializeField] private Slider teleportFadeSlider;
    [SerializeField] private MultipleChoice buttonSizeChoice;
    [SerializeField] private ToggleSwitch rotationToggle;

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
        
        // reset all settings back to defaults
        LocomotionSettings.LOCOMOTION_MODE = LocomotionSettings.LocomotionMode.TELEPORT;
        GlobalSettings.MASTER_VOLUME = 1;
        SettingsManager.m_VideoVolumeChanged?.Invoke();
        GlobalSettings.MUSIC_VOLUME = 1;
        GlobalSettings.TEXT_SIZE_MULTIPLIER = 1;
        SettingsManager.m_TextSizeChanged.Invoke();
        LocomotionSettings.CONTINUOUS_VIGNETTE = false;
        LocomotionSettings.TELEPORT_FADE_TIME = 0.5f;
        LocomotionSettings.TELEPORT_FADE_WAIT = 0.5f;
        GlobalSettings.INTERACTION_SIZE_MULTIPLER = 1;
        SettingsManager.m_ButtonSizeChanged.Invoke();
        GlobalSettings.SKYBOX_ROTATION_ON = 1f;
        
        // set all menu visuals back to defaults
        locomotionToggle.Reset();
        masterVolSlider.value = 1;
        musicVolSlider.value = 1;
        textSizeChoice.ResetChoice();
        vignetteToggle.Reset();
        teleportFadeSlider.value = 0.5f;
        buttonSizeChoice.ResetChoice();
        rotationToggle.Reset();

        InMuseum = false;
    }
}
