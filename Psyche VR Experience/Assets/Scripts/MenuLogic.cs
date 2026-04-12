using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MenuLogic : MonoBehaviour
{
    public void StartExpo()
    {
        SceneManager.LoadSceneAsync(1);        
    }

    public void StartFreeRoam()
    {
        SceneManager.LoadSceneAsync(2);        
    }

    public void QuitExperience()
    {
        Debug.Log("Exiting Psyche VR Experience...");
        Application.Quit();
    }
}
