using UnityEngine;

// attach to the Environment Manager
// rotates the "Lighting" Empty which rotates all child lights with it.
[ExecuteAlways]
public class LightRotationUtilities : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;

    private Transform _lightingParent;

    void OnEnable()
    {
        GameObject go = GameObject.Find("Lighting");
        if (go != null)
            _lightingParent = go.transform;
        else
            Debug.LogWarning("[LightRotationUtilities] Could not find a GameObject named 'Lighting'.", this);
    }

    void Update()
    {
        if (_lightingParent == null) return;
        _lightingParent.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}