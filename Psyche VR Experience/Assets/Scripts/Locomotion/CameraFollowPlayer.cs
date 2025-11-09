//using System.Threading.Tasks.Dataflow;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField][Tooltip ("Add Mouse and Keyboard Player here")] 
    private Transform player;
    [SerializeField][Tooltip ("Offset for the camera (Default is )")] 
    private Vector3 offset;

    void LateUpdate()
    {
        transform.position = player.position + player.rotation * offset;
        transform.LookAt(player);
    }

}
