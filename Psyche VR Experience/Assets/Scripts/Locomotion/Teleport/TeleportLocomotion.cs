using System.Threading.Tasks;
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

    enum TELESTATE { IDLE, TELEPORTHELD, TELEPORTING, CANCELLED }

    TELESTATE state = TELESTATE.IDLE;

    [SerializeField]
    public TeleportVisual teleportVisual;

    [Header("Damping Value (0 - 1)")]
    public float dampingValue = 0.25f;
    private bool teleValid = false;
    private Vector3 telePos = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // we don't need to worry about it if the setting is not enabled
        if (LocomotionSettings.LOCOMOTION_MODE != LocomotionSettings.LocomotionMode.TELEPORT)
        {
            return;
        }

        if (teleInput == null || cancelInput == null || rig == null)
        {
            Debug.LogWarning("TELEPORT LOCOMOTION ERROR: Check the input values and rig fields! They are not set properly!");
            return;
        }

        bool cancelPresssed = cancelInput.ReadValue() == 1;
        Vector2 value = teleInput.ReadValue();
        bool teleportPressed = value != new Vector2();

        if (cancelPresssed && state != TELESTATE.TELEPORTING)
        {
            state = TELESTATE.CANCELLED;
        }

        switch (state)
        {
            case TELESTATE.IDLE:
                if (teleportPressed)
                {
                    teleValid = false;
                    telePos = new Vector3();
                    state = TELESTATE.TELEPORTHELD;
                    return;
                }
                break;
            case TELESTATE.TELEPORTHELD:

                TeleportData data = FindTeleportPoint();

                if (teleportVisual && data.valid)
                {
                    teleportVisual.SetTarget(data.position);
                } else if (teleportVisual)
                {
                    teleportVisual.Unvisualize();
                }

                if (!teleportPressed)
                {
                    TeleportAction(data);
                    return;
                }
                break;
            case TELESTATE.CANCELLED:

                if (teleportVisual.lineRenderer.enabled)
                {
                    teleportVisual.Unvisualize();
                }

                if(!cancelPresssed && !teleportPressed)
                {
                    state = TELESTATE.IDLE;
                    return;
                }
                break;
        }
    }

    async void TeleportAction(TeleportData data)
    {
        if (data.valid)
        {
            if (LocomotionSettings.TELEPORT_FADE_TO_BLACK)
            {
                state = TELESTATE.TELEPORTING;
                teleportVisual.FadeOut();

                while (teleportVisual.blackScreen.color.a < 1f)
                {
                    await Task.Delay(10);
                }
            }

            teleportVisual.Unvisualize();
            TeleportToPoint(data);

            while (teleportVisual.fadeState != TeleportVisual.FadeState.IDLE)
            {
                await Task.Delay(100);
            }
        }

        state = TELESTATE.IDLE;
    }

    struct TeleportData
    {
        public bool valid;
        public Vector3 position;

        public TeleportData(bool valid, Vector3 position)
        {
            this.valid = valid;
            this.position = position;
        }
    }

    TeleportData FindTeleportPoint()
    {
        TeleportData teleData;

        Vector3 direction = transform.forward;

        direction.y = Mathf.Min(-Mathf.Abs(direction.y), -0.05f);

        if (Physics.Raycast(new Ray(transform.position, direction), out RaycastHit hit, Mathf.Infinity, (1 << 6)))
        {
            teleData = new TeleportData(true, hit.point);
        } else
        {
            teleData = new TeleportData(false, Vector3.zero);
        }

        if(!teleValid && teleData.valid)
        {
            teleValid = true;
            telePos = teleData.position;
        }
        else if (teleData.valid)
        {
            telePos = Vector3.MoveTowards(telePos, teleData.position, Vector3.Distance(telePos, teleData.position) * dampingValue);
        }
        

        return new TeleportData(teleValid, telePos);
    }

    public void TeleportToPoint()
    {
        TeleportData data = FindTeleportPoint();

        TeleportToPoint(data);
    }

    void TeleportToPoint(TeleportData data)
    {
        if (data.valid)
        {
            rig.transform.position = data.position;
        }
    }
}
