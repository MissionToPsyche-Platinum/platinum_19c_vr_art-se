using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SpawnController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected PlayerInput playerInput;
    protected InputAction swapAction;
    [SerializeField][Tooltip("Assign the M&K Camera here")]
    protected Camera MKPlayerCamera;
    [SerializeField][Tooltip("Assign the VRPlayer Camera here")]
    protected Camera VRPlayerCamera;
    [SerializeField]
    [Tooltip("Assign the Freeroam Camera here")]
    protected Camera FreePlayerCamera;
    [SerializeField]
    protected Camera[] cameraArray;

    [SerializeField][Tooltip("0-VR, 1-M&K, 2-Freeroam")]
    protected GameObject[] characterArray;

    protected int currentPerspective; //0 is VR, 1 is M&K, 2 is Freeroam
    
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInput.currentActionMap.Enable();
        if (playerInput != null)
        {
            var controlCamera = playerInput.actions.FindActionMap("Mouse&Keyboard", true);
            swapAction = controlCamera.FindAction("SwapCamera", true);
            swapAction.Enable();
        }

        for (int i = 1; i < 3; i++)
        {
            cameraArray[i].enabled = false;
            characterArray[i].SetActive(false);
        }       
    }

    // Update is called once per frame
    void Update()
    {
        SwapCamera();
    }

    void SwapCamera()
    {
        
        if (swapAction.triggered)
        {
            Debug.Log("Current Perspective is: " + this.currentPerspective);
            Vector3 currentPosition = characterArray[currentPerspective].transform.position;
            cameraArray[currentPerspective].enabled = false;
            characterArray[currentPerspective].SetActive(false);
            if (currentPerspective < 2)
            {
                Debug.Log("Swapping to the next available camera");
                this.currentPerspective++;
                cameraArray[currentPerspective].enabled = true;
                characterArray[currentPerspective].SetActive(true);
                characterArray[currentPerspective].transform.position = currentPosition;
            } else
            {
                Debug.Log("Resetting to VR");
                this.currentPerspective = 0;
                cameraArray[currentPerspective].enabled = true;
                characterArray[currentPerspective].SetActive(true);
                characterArray[currentPerspective].transform.position = currentPosition;
            }
            
        }
        
    }
}
