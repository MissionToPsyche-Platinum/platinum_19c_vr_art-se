using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEditor.Callbacks;


public class MoveTestPlayer : MonoBehaviour
{
    protected PlayerInput playerInput;
    protected InputAction moveAction;
    protected InputAction sprintAction;
    protected InputAction ascendAction;
    protected InputAction descendAction;
    protected InputAction interactAction;
    protected bool toggledSprintAction = false;
    protected Rigidbody rb;

    [SerializeField]
    protected SpawnController spawnController;

    [SerializeField]
    protected Camera playerCamera;
    protected float speed = 1.5f;
    protected int currentPerspective;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Debug.Log("Hello There");
        rb = GetComponent<Rigidbody>();
        //playerCamera = GetComponentInChildren<Camera>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInput.currentActionMap.Enable();
        if (playerInput != null)
        {
            Debug.Log("Found playerInput");
            var playerMap = playerInput.actions.FindActionMap("Mouse&Keyboard", true);
            moveAction = playerMap.FindAction("Move", true); // W/S for forward/backward, A/D for turn Left/turn right
            sprintAction = playerMap.FindAction("Sprint", true); //Left-Shift on Keyboard
            ascendAction = playerMap.FindAction("Ascend", true); //Spacebar on Keyboard (FREE CAMERA ONLY)
            descendAction = playerMap.FindAction("Descend", true); //Left Control (FREE CAMERA ONLY)
            interactAction = playerMap.FindAction("Interact", true); //E on Keyboard
            
            Debug.Log($"Found moveAction action in map : {moveAction.actionMap.name}");
            moveAction.Enable();
            if (moveAction != null)
            {
                Debug.Log("Found moveAction");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        CurrentPerspectiveListener();
        Debug.Log(currentPerspective);
        if (currentPerspective == 1)
        {
            MovePlayer();
        }
        else if (currentPerspective == 2)
        {
            MoveFreeCamera();
        }

        
    }

    void MovePlayer()
    {
        if (sprintAction.triggered) //If left shift is pressed, toggle the sprint
        {
            toggledSprintAction = !toggledSprintAction;
            Debug.Log("Toggled Sprint! Value is now " + toggledSprintAction);
        }

        if (toggledSprintAction == true)
        {
            speed = 3.5f;
        }
        else
        {
            speed = 1.5f;
        }

        Vector2 direction = moveAction.ReadValue<Vector2>();
        // Turn left/right with A/D (x-axis) **MAY SWAP TO BE A/D FOR MOVE LEFT MOVE RIGHT AND MOUSE FOR LOOK**
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            float turnSpeed = 150f;
            transform.Rotate(0, direction.x * turnSpeed * Time.deltaTime, 0);
        }
        // Move forward/backward with W/S (y-axis)
        Vector3 move = transform.forward * direction.y * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    void MoveFreeCamera()
    {
        Vector2 direction = moveAction.ReadValue<Vector2>();
        // Turn left/right with A/D (x-axis)
        if (sprintAction.triggered) //If left shift is pressed, toggle the sprint
        {
            toggledSprintAction = !toggledSprintAction;
            Debug.Log("Toggled Sprint! Value is now " + toggledSprintAction);
        }

        if (toggledSprintAction == true)
        {
            speed = 3.5f;
        }
        else
        {
            speed = 1.5f;
        }

        if (Mathf.Abs(direction.x) > 0.1f)
        {
            float turnSpeed = 150f;
            transform.Rotate(0, direction.x * turnSpeed * Time.deltaTime, 0);
        }

        if (ascendAction.IsPressed())
        {
            transform.position += transform.up * .5f * Time.fixedDeltaTime;
        }
        else if (descendAction.IsPressed())
        {
            transform.position -= transform.up * .5f * Time.fixedDeltaTime;
        }
        //Generate Movement on a non-rigid body (Freecamera ignores physics and colliders)
        Vector3 move = transform.forward * direction.y * speed * Time.fixedDeltaTime;
        transform.position += move;
    }
    
    void Interact() //TODO Link this code up with the art system.
    { 
    //Currently this code throws out a ray, and tries to interact with whatever it hits. However, we need to set art pieces to
    //have a special trigger on them (IInteractable).
        /*if(interactAction.triggered)
        {
            Debug.Log("You attempted to interact.");
             Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                interactable.Interact();
                }
            }
        }*/
    }
    
    int CurrentPerspectiveListener()
    {
        currentPerspective = spawnController.GetCurrentPerspective();
        return currentPerspective;
    }
}
