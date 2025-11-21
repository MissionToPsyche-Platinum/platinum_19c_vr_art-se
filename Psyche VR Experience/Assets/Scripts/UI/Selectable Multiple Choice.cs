using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SelectableMultipleChoice : SelectableSetting
{
    [SerializeField] private MultipleChoice choiceController;
    [SerializeField] private XRInputValueReader<Vector2> multipleChoiceInteraction;

    // Update is called once per frame
    private void Update()
    {
        if (!multipleChoiceInteraction.inputAction.triggered)
        {
            return;
        }

        float x = multipleChoiceInteraction.ReadValue().x;

        // left
        if (x < 0)
        {
            choiceController.LeftPressed();
        }
        // right
        else if (x > 0)
        {
            choiceController.RightPressed();
        }
    }
}
