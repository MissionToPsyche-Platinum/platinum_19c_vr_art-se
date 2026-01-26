using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    public MeshRenderer buttonRenderer;
    public Material unhovered;
    public Material hovering;

    public Animator animator;

    [SerializeField] private UnityEvent ButtonClickedEvent;

    public bool canBePushed = true;

    public void Hovered()
    {
        if (!canBePushed)
            return;

        buttonRenderer.material = hovering;
    }

    public void Unhovered()
    {
        if (!canBePushed)
            return;

        buttonRenderer.material = unhovered;
    }

    public async void Pressed()
    {
        if (!canBePushed)
            return;

        Unhovered();
        ButtonClickedEvent?.Invoke();

        animator.Play("Push");

        canBePushed = false;

        while(animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            if (this == null)
                return;

            await Task.Delay(10);
        }

        animator.Play("Idle");
        canBePushed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Pressed();
    }
}
