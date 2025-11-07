using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class LocomotionSettingsManager : MonoBehaviour
{
    [SerializeField]
    XRInputButtonReader menuButton;

    public GameObject LocomotionSettingsMenu;

    private bool menuOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (menuButton.ReadValue() == 1) { OpenOrCloseMenu(); }
    }

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

    private void OpenOrCloseMenu()
    {
        LocomotionSettingsMenu.SetActive(!menuOpen);
        menuOpen = !menuOpen;
    }
}
