using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HeadsetCollisionManager : MonoBehaviour
{
    [SerializeField, Range(0, 0.5f)]
    private float detectionDelay = 0.05f;
    [SerializeField]
    private float detectionDistance = 0.2f;
    [SerializeField]
    private LayerMask detectionLayers;
    public List<RaycastHit> DetectedColliderHits {get; private set; }
    private float currentTime = 0;
    private List<RaycastHit> DetectCollisions (Vector3 position, float distance, LayerMask mask)
    {
        List<RaycastHit> detectedHits = new();

        List<Vector3> directions = new() { transform.forward, transform.right, -transform.right };

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
    }
}
