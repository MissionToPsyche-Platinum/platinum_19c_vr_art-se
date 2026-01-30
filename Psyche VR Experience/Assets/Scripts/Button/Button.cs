using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    public MeshRenderer buttonRenderer;
    public Material unhovered;
    public Material hovering;
    public Material pressed;

    public Animator animator;

    [SerializeField] private UnityEvent ButtonClickedEvent;

    public bool canBePushed = true;

    public bool toggleColor = false;
    bool toggle = false;

    public void Hovered()
    {
        if (!canBePushed)
            return;

        buttonRenderer.material = hovering;
    }

    public void Unhovered()
    {
        if (!toggle)
        {
            buttonRenderer.material = unhovered;
        }
        else
        {
            buttonRenderer.material = pressed;
        }
    }

    public async void Pressed()
    {
        if (!canBePushed)
            return;

        if (toggleColor)
        {
            toggle = !toggle;
        }

        Unhovered();

        animator.Play("Push");

        canBePushed = false;

        await Task.Delay(100);

        while(animator.GetCurrentAnimatorStateInfo(0).IsName("Push"))
        {
            if (this == null)
                return;

            await Task.Delay(100);
        }

        ButtonClickedEvent?.Invoke();

        canBePushed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("PHYSICAL PUSH");
        Pressed();
    }
}
