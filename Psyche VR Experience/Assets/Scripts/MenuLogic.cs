using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Diagnostics;

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
        UnityEngine.Debug.Log("Exiting Psyche VR Experience...");
        Application.Quit();
    }
}
