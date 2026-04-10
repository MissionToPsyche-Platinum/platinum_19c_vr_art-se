using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MultipleChoice : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI textBox;
    
    [Header("Options (Order Matters!)")]
    [SerializeField] private List<string> options;

    [Header("Starting Option (zero-based)")]
    [SerializeField] private int startingIndex;

    [Header("Arrows")]
    [SerializeField] private GameObject leftArrow;
    [SerializeField] private GameObject rightArrow;

    [Header("On Change - passes the string that the option is set to")]
    [SerializeField] private UnityEvent<string> onChange;

    [SerializeField] private MusicManager musicManager;
    private int currIndex;
    [Header("Only check this for the Music Change Multiple Choice")]
    [SerializeField] private bool needsMusicManaged = false;
    
    protected void OnValidate()
    {
        currIndex = startingIndex;
        SetTextToIndex(currIndex);
        
        if (needsMusicManaged == true)
        {
            GetTextFromMusicManager();
        }

        if (currIndex == 0)
        {
            leftArrow.SetActive(false);
        }
        else
        {
            leftArrow.SetActive(true);
        }
        if (currIndex >= options.Count - 1)
        {
            rightArrow.SetActive(false);
        }
        else
        {
            rightArrow.SetActive(true);
        }
    }

    protected void Awake()
    {
        OnValidate();
    }

    public void LeftPressed()
    {
        if (currIndex == 0)
        {
            return;
        }

        currIndex -= 1;
        if (needsMusicManaged == true)
        {
            GetTextFromMusicManager();
            musicManager.PlayNextClip();
        } 
        else
        {
            SetTextToIndex(currIndex);    
        }
        buttonPressed();
    }

    public void RightPressed()
    {
        if (currIndex >= options.Count - 1)
        {
            return;
        }

        currIndex += 1;
        if (needsMusicManaged == true)
        {
            GetTextFromMusicManager();
            musicManager.PlayPreviousClip();
        } 
        else
        {
            SetTextToIndex(currIndex);    
        }
        buttonPressed();
    }

    private void SetTextToIndex(int index)
    {
        textBox.text = options[index];
    }

    private void GetTextFromMusicManager()
    {
        textBox.text = musicManager.getSongName();
    }

    private void buttonPressed()
    {
        if (currIndex == 0)
        {
            leftArrow.SetActive(false);
        }
        else
        {
            leftArrow.SetActive(true);
        }
        if (currIndex >= options.Count - 1)
        {
            rightArrow.SetActive(false);
        }
        else
        {
            rightArrow.SetActive(true);
        }

        onChange?.Invoke(options[currIndex]);
    }

    public void ResetChoice()
    {
        OnValidate();
    }
}
