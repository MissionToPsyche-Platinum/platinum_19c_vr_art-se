using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class LocomotionSettingsManager : MonoBehaviour
{
    [SerializeField] private XRInputButtonReader menuButton;
    [SerializeField] private XRInputValueReader<Vector2> menuInteraction;

    [SerializeField] private GameObject LocomotionSettingsMenu;

    [SerializeField, Tooltip("The settings that can be selected and interacted with")] private List<SelectableSetting> selectables;

    private bool menuOpen = false;
    private int selectedIndex = 0;

    private float pressThreshold = 0.8f;
    private bool menuInteracted = false;        // need this to use the joystick as a button

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

    public void adjustVolume(float val)
    {
        GlobalSettings.MASTER_VOLUME = val;
    }
}
