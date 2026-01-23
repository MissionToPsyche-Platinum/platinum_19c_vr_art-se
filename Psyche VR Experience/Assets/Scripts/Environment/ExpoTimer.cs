using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExpoTimer : MonoBehaviour
{
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;
    [SerializeField] private AudioSource warningAudio;
    [SerializeField] public Image blackScreen;

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
        // if the timer is finished and the screen is not fully black, darken the screen black cover
        if (timerDoneHappened && blackScreen.color.a != 1.0f)
        {
            Color color = blackScreen.color;
            color.a += Time.deltaTime;
            color.a = Mathf.Clamp(color.a, 0, 1);
            blackScreen.color = color;
        }
        
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
        
        // remove the black screen
        Color color = blackScreen.color;
        color.a = 0;
        blackScreen.color =  color;
}

    public void startTimer()
    {
        timerRunning = true;

        Debug.Log("Timer Started");
    }
}
