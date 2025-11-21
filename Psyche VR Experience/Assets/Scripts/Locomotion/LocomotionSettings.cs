using UnityEngine;

public static class LocomotionSettings
{
    public enum LocomotionMode { TELEPORT, CONTINUOUS }

    public static LocomotionMode LOCOMOTION_MODE
    {
        get {  return locomotionMode; }
        set { locomotionMode = value; }
    }

    public static bool TELEPORT_FADE_TO_BLACK
    {
        get { return fadeToBlack; }
        set { fadeToBlack = value; }
    }

    public static float TELEPORT_FADE_TIME
    {
        get { return fadeTime; }
        set { fadeTime = value; }
    }

    public static float TELEPORT_FADE_WAIT
    {
        get { return fadeWait; }
        set { fadeWait = value; }
    }
    
    // a boolean to represent whether a vignette should be added to the player's view when moving with continuous locomotion
    public static bool CONTINUOUS_VIGNETTE
    {
        get { return continuousVignette; }
        set { continuousVignette = value; }
    }

    static LocomotionMode locomotionMode = LocomotionMode.TELEPORT;
    static bool fadeToBlack = true;
    static float fadeTime = 1.0f;
    static float fadeWait = 1.0f;
    static bool continuousVignette = false;
}
