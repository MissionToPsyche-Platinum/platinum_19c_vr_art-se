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

        watchText.text = ((int) seconds).ToString();
    }
}
