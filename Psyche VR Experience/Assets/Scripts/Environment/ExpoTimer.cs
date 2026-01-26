using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.InputSystem;

public class ExpoTimer : MonoBehaviour
{
    [Header("Timer Length")]
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;

    [SerializeField] private AudioSource warningAudio;

    [SerializeField] private Image blackScreen;

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
                Color color = blackScreen.color;
                color.a += Time.deltaTime;
                color.a = Mathf.Clamp(color.a, 0, 1);
                blackScreen.color = color;
            }

            return;
        }

        secondsLeft -= Time.deltaTime;

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
        Color color = blackScreen.color;
        color.a = 0;
        blackScreen.color =  color;
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
