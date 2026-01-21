using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExpoTimer : MonoBehaviour
{
    [SerializeField] private float startingTimerSeconds;
    [SerializeField] private float warningSeconds;
    [SerializeField] private AudioSource warningAudio;
    [SerializeField] private GameObject screenBlackSquare;

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
            // if timer is not running, fade in black screen
            Color squareColor = screenBlackSquare.GetComponent<SpriteRenderer>().color;
            if (squareColor.a < 1)
            {
                screenBlackSquare.GetComponent<SpriteRenderer>().color = new Color(.1f, .1f, .1f, squareColor.a + 5 * Time.deltaTime);
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
}
