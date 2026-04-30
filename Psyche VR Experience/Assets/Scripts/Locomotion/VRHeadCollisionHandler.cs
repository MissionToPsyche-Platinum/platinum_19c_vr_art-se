using UnityEngine;
using System.Collections.Generic;
//using System.Diagnostics;

public class VRHeadCollisionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] [Tooltip("Main Camera. Represents the actual VRHeadTrigger Script")] private VRHeadTrigger headTrigger;
    [SerializeField] [Tooltip("Main Camera. Represents the position")]private Transform headTransform;
    [SerializeField] [Tooltip("Represents the actual ROOT of the XR system (the top level object that contains all others)")] private Transform xrOriginRoot;

    [Header("Tuning")]
    [SerializeField, Range(0f, 3f)]
    private float pushStrength = 1.5f;

    [SerializeField]
    private bool ignoreVertical = true;

    void LateUpdate()
    {
        //Debug.Log("VR Collision Handler is Active!");
        if (headTrigger.CurrentCollisions.Count == 0)
            return;

        Vector3 headPos = headTransform.position;
        Vector3 totalPush = Vector3.zero;

        foreach (var col in headTrigger.CurrentCollisions)
        {
            if (col == null) continue;

            Vector3 closest = col.ClosestPoint(headPos);
            Vector3 penetration = headPos - closest;

            if (ignoreVertical)
                penetration.y = 0f;

            float sqrMag = penetration.sqrMagnitude;
            if (sqrMag > 0.0001f)
            {
                float distance = Mathf.Sqrt(sqrMag);
                totalPush += (penetration / distance) * distance;
            }
        }

        if (totalPush.sqrMagnitude > 0.0001f)
        {
            Vector3 move = totalPush.normalized * pushStrength * Time.deltaTime;

            if (ignoreVertical)
                move.y = 0f;
            
            //Debug.Log($"Push: {totalPush}, Magnitude: {totalPush.magnitude}");
            // Move the entire rig, not the camera
            xrOriginRoot.position += move;
        }
    }
}