using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SelectableMultipleChoice : SelectableSetting
{
    [SerializeField] private MultipleChoice choiceController;
    [SerializeField] private XRInputValueReader<Vector2> multipleChoiceInteraction;

    private float pressThreshold = 0.8f;
    private bool menuInteracted = false;        // need this to use the joystick as a button

    // Update is called once per frame
    private void Update()
    {
        float x = multipleChoiceInteraction.ReadValue().x;

        // up
        if (!menuInteracted && x > pressThreshold)
        {
            menuInteracted = true;
            choiceController.RightPressed();
        }
        // down
        else if (!menuInteracted && x < -pressThreshold)
        {
            menuInteracted = true;
            choiceController.LeftPressed();
        }

        if (menuInteracted && Mathf.Abs(x) < pressThreshold)
        {
            menuInteracted = false;
        }
    }
}
