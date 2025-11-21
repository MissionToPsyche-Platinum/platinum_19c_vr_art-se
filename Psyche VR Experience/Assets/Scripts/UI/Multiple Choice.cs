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

    private int currIndex;

    protected void OnValidate()
    {
        currIndex = startingIndex;
        SetTextToIndex(currIndex);

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
        SetTextToIndex(currIndex);
        buttonPressed();
    }

    public void RightPressed()
    {
        if (currIndex >= options.Count - 1)
        {
            return;
        }

        currIndex += 1;
        SetTextToIndex(currIndex);
        buttonPressed();
    }

    private void SetTextToIndex(int index)
    {
        textBox.text = options[index];
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
}
