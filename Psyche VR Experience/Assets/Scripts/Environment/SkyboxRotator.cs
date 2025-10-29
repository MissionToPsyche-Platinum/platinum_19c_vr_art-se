using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private float rotationRate = 1.0f;
    // rotates it just a tad every frame

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationRate);
    }
}
