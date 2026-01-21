using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ExpoTimer : MonoBehaviour
{
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;
    [SerializeField] private AudioSource warningAudio;

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
        if (!timerRunning)
        {
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

    // TODO: Implement this as part of US 196 Task 205
    public void timerDone()
    {
        Debug.Log("Expo Timer Over");

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
}

    public void startTimer()
    {
        timerRunning = true;

        Debug.Log("Timer Started");
    }
}
