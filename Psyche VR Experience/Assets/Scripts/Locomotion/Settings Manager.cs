using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class LocomotionSettingsManager : MonoBehaviour
{

    [SerializeField] private XRInputButtonReader menuButton;
    [SerializeField] private XRInputButtonReader menuSelect; // TODO: DELETE as part of task 163
    [SerializeField] private XRInputValueReader<Vector2> menuInteraction;

    [SerializeField] private List<SelectableSetting> selectables;

    [SerializeField] private GameObject LocomotionSettingsMenu;
    [SerializeField] private Toggle LocomotionToggle;

    private bool menuOpen = false;
    private int selectedIndex = 0;

    private void OnValidate()
    {
        foreach (Transform child in LocomotionSettingsMenu.transform)
        {
            SelectableSetting setting = child.GetComponent<SelectableSetting>();
            if (setting != null)
            {
                selectables.Add(setting);
            }
        }

    }
    private void Awake()
    {
        OnValidate();
    }

    // Update is called once per frame
    void Update()
    {
        if (menuButton.ReadWasPerformedThisFrame()) {
            OpenOrCloseMenu(); 
        }

        if (menuOpen && menuInteraction.inputAction.triggered)
        {
            float y = menuInteraction.ReadValue().y;

            // up
            if (y > 0)
            {
                upSelection();
            }
            // down
            if (y < 0)
            {
                downSelection();
            }
        }

        // TODO: DELETE as part of task 163
        if (menuOpen && menuSelect.ReadWasPerformedThisFrame()) {
            bool curr = LocomotionToggle.isOn;
            LocomotionToggle.isOn = !curr;
        }
    }

    private void OpenOrCloseMenu()
    {
        LocomotionSettingsMenu.SetActive(!menuOpen);
        menuOpen = !menuOpen;
    }

    public void downSelection()
    {
        if (selectedIndex >= selectables.Count - 1)
        {
            return;
        }

        selectables[selectedIndex].enabled = false;
        selectedIndex++;
        selectables[selectedIndex].enabled = true;
    }

    public void upSelection()
    {
        if (selectedIndex <= 0)
        {
            return;
        }

        selectables[selectedIndex].enabled = false;
        selectedIndex--;
        selectables[selectedIndex].enabled = true;
    }

    // TODO: DELETE as part of task 163
    public void ToggleLocomotion()
    {
        if (LocomotionSettings.LOCOMOTION_MODE == LocomotionSettings.LocomotionMode.TELEPORT)
        {
            LocomotionSettings.LOCOMOTION_MODE = LocomotionSettings.LocomotionMode.CONTINUOUS;
        }
        else
        {
            LocomotionSettings.LOCOMOTION_MODE = LocomotionSettings.LocomotionMode.TELEPORT;
        }
    }
}
