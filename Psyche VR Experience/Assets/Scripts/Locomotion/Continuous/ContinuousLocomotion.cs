using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class ContinuousLocomotion : MonoBehaviour
{
    public Transform rig;
    public Transform cameraTransform;
    public float moveSpeed = 2f;
    public Volume volume;
    public Vignette vignette;

    [SerializeField]
    XRInputValueReader<Vector2> inputDir;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
       if (volume.profile.TryGet(out vignette))
       {
            vignette.intensity.value = 0f;
       }
    }

    // Update is called once per frame
    void Update()
    {
        if (LocomotionSettings.LOCOMOTION_MODE != LocomotionSettings.LocomotionMode.CONTINUOUS)
        {
            return;
        }

        Vector2 input = inputDir.ReadValue();

        // skip if there is not much input
        if (input.magnitude > 0.01f)
        {
            Debug.Log("INPUT: " + input.magnitude);
            Vector3 movementDirection = new Vector3(input.x, 0f, input.y);
            Vector3 cameraDirection = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
            
            // rotate the movement to be in the direction of the camera
            movementDirection = Quaternion.LookRotation(cameraDirection) * movementDirection;

            // apply vignette if setting is on
            if (LocomotionSettings.CONTINUOUS_VIGNETTE)
            {
                vignette.intensity.value = 0.5f;
            }

            rig.position += movementDirection * moveSpeed * Time.deltaTime;
        }
        else
        {
            // remove vignette
            vignette.intensity.value = 0f;
        }
    }
}
