/*using System.Collections.Generic;
using UnityEngine;

public class HeadsetCollisionHandler : MonoBehaviour
{
    [SerializeField]
    private HeadsetCollisionManager collisionDetector;
    [SerializeField]
    private CharacterController characterController;
    [SerializeField, Range(0, 2.0f)][Tooltip("The amount of force the wall pushes the player with")]
    public float knockbackStrength = 1.35f;
    
    /// <summary>
    /// Takes the given collider hits and calculates how far the player needs to be pushed to get back into bounds.
    /// </summary>
    /// <param name="colliderHits">
    /// Number of colliders that have been triggered
    /// </param>
    /// <returns>
    /// A calculated normal for knocking the player back into bounds.
    /// </returns>
    /*private Vector3 CalculateKnockbackDirection(List<Collider> colliderHits)
    {
        Vector3 combinedNormal = Vector3.zero;
        foreach (RaycastHit hitLocation in colliderHits)
        {
            combinedNormal += new Vector3 (hitLocation.normal.x, 0, hitLocation.normal.z);
        }
        return combinedNormal;
    }

    private Vector3 CalculateKnockbackDirection(List<Collider> colliders, Vector3 headPosition)
    {
        Vector3 combined = Vector3.zero;

        foreach (Collider col in colliders)
        {
            Vector3 closest = col.ClosestPoint(headPosition);
            Vector3 penetration = headPosition - closest;

            // Ignore vertical push if you want to keep current behavior
            penetration.y = 0;

            if (penetration.sqrMagnitude > 0.0001f)
            {
                combined += penetration.normalized;
            }
        }

        return combined;
    }
    // Update is called once per frame
    void Update()
    {
        if (collisionDetector.DetectedColliderHits.Count <= 0)
        {
            return;
        }
        
        Vector3 knockbackDirection = CalculateKnockbackDirection(collisionDetector.DetectedColliderHits);
        
        characterController.Move(knockbackDirection.normalized * knockbackStrength * Time.deltaTime);
        Vector3 position = characterController.transform.position;
        position.y = 0;
        characterController.transform.position = position;
        Debug.Log("I threw you back inside the gameworld!");
    }
}
*/
