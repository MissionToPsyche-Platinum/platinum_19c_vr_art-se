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

    [SerializeField][Tooltip ("Add the SpawnPoint Controller object here")]
    protected SpawnController spawnController;

    [SerializeField][Tooltip("Add the Player Camera object here")]
    protected Camera playerCamera;

    [SerializeField]
    protected float interactRange = 4f;
    protected float speed;

    [SerializeField][Tooltip("The default speed the player moves at")] protected float standardSpeed = 5.0f;

    [SerializeField][Tooltip("The sprinting speed the player moves at")] protected float sprintSpeed = 9.0f;
    protected int currentPerspective;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
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

    //Helps stablize and control movement.
    void FixedUpdate() 
    {
        CurrentPerspectiveListener();
        if(currentPerspective == 1)
        {
            MovePlayer();    
        }
        else if (currentPerspective == 2)
        {
            MoveFreeCamera();
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        //Both need to be checked every frame, otherwise they can get missed.
        if (interactAction.triggered)
        {
            PlayerInteract();
        }
        
        if (sprintAction.triggered)
        {
            ToggledSprintAction();
        }
        
    }

    void MovePlayer()
    {
        if (toggledSprintAction == true)
        {
            speed = sprintSpeed;
        }
        else
        {
            speed = standardSpeed;
        }

        Vector2 direction = moveAction.ReadValue<Vector2>();
        // Turn left/right with A/D (x-axis) **MAY SWAP TO BE A/D FOR MOVE LEFT MOVE RIGHT AND MOUSE FOR LOOK**
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            float turnSpeed = 150f;
            transform.Rotate(0, direction.x * turnSpeed * Time.fixedDeltaTime, 0);
        }
        // Move forward/backward with W/S (y-axis)
        Vector3 move = transform.forward * direction.y * speed * Time.fixedDeltaTime;    
        rb.MovePosition(rb.position + move);
    }

    //Movement logic for the free flying camera
    void MoveFreeCamera()
    {
        if (toggledSprintAction == true)
        {
            speed = sprintSpeed;
        }
        else
        {
            speed = standardSpeed;
        }

        Vector2 direction = moveAction.ReadValue<Vector2>();
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            float turnSpeed = 150f;
            transform.Rotate(0, direction.x * turnSpeed * Time.fixedDeltaTime, 0);
        }

        if (ascendAction.IsPressed())
        {
            transform.position += transform.up * 1.8f * Time.fixedDeltaTime;
        }
        else if (descendAction.IsPressed())
        {
            transform.position -= transform.up * 1.8f * Time.fixedDeltaTime;
        }
        //Generate Movement on a non-rigid body (Freecamera ignores physics and colliders)
        Vector3 move = transform.forward * direction.y * speed * Time.fixedDeltaTime;
        transform.position += move;
    }
    
    void PlayerInteract() //TODO Link this code up with the art system.
    { 
    //Currently this code throws out a ray, and tries to interact with whatever it hits. However, we need to set art pieces to
    //have a special trigger on them (Interactable).
        
        if(interactAction.triggered)
        {
            Debug.Log("You attempted to interact with an object.");
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;
            //Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.green, 0.1f);
            if (Physics.Raycast(ray, out hit, interactRange))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
    
    //Gives us our current perspective for the camera.
    int CurrentPerspectiveListener()
    {
        currentPerspective = spawnController.GetCurrentPerspective();
        return currentPerspective;
    }
    
    //Flips the sprint toggle on and off
    void ToggledSprintAction()
    {
        toggledSprintAction = !toggledSprintAction;
    }
}
