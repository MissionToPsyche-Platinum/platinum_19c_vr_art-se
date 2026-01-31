using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.InputSystem;

public class ExpoTimer : MonoBehaviour
{
    [Header("Timer Length (Overridden By Expo Settings)")]
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;

    [SerializeField] private AudioSource warningAudio;

    [SerializeField] private Image blackScreen;
    [SerializeField] private TextMeshProUGUI expoEndText;

    [SerializeField] private InputActionReference resetEvent;
    [Tooltip("Attach the launch room manager hear to handle when the reset is triggered.")]
    [SerializeField] private LaunchRoomManager launchRoomManager;

    // use these to actually run the timer
    private float secondsLeft;
    private bool timerRunning = false;

    // only do the oneMinuteLeft and timerDone "events" once
    private bool warningHappened = false;
    private bool timerDoneHappened = false;

    void Start()
    {
        startingTimerSeconds = ExpoSettings.MUSEUM_TOUR_DURATION;

        resetEvent.action.performed += resetEventHappened;
        resetTimer();
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
        warningAudio.Play();

        warningHappened = true;
    }

    public void timerDone()
    {
        Debug.Log("Expo Timer Done!");

        timerRunning = false;
        timerDoneHappened = true;
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

        if (!timerRunning && timerDoneHappened)
        {
            resetTimer();
            launchRoomManager.ResetExpo();
        }
    }
}
