using UnityEngine;


public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private float rotationRate = 1.0f;

    // little rotation every frame.
    void Update()
    {
        rotationRate = GlobalSettings.SKYBOX_ROTATION_ON;
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationRate);
    }
}
