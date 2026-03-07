using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FadeOutHandler : MonoBehaviour
{
    [SerializeField] private float fadeToBlackDelay = 0.05f;
    private Material material;
    private bool fadeActive = false;

    private void Start()
    {
        material = GetComponent<MeshRenderer>().material;
    }

    public void Fade(bool fadeOut)
    {
        if (fadeOut && fadeActive)
        {
            return;
        }
        else if (!fadeOut && fadeActive)
        {
            return;
        }

        fadeActive = fadeOut;
        Debug.Log("Fade is activating");
        StartCoroutine(FadeEffect(fadeOut));
    }

    private IEnumerator FadeEffect(bool fadeOut)
    {
        float startAlpha = material.GetFloat("Alpha");
        float endAlpha = fadeOut ? 1.0f : 0.0f;
        float remainingTime = fadeToBlackDelay * Mathf.Abs(endAlpha - startAlpha);
        float elapsedTime = 0;
        while (elapsedTime < fadeToBlackDelay)
        {
            elapsedTime += Time.deltaTime;
            float tempVal = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / remainingTime);
            material.SetFloat("Alpha", tempVal);
            yield return null;
        }
        material.SetFloat("Alpha", endAlpha);
    }
}