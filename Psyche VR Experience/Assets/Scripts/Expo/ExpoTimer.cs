using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using System.Collections;

public class ExpoTimer : MonoBehaviour
{
    [Header("Timer Length (Overridden By Expo Settings)")]
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;

    [SerializeField] private AudioSource warningAudio;

    [SerializeField] private Image blackScreen;
    [SerializeField] private TextMeshProUGUI expoEndText;
    [SerializeField] private TextMeshProUGUI expoWarningText;

    [SerializeField] private InputActionReference resetEvent;
    [Tooltip("Attach the launch room manager hear to handle when the reset is triggered.")]
    [SerializeField] private LaunchRoomManager launchRoomManager;
    
    [Header("Settings to reset")]
    [SerializeField] private ToggleSwitch locomotionToggle;
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private MultipleChoice textSizeChoice;
    [SerializeField] private ToggleSwitch vignetteToggle;
    [SerializeField] private Slider teleportFadeSlider;
    [SerializeField] private MultipleChoice buttonSizeChoice;
    [SerializeField] private ToggleSwitch rotationToggle;

    // use these to actually run the timer
    private float secondsLeft;
    private bool timerRunning = false;

    // only do the oneMinuteLeft and timerDone "events" once
    private bool warningHappened = false;
    private bool timerDoneHappened = false;
    private bool timerWarningFinished = false;
    
    // track when the timer warning text has fully appeared
    private bool timerWarningIncreasing = true;

    void Start()
    {
        startingTimerSeconds = ExpoSettings.MUSEUM_TOUR_DURATION;

        resetEvent.action.performed += resetEventHappened;
        resetTimer();
        
        expoWarningText.text = ""; // TODO: Decide whether to fully remove or replace somehow
        expoEndText.text = "Museum Experience has Ended.\nHand the headset back to the technician.\nHold down both triggers for 5 seconds to reset.";
    }

    void Update()
    {
        if (!timerRunning && timerDoneHappened)
        {
            // if the screen is not fully black, darken the screen black cover
            if (blackScreen.color.a != 1.0f)
            {
                // screen black
                Color screenColor = blackScreen.color;
                screenColor.a += Time.deltaTime;
                screenColor.a = Mathf.Clamp(screenColor.a, 0, 1);
                blackScreen.color = screenColor;
                
                // instruction text
                Color textColor = expoEndText.color;
                textColor.a +=  Time.deltaTime;
                textColor.a = Mathf.Clamp(textColor.a, 0, 1);
                expoEndText.color = textColor;
            }

            return;
        }

        // if the timer warning went off and the warning text has not finished appearing and disappearing, change it
        if (warningHappened && !timerWarningFinished)
        {
            Color warningColor = expoWarningText.color;
            
            // if the opacity is still increasing, increase it more
            if (timerWarningIncreasing)
            {
                warningColor.a += Time.deltaTime;
                warningColor.a = Mathf.Clamp(warningColor.a, 0, 1);
                expoWarningText.color = warningColor;
                // if the opacity has reached 1.0, it's time to decrease
                if(warningColor.a == 1.0f)
                    timerWarningIncreasing = false;
            }
            // if the opacity is decreasing, decrease it more
            else
            {
                warningColor.a -= Time.deltaTime;
                warningColor.a = Mathf.Clamp(warningColor.a, 0, 1);
                expoWarningText.color = warningColor;
                // if the opacity has reached 0.0, it's time to stop
                if (warningColor.a == 0f)
                    timerWarningFinished = true;
            }
        }

        if (timerRunning)
            secondsLeft -= Time.deltaTime;
        else
            return;

        // fire off methods if time is up
        if (secondsLeft < warningSeconds && !warningHappened)
        {
            warning();
        }
        else if (secondsLeft < 0f && !timerDoneHappened)
        {
            timerDone();
        }
    }

    public void warning()
    {
        Debug.Log(warningSeconds + " seconds left on the expo timer!");
        warningHappened = true;
        StartCoroutine(PlayWarningNTimes(4));
    }

    public void timerDone()
    {
        Debug.Log("Expo Timer Done!");

        timerRunning = false;
        timerDoneHappened = true;
        
        Debug.Log("Reset time");
        
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
        masterVolSlider.normalizedValue = 1;
        musicVolSlider.normalizedValue = 1;
        textSizeChoice.ResetChoice();
        vignetteToggle.Reset();
        teleportFadeSlider.normalizedValue = 0.5f;
        buttonSizeChoice.ResetChoice();
        rotationToggle.Reset();
    }

    public float getSecondsLeft()
    {
        return secondsLeft;
    }

    public void setStartingSeconds(float seconds)
    {
        startingTimerSeconds = seconds;
    }

    public void setWarningSeconds(float seconds)
    {
        warningSeconds = seconds;
    }

    public void resetTimer()
    {
        secondsLeft = startingTimerSeconds;
        timerRunning = false;
        warningHappened = false;
        timerDoneHappened = false;
        timerWarningFinished = false;
        timerWarningIncreasing = true;
        
        // remove the black screen
        Color screenColor = blackScreen.color;
        screenColor.a = 0;
        blackScreen.color =  screenColor;
        
        // remove the ending text
        Color textColor = expoEndText.color;
        textColor.a = 0;
        expoEndText.color = textColor;
}

    public void startTimer()
    {
        timerRunning = true;

        Debug.Log("Expo Timer Started");
    }

    private void resetEventHappened(InputAction.CallbackContext ctx)
    {
        Debug.Log("Reset Event Performed!");

        if (timerRunning || timerDoneHappened)
        {
            resetTimer();
            launchRoomManager.ResetExpo();
        }
    }

    IEnumerator PlayWarningNTimes(int num)
    {
        Debug.Log("Here!");
        for (int i = 0; i < num; i++)
        {
            Debug.Log("Here!");
            warningAudio.Play();
            yield return new WaitForSeconds(warningAudio.clip.length);
        }
    }
}
