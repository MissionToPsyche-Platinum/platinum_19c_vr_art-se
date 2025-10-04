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

    bool teleportState = false;

    bool ignoreInputUntilCancelReleased = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //TODO: please go back and clean this up, my eyes hurt
        if (teleInput != null && cancelInput != null && rig != null)
        {
            if (!teleportState)
            {
                float cancelPressed = cancelInput.ReadValue();

                if(cancelPressed == 1)
                {
                    ignoreInputUntilCancelReleased = true;
                }           

                Vector2 value = teleInput.ReadValue();
                bool teleportPressed = value != new Vector2();

                if (teleportPressed && !ignoreInputUntilCancelReleased)
                {
                    teleportState = true;
                    return;
                }

                if (!teleportPressed)
                {
                    ignoreInputUntilCancelReleased = false;
                }

            } else
            {
                float cancelPressed = cancelInput.ReadValue();
                
                if(cancelPressed == 1)
                {
                    teleportState = false;
                    return;
                }

                //on release, attempt teleport
                if (teleInput.ReadValue() == new Vector2() && Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit hit, Mathf.Infinity, (1 << 6)))
                {
                    rig.transform.position = hit.point;
                    teleportState = false;
                }
            }
        } else
        {
            Debug.LogWarning("TELEPORT LOCOMOTION ERROR: Check the input values and rig fields! They are not set properly!");
        }
    }
}
