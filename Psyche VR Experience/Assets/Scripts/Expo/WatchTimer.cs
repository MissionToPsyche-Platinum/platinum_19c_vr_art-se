using TMPro;
using UnityEngine;

public class WatchTimer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private ExpoTimer expoTimer;
    [SerializeField] private TextMeshProUGUI watchText;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float seconds = expoTimer.getSecondsLeft();
        if (seconds <= 0)
        {
            seconds = 0f;
        }

        watchText.text = ConvertSecondsToTime((int) seconds);
    }

    //Yes this is a duplicate from Expo Settings. Just roll with it, it makes more sense than linking it together all weirdly.
    public string ConvertSecondsToTime(int duration) //Converts time in seconds to a 00:00 format
    {
        int minutes = duration / 60;
        int seconds = duration % 60;
        string timeFormatted = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        return timeFormatted;
    }
}
