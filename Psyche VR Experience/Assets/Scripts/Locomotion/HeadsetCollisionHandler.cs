using System.Collections.Generic;
using UnityEngine;

public class HeadsetCollisionHandler : MonoBehaviour
{
    [SerializeField]
    private HeadsetCollisionManager collisionDetector;
    [SerializeField]
    private CharacterController characterController;
    [SerializeField][Tooltip("The amount of force the wall pushes the player with")]
    public float knockbackStrength = 1.0f;
    
    private Vector3 CalculateKnockbackDirection(List<RaycastHit> colliderHits)
    {
        Vector3 combinedNormal = Vector3.zero;
        foreach (RaycastHit hitLocation in colliderHits)
        {
            combinedNormal += new Vector3 (hitLocation.normal.x, 0, hitLocation.normal.z);
        }
        return combinedNormal;
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
        Debug.Log("I threw you back inside the gameworld!");
    }
}
