using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HeadsetCollisionManager : MonoBehaviour
{
    [SerializeField, Range(0, 0.5f)][Tooltip("The delay between detection ticks")]
    private float detectionDelay = 0.05f;
    [SerializeField, Range(0, 0.3f)][Tooltip("The range at which the collision is measured for pushing back")]
    private float detectionDistance = 0.25f;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private LayerMask teleportationLayer;
    public List<RaycastHit> DetectedColliderHits {get; private set; }
    private float searchRadius = 25f;
    private float currentTime = 0;
    
    /// <summary>
    /// Returns a list of collisions used by the headset to push characters out of walls.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="distance">
    /// </param>
    /// <param name="mask"></param>
    /// <returns></returns>
    private List<RaycastHit> DetectCollisions (Vector3 position, float distance, LayerMask mask)
    {
        List<RaycastHit> detectedHits = new();

        //List of all current ray casts
        List<Vector3> directions = new() { transform.forward, -transform.forward, transform.right, -transform.right, Vector3.down };

        RaycastHit hit;
        foreach (var dir in directions)
        {
            if (Physics.Raycast(position, dir, out hit, distance, mask))
            {
                detectedHits.Add(hit);
            }
        }
        return detectedHits;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DetectedColliderHits = DetectCollisions(transform.position, detectionDistance, detectionLayers);
    }

    // Update is called once per frame
    void Update()
    {
         currentTime += Time.deltaTime;
        if (currentTime > detectionDelay)
        {
            currentTime = 0;
            DetectedColliderHits = DetectCollisions(transform.position, detectionDistance, detectionLayers);
        }

        if (GetNearestTeleportable() > -1)
        {
            Debug.Log("You have fallen out of bounds, resetting your position!");
            transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);

        }
    }

    public float GetNearestTeleportable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, teleportationLayer);
        float minimumDistance = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            float distance = Vector3.Distance(transform.position, c.transform.position);
            if (distance < minimumDistance) {
                minimumDistance = distance;
            }
        }
        return minimumDistance == Mathf.Infinity ? -1 : minimumDistance;
    }
}
