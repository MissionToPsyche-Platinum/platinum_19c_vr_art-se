using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TeleportVisual : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject landingSpot;
    public Image blackScreen;

    public enum FadeState { IDLE, INCREASING, DECREASING };
    public FadeState fadeState = FadeState.IDLE;

    Vector3 target = new Vector3();   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Unvisualize();
        landingSpot.gameObject.transform.SetParent(null);
    }

    // Update is called once per frame
    async void Update()
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

        if (fadeState == FadeState.INCREASING)
        {
            Color color = blackScreen.color;
            color.a += Time.deltaTime / LocomotionSettings.TELEPORT_FADE_TIME;
            color.a = Mathf.Clamp(color.a, 0, 1);
            blackScreen.color = color;

            if(color.a == 1)
            {
                await Task.Delay((int)(LocomotionSettings.TELEPORT_FADE_WAIT * 1000));
                fadeState = FadeState.DECREASING;
            }
        } 
        else if(fadeState == FadeState.DECREASING)
        {
            Color color = blackScreen.color;
            color.a -= Time.deltaTime / LocomotionSettings.TELEPORT_FADE_TIME;
            color.a = Mathf.Clamp(color.a, 0, 1);
            blackScreen.color = color;
            
            if (color.a == 0)
            {
                fadeState = FadeState.IDLE;
            }
        }
    }

    public void FadeOut()
    {
        fadeState = FadeState.INCREASING;
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
