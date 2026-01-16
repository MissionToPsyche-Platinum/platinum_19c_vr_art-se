using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SelectableSetting : MonoBehaviour
{
    // images that will change color when selected
    [SerializeField] private List<Image> selectedImages;
    [SerializeField] private Color selectedColor = new Color(0, 255, 206);
    [SerializeField] private Color defaultColor = Color.white;

    private void OnEnable()
    {
        foreach (Image image in selectedImages)
        {
            image.color = selectedColor;
        }
    }

    private void OnDisable()
    {
        foreach (Image image in selectedImages)
        {
            image.color = defaultColor;
        }
    }

    private void Update()
    {
        // listen for inputs here
    }
}
