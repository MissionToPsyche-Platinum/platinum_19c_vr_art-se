using UnityEngine;

public class ContinuousLocomotion : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (LocomotionSettings.LOCOMOTION_MODE != LocomotionSettings.LocomotionMode.CONTINUOUS)
        {
            return;
        }

        // TODO : Task 126
    }
}
