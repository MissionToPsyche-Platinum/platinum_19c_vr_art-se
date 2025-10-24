using UnityEngine;

public class Button : MonoBehaviour
{
    public MeshRenderer buttonRenderer;
    public Material unhovered;
    public Material hovering;

    public void Hovered()
    {
        buttonRenderer.material = hovering;
        Debug.Log("HOVERED");
    }

    public void Unhovered()
    {
        buttonRenderer.material = unhovered;
    }
}
