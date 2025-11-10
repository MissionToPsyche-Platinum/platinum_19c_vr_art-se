using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class ContinuousLocomotion : MonoBehaviour
{
    public Transform rig;
    public Transform cameraTransform;
    public float moveSpeed = 2f;

    [SerializeField]
    XRInputValueReader<Vector2> inputDir;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
            Vector3 movementDirection = new Vector3(input.x, 0f, input.y);
            Vector3 cameraDirection = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
            
            // rotate the movement to be in the direction of the camera
            movementDirection = Quaternion.LookRotation(cameraDirection) * movementDirection;


            rig.position += movementDirection * moveSpeed * Time.deltaTime;
        }
    }
}
