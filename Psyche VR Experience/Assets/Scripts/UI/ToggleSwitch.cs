using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ToggleSwitch : MonoBehaviour
{
    [SerializeField] protected float sliderValue = 0f;
    public bool CurrentValue { get; private set; }

    private Slider slider;

    private float animationDuration = 0.5f;
    private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine animateSliderCoroutine;

    [Header("Events")]
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;

    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Material backgroundMaterial;
    private Material localCopyOfBackgroundMaterial;

    protected Action transitionEffect;
    
    private float defaultSliderVal;

    protected virtual void OnValidate()
    {
        localCopyOfBackgroundMaterial = new Material(backgroundMaterial);
        backgroundImage.material = localCopyOfBackgroundMaterial;

        if (slider != null) { return; }

        slider = GetComponent<UnityEngine.UI.Slider>();

        CurrentValue = sliderValue > 0 ? true : false;
    }

    protected virtual void Awake()
    {
        // set default value to currently set value
        defaultSliderVal = sliderValue;
        OnValidate();

        if (sliderValue != 0)
        {
            StartCoroutine(InitializeMaterial());
        }
    }

    //this is a really scuffed way to do this, but basically if the sliderValue is at 1,
    // meaning that the property is on at the start (such as skybox rotation), then you
    // can't actually transition the image on the first frame this object is awake. So
    // We just wait a frame here and then do it. Works like a charm.
    private IEnumerator InitializeMaterial()
    {
        yield return new WaitForEndOfFrame();

        TransitionImages();
    }

    public void Toggle()
    {
        startAnimation(!CurrentValue);
    }

    private void startAnimation(bool state)
    {
        bool previousValue = CurrentValue;
        CurrentValue = state;

        if (previousValue != CurrentValue)
        {
            if (CurrentValue)
                onToggleOn?.Invoke();
            else
                onToggleOff?.Invoke();
        }

        if (animateSliderCoroutine != null)
            StopCoroutine(animateSliderCoroutine);

        animateSliderCoroutine = StartCoroutine(AnimateSlider());
    }

    private IEnumerator AnimateSlider()
    {
        float startValue = slider.value;
        float endValue = CurrentValue ? 1 : 0;

        float time = 0;
        if (animationDuration > 0)
        {
            while (time < animationDuration)
            {
                time += Time.deltaTime;

                float lerpFactor = slideEase.Evaluate(time / animationDuration);
                slider.value = sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                TransitionImages();

                yield return null;
            }
        }

        slider.value = endValue;
    }

    private void TransitionImages()
    {
        backgroundImage.material.SetFloat("_MixValue", sliderValue);
    }
    
    public void Reset()
    {
        if (slider.value != defaultSliderVal)
        {
            Toggle();
        }
    }
}
