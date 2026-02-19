using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

/* Credit to @zbarlow FrameController for some of the base setup functionality*/

public class TextBoxController : MonoBehaviour
{
    public void Awake()
    {
        SettingsManager.m_TextSizeChanged.AddListener(ChangeTextSize);
        ChangeTextSize();
    }

    public void ChangeTextSize()
    {
        //Transform parent = transform.parent; 
        //transform.SetParent(null);
        transform.localScale = Vector3.one * GlobalSettings.TEXT_SIZE_MULTIPLIER;
        //transform.SetParent(parent);
    }
}
