using UnityEngine;

public class ControllerButtonLaser : MonoBehaviour
{
    public Transform laserObject;
    public float maxButtonDistance = 15;

    Button target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit info;
        Physics.Raycast(new Ray(transform.position, transform.forward), out info, maxButtonDistance, (1 << 7));

        if (info.collider == null)
        {
            if(target != null) {
                target.Unhovered();
                target = null; 
            }

            laserObject.gameObject.SetActive(false);

            return;
        }

        laserObject.gameObject.SetActive(true);
        laserObject.transform.position = transform.position;

        laserObject.LookAt(info.point);
        laserObject.localEulerAngles = new Vector3(90, 0, 0);
        laserObject.transform.localScale = new Vector3(laserObject.transform.localScale.x, info.distance / 2, laserObject.transform.localScale.z);

        Button hover = info.collider.transform.gameObject.GetComponent<Button>();

        if (hover == null)
        {
            if(target != null)
                target.Unhovered();
            laserObject.gameObject.SetActive(false);
            target = null;
            return;
        }

        if (target != null && target == hover)
            return;

        if(target != null)
            target.Unhovered();

        target = hover;
        target.Hovered();
    }
}
