using UnityEngine;

public class LaunchRoomDoor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource openAudio;
    [SerializeField] private AudioSource closeAudio;

    private bool doorOpen = false;

    public void DoorButtonPressed()
    {
        if (AnimationPlaying()) { return; }
        if (doorOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }


    private void OpenDoor()
    {
        openAudio.Play();
        animator.Play("DoorDown");
        doorOpen = true;
    }

    private void CloseDoor()
    {
        closeAudio.Play();
        animator.Play("DoorUp");
        doorOpen = false;
    }

    private bool AnimationPlaying()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.normalizedTime < 1f;
    }
}
