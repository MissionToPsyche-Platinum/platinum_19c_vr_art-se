using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class SelectableSlider : SelectableSetting
{
    [SerializeField] private Slider slider;
    [SerializeField] private XRInputValueReader<Vector2> menuInteraction;

    [SerializeField, Tooltip("How fast the slider changes per second based on stick input")]
    private float sensitivity = 1f;

    private void Update()
    {
        float x = menuInteraction.ReadValue().x;

        // Update slider value based on input, sensitivity, and deltaTime
        slider.value += x * sensitivity * Time.deltaTime;

        // Clamp value to slider limits
        slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
    }
}
