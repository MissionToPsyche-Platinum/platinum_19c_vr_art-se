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

    private void OpenOrCloseMenu()
    {
        LocomotionSettingsMenu.SetActive(!menuOpen);
        menuOpen = !menuOpen;
    }
}
