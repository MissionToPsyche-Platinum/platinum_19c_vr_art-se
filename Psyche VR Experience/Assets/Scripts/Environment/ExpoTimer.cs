using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class ExpoTimer : MonoBehaviour
{
    [Header("Timer Length")]
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;

    [SerializeField] private AudioSource warningAudio;

    [SerializeField] private GameObject screenBlackSquare;

    [SerializeField] private XRInputButtonReader resetEvent;

    // use these to actually run the timer
    private float secondsLeft;
    private bool timerRunning = false;

    // only do the oneMinuteLeft and timerDone "events" once
    private bool warningHappened = false;
    private bool timerDoneHappened = false;

    void Start()
    {
        resetTimer();
    }

    void Update()
    {
        if (!timerRunning && timerDoneHappened)
        {
            // if timer is not running, fade in black screen
            Color squareColor = screenBlackSquare.GetComponent<SpriteRenderer>().color;
            if (squareColor.a < 1)
            {
                screenBlackSquare.GetComponent<SpriteRenderer>().color = new Color(.1f, .1f, .1f, squareColor.a + 5 * Time.deltaTime);
            }

            if (resetEvent.ReadIsPerformed())
            {
                resetToLaunchRoom();
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
        
        // reset screen overlay
        screenBlackSquare.GetComponent<SpriteRenderer>().color = new Color(.1f, .1f, .1f, 0);
    }

    public void startTimer()
    {
        timerRunning = true;

        Debug.Log("Timer Started");
    }

    // TODO: RESET the event by going back to the launch room (rest of task 214)
    private void resetToLaunchRoom()
    {
        return;
    }
}
