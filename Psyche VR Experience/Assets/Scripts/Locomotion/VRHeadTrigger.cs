using System.Collections.Generic;
using UnityEngine;

public class VRHeadTrigger : MonoBehaviour
{
    [Tooltip("Layers that should block the player's head. Set to Barrier")]
    public LayerMask collisionMask;

    public HashSet<Collider> CurrentCollisions { get; private set; } = new();

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("ENTER: " + other.name);
        if (IsInLayerMask(other.gameObject.layer))
        {
            CurrentCollisions.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("EXIT: " + other.name);
        if (CurrentCollisions.Contains(other))
        {
            CurrentCollisions.Remove(other);
        }
    }

    private bool IsInLayerMask(int layer)
    {
        return (collisionMask.value & (1 << layer)) != 0;
    }
}