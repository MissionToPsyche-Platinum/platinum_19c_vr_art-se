using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEditor.Callbacks;


public class MoveTestPlayer : MonoBehaviour
{
    protected PlayerInput playerInput;
    protected InputAction moveAction;
    protected Rigidbody rb;

    [SerializeField]
    protected Camera playerCamera;
    protected float speed = 5f;
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
            moveAction = playerMap.FindAction("Move", true);
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
        MovePlayer();
    }

    void MovePlayer()
    {
        /*Vector2 direction = moveAction.ReadValue<Vector2>();
        Debug.Log(direction);
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        //Vector3 move = (direction.y * camForward + direction.x * camRight) * 3.0f * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);*/
        

        Vector2 direction = moveAction.ReadValue<Vector2>();
        // Turn left/right with A/D (x-axis)
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            float turnSpeed = 150f;
            transform.Rotate(0, direction.x * turnSpeed * Time.deltaTime, 0);
        }
        // Move forward/backward with W/S (y-axis)
        //Vector3 forwardMove = transform.forward * direction.y * speed * Time.deltaTime;
        Vector3 move = transform.forward * direction.y * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);


    }
}
