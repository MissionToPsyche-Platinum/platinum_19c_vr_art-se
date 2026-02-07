using UnityEngine;
using UnityEngine.InputSystem;

public class SnapRotation : MonoBehaviour
{
    public InputActionReference snapRotationInput;
    public Transform XRTransform;
    [SerializeField] private int rotationAmount = 90;

    private bool turned = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float xInput = snapRotationInput.action.ReadValue<Vector2>().x;
        
        if (Mathf.Abs(xInput) >= 0.8f && !turned && GlobalSettings.canSnapTurn)
        {
            XRTransform.Rotate(0f, rotationAmount * Mathf.Sign(xInput), 0f, Space.Self);
            turned = true;
        }
        else if (Mathf.Abs(xInput) <= 0.8f && turned)
        {
            turned = false;
        }
    }
}
