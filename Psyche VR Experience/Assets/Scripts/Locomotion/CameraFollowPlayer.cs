//using System.Threading.Tasks.Dataflow;
using System;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField][Tooltip ("Add Mouse and Keyboard Player here")] 
    private Transform player;
    [SerializeField][Tooltip ("Offset for the camera (Default is )")] 
    private Vector3 offset = new Vector3(0.0f, 1.5f, -3.8f);
    public bool firstPersonCamera = false;
    
    void Awake()
    {
        // Allows for a default setting to be changed in the Unity Editor (Checks for true or false)
        CameraPositionChange();
    }

    //Captures the current location of the player and adjusts the camera accordingly
    void LateUpdate()
    {
        transform.position = player.position + player.rotation * offset;
        
        if (firstPersonCamera == true)
        {
            transform.rotation = player.rotation;
        }
        else if (firstPersonCamera == false)
        {
            transform.LookAt(player);
        }
    }

    //Controls the positional swap of the camera.
    public void CameraPositionChange()
    {
        if (firstPersonCamera == false)
        {
            offset = new Vector3 (0.0f, 0.5f, 0.0f);

        } 
        else
        {
            offset = new Vector3 (0.0f, 1.5f, -3.8f);            
        }
        firstPersonCamera = !firstPersonCamera;
        return;
    }

}
