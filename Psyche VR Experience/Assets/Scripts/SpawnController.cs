//TEMP DISABLE 1/14/26 Cade Tanner
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class SpawnController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected PlayerInput playerInput;
    protected InputAction swapAction;
    [SerializeField][Tooltip("Assign the M&K Camera here")]
    protected Camera MKPlayerCamera;
    [SerializeField][Tooltip("Assign the VRPlayer Camera here")]
    protected Camera VRPlayerCamera;
    [SerializeField][Tooltip("Assign the Freeroam Camera here")]
    protected Camera FreePlayerCamera;
    [SerializeField]
    protected Camera[] cameraArray;

    [SerializeField][Tooltip("0-VR, 1-M&K, 2-Freeroam")]
    protected GameObject[] characterArray;

    protected int currentPerspective; //0 is VR, 1 is M&K, 2 is Freeroam

    [SerializeField] protected MuseumManager MuseumManagerTracker;    
    void Awake()
    {
        //Task 188, Grab the position of a placed tile and set players spawn location to that point.
        Vector3 spawnPosition = MuseumManagerTracker.firstRoom.position;
        transform.position = spawnPosition;
        Debug.Log("SpawnController moved to position " + transform.position);
        if (spawnPosition != null) 
        {
            Vector3 playerSpawnPosition = new Vector3 (spawnPosition.x, 1f, spawnPosition.z);
            characterArray[0].transform.position = playerSpawnPosition;
        }
        
        //Set up Mouse and Keyboard controls
        playerInput = GetComponent<PlayerInput>();
        playerInput.currentActionMap.Enable();
        if (playerInput != null)
        {
            var controlCamera = playerInput.actions.FindActionMap("Mouse&Keyboard", true);
            swapAction = controlCamera.FindAction("SwapCamera", true); //Q on Keyboard (Opens up Free Camera Only actions as well *See MoveTestPlayer*)
            swapAction.Enable();
        }
        
        for (int i = 1; i < 3; i++)
        {
            cameraArray[i].GetComponent<AudioListener>().enabled = false;
            cameraArray[i].enabled = false;
            characterArray[i].SetActive(false);
        }
        
        //Task 113, this checks for an active instance of a VR headset. If it can't find one, it defaults to mouse and keyboard
        if (XRGeneralSettings.Instance?.Manager?.activeLoader ==  null)
        {
            Debug.Log("No VR device detected, automatically swapping you to Mouse and Keyboard");
            //Grab all information needed by the starting VR position
            Vector3 currentPosition = characterArray[0].transform.position;
            cameraArray[0].GetComponent<AudioListener>().enabled = false;
            cameraArray[0].enabled = false;
            characterArray[0].SetActive(false);
            
            //Swap user to mouse and keyboard and enable 
            currentPerspective = 1;
            cameraArray[1].GetComponent<AudioListener>().enabled = true;
            cameraArray[1].enabled = true;
            characterArray[1].SetActive(true);
            characterArray[1].transform.position = currentPosition;
        }       
    }

    // Update is called once per frame
    void Update()
    {
        SwapCamera();
    }

    void SwapCamera()
    {
        //Checks to see if there is a trigger action that occurs
        //Pattern is VR -> Mouse & Keyboard -> Freeroam 
        if (swapAction.triggered)
        {
            //Disable all relevant pieces of current perspective
            Debug.Log("Current Perspective is: " + this.currentPerspective);
            Vector3 currentPosition = characterArray[currentPerspective].transform.position;
            cameraArray[currentPerspective].GetComponent<AudioListener>().enabled = false;
            cameraArray[currentPerspective].enabled = false;
            characterArray[currentPerspective].SetActive(false);
            if (currentPerspective < 2) //Enable next perspective
            {
                Debug.Log("Swapping to the next available camera");
                this.currentPerspective++;
                cameraArray[currentPerspective].enabled = true;
                cameraArray[currentPerspective].GetComponent<AudioListener>().enabled = true;
                characterArray[currentPerspective].SetActive(true);
                characterArray[currentPerspective].transform.position = currentPosition;
            }
            else if (XRGeneralSettings.Instance?.Manager?.activeLoader == null && currentPerspective == 2)
            {
                //Case where no headset is detected and we want to swap from freecamera to normal camera
                Debug.Log("No VR Headset detected, swapping to mouse and keyboard");
                this.currentPerspective = 1;
                cameraArray[currentPerspective].enabled = true;
                cameraArray[currentPerspective].GetComponent<AudioListener>().enabled = true;
                characterArray[currentPerspective].SetActive(true);
                characterArray[currentPerspective].transform.position = currentPosition;
            }
            else//Swap back to VR if headset is detected
            {
                Debug.Log("Resetting to VR");
                this.currentPerspective = 0;
                cameraArray[currentPerspective].enabled = true;
                cameraArray[currentPerspective].GetComponent<AudioListener>().enabled = true;
                characterArray[currentPerspective].SetActive(true);
                characterArray[currentPerspective].transform.position = currentPosition;
            }

        }
    }
    
    public int GetCurrentPerspective()
    {
        return currentPerspective;
    }
}
