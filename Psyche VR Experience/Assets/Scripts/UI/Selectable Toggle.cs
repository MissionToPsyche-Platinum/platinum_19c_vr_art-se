using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SelectableToggle : SelectableSetting
{
    [SerializeField] private XRInputButtonReader selectButton;
    [SerializeField] private ToggleSwitch toggle;

    private void Update()
    {
        if (selectButton.ReadWasPerformedThisFrame())
        {
            toggle.Toggle();
        }
    }
}
