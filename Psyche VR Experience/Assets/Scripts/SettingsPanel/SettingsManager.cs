using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SettingsManager : MonoBehaviour
{

    [SerializeField] private XRInputButtonReader menuButton;
    [SerializeField] private XRInputValueReader<Vector2> menuInteraction;

    [SerializeField] private GameObject LocomotionSettingsMenu;

    [SerializeField, Tooltip("The settings that can be selected and interacted with")] private List<SelectableSetting> selectables;

    private bool menuOpen = false;
    private int selectedIndex = 0;

    private float pressThreshold = 0.8f;
    private bool menuInteracted = false;        // need this to use the joystick as a button

    //event to let video audio sources know that the volume has changed
    public static UnityEvent m_VideoVolumeChanged = new UnityEvent();
    //event to let text know to change size
    public static UnityEvent m_TextSizeChanged = new UnityEvent();

    private void OnValidate()
    {
        RefreshSelectables();
    }
    private void Awake()
    {
        RefreshSelectables();
        selectables[selectedIndex].enabled = true;
    }

    private void RefreshSelectables()
    {
        selectables.Clear();

        foreach (Transform child in LocomotionSettingsMenu.transform)
        {
            SelectableSetting setting = child.GetComponent<SelectableSetting>();
            if (setting != null)
            {
                selectables.Add(setting);
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (menuButton.ReadWasPerformedThisFrame()) {
            OpenOrCloseMenu(); 
        }

        if (menuOpen)
        {
            float y = menuInteraction.ReadValue().y;

            // up
            if (!menuInteracted && y > pressThreshold)
            {
                upSelection();
            }
            // down
            else if (!menuInteracted && y < -pressThreshold)
            {
                downSelection();
            }

            if (menuInteracted && Mathf.Abs(y) < pressThreshold)
            {
                menuInteracted = false;
            }
        }
    }

    private void OpenOrCloseMenu()
    {
        LocomotionSettingsMenu.SetActive(!menuOpen);
        menuOpen = !menuOpen;
    }

    private void downSelection()
    {
        if (selectedIndex >= selectables.Count - 1)
        {
            return;
        }
        menuInteracted = true;

        selectables[selectedIndex].enabled = false;
        selectedIndex++;
        selectables[selectedIndex].enabled = true;
    }

    private void upSelection()
    {
        if (selectedIndex <= 0)
        {
            return;
        }
        menuInteracted = true;

        selectables[selectedIndex].enabled = false;
        selectedIndex--;
        selectables[selectedIndex].enabled = true;
    }

    public void LocomotionToggleOn()
    {
        LocomotionSettings.LOCOMOTION_MODE = LocomotionSettings.LocomotionMode.CONTINUOUS;
    }

    public void LocomotionToggleOff()
    {
        LocomotionSettings.LOCOMOTION_MODE = LocomotionSettings.LocomotionMode.TELEPORT;
    }
    
    public void LocomotionVignetteToggleOn()
    {
        LocomotionSettings.CONTINUOUS_VIGNETTE = true;
    }

    public void LocomotionVignetteToggleOff()
    {
        LocomotionSettings.CONTINUOUS_VIGNETTE = false;
    }

    public void AdjustMasterVolume(float val)
    {
        GlobalSettings.MASTER_VOLUME = val;
        m_VideoVolumeChanged?.Invoke();
    }

    public void AdjustMusicVolume(float val)
    {
        GlobalSettings.MUSIC_VOLUME = val;
    }

    public void TextSizeValueChange(string newValue)
    {
        string input = newValue;
        input = input.Substring(0, input.Length - 1);

        float result = float.Parse(input);
        GlobalSettings.TEXT_SIZE_MULTIPLIER = result;
        m_TextSizeChanged?.Invoke();
    }

    public void AutoIterateOn()
    {
        GlobalSettings.AUTO_ITERATE_ON = true;
    }

    public void AutoIterateOff()
    {
        GlobalSettings.AUTO_ITERATE_ON = false;
    }

    public void AdjustTeleportFade(float val)
    {
        if (val > 0f)
        {
            LocomotionSettings.TELEPORT_FADE_TO_BLACK = true;
        }
        else
        {
            LocomotionSettings.TELEPORT_FADE_TO_BLACK = false;
        }

        // adjust this to change the value of how high the fade and be
        float maxFadeRange = 2f;
        float newFadeTime = maxFadeRange * val;

        LocomotionSettings.TELEPORT_FADE_TIME = newFadeTime;
        LocomotionSettings.TELEPORT_FADE_WAIT = newFadeTime;
    }

    public void InteractionSizeValueChange(string newValue)
    {
        string input = newValue;
        input = input.Substring(0, input.Length - 1);

        float result = float.Parse(input);
        GlobalSettings.INTERACTION_SIZE_MULTIPLER = result;
    }

    public void SkyboxRotationToggleOn()
    {
        GlobalSettings.SKYBOX_ROTATION_ON = 1f;
    }

    public void SkyboxRotationToggleOff()
    {
        GlobalSettings.SKYBOX_ROTATION_ON = 0f;
    }
}
