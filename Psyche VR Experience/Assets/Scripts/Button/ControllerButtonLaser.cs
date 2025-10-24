using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class ControllerButtonLaser : MonoBehaviour
{
    public Transform laserObject;
    public float maxButtonDistance = 15;

    Button target;

    [SerializeField]
    XRInputButtonReader triggerInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        FindTarget();

        bool pressed = triggerInput.ReadIsPerformed();

        if (pressed && target != null)
        {
            target.Pressed();
            target = null;
        }
    }

    public void FindTarget()
    {
        RaycastHit info;
        Physics.Raycast(new Ray(transform.position, transform.forward), out info, maxButtonDistance, (1 << 7));

        if (info.collider == null)
        {
            if (target != null)
            {
                target.Unhovered();
                target = null;
            }

            laserObject.gameObject.SetActive(false);

            return;
        }

        Button hover = info.collider.transform.gameObject.GetComponent<Button>();

        if (hover == null)
        {
            if (target != null)
                target.Unhovered();
            laserObject.gameObject.SetActive(false);
            target = null;
            return;
        }

        PointLaserAtTarget(info.point, info.distance);

        if (!hover.canBePushed)
            return;

        if (target != null && target == hover)
            return;

        if (target != null)
            target.Unhovered();

        target = hover;
        target.Hovered();
    }

    public void PointLaserAtTarget(Vector3 point, float distance)
    {
        laserObject.gameObject.SetActive(true);
        laserObject.transform.position = transform.position;

        laserObject.LookAt(point);
        laserObject.localEulerAngles = new Vector3(90, 0, 0);
        laserObject.transform.localScale = new Vector3(laserObject.transform.localScale.x, distance / 2, laserObject.transform.localScale.z);
    }
}
