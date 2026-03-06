using UnityEngine;

public class LightController : MonoBehaviour
{
    [Tooltip("The text will appear to the right of this frame part.")]
    [SerializeField] private Transform frameTransform;
    [Tooltip("Wall lamp prefab object.")]
    [SerializeField] private GameObject lamp;

    // Yes, I know that updating the lamp position every frame is bad practice.  But that doesn't mean I'm not going to do it.
    void Update()
    {
        lamp.transform.position = new Vector3(lamp.transform.position.x, frameTransform.position.y + 1, lamp.transform.position.z);
    }
}
