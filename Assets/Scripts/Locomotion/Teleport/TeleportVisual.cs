using UnityEngine;

public class TeleportVisual : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject landingSpot;
    Vector3 target = new Vector3();   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Unvisualize();
        landingSpot.gameObject.transform.SetParent(null);
    }

    // Update is called once per frame
    void Update()
    {
        if (lineRenderer != null && lineRenderer.enabled)
        {
            Vector3 origin = transform.position;
            Vector3 end = target;

            Vector3 mid = (end - origin)/2 + origin;

            Vector3 Q3 = (end - mid)/2 + mid;
            Vector3 Q1 = (mid - origin)/2 + origin;

            Q3 += new Vector3(0, 0.25f, 0);
            Q1 += new Vector3(0, 0.25f, 0);

            Vector3[] positions = new Vector3[] { origin, Q1, Q3, end };

            lineRenderer.SetPositions(positions);

            landingSpot.transform.position = target;
        }
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        lineRenderer.enabled = true;
        landingSpot.SetActive(true);
        landingSpot.transform.position = target;
    }

    public void Unvisualize()
    {
        lineRenderer.enabled = false;
        landingSpot.SetActive(false); 
    }
}
