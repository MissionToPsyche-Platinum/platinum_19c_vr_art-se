using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class TeleportLocomotion : MonoBehaviour
{
    public Transform rig;

    [SerializeField]
    XRInputValueReader<Vector2> teleInput;

    [SerializeField]
    XRInputButtonReader cancelInput;

    enum TELESTATE { IDLE, TELEPORTHELD, CANCELLED }

    TELESTATE state = TELESTATE.IDLE;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (teleInput == null || cancelInput == null || rig == null)
        {
            Debug.LogWarning("TELEPORT LOCOMOTION ERROR: Check the input values and rig fields! They are not set properly!");
            return;
        }

        bool cancelPresssed = cancelInput.ReadValue() == 1;
        Vector2 value = teleInput.ReadValue();
        bool teleportPressed = value != new Vector2();

        //this should always happen
        if (cancelPresssed)
        {
            state = TELESTATE.CANCELLED;
        }

        switch (state)
        {
            case TELESTATE.IDLE:
                if (teleportPressed)
                {
                    state = TELESTATE.TELEPORTHELD;
                    return;
                }
                break;
            case TELESTATE.TELEPORTHELD:
                if (!teleportPressed)
                {
                    state = TELESTATE.IDLE;
                    TeleportToPoint();
                    return;
                }
                break;
            case TELESTATE.CANCELLED:
                if(!cancelPresssed && !teleportPressed)
                {
                    state = TELESTATE.IDLE;
                    return;
                }
                break;
        }
    }

    public void TeleportToPoint()
    {
        if(Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit hit, Mathf.Infinity, (1 << 6)))
        {
            rig.transform.position = hit.point;
        }
    }
}
