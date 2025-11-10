using UnityEngine;

public class InteractableObject : MonoBehaviour, Interactable
{
    public void Interact()
    {
        Debug.Log($"{gameObject.name} has been interacted with (DEFAULT BEHAVIOR)");

    }
}
