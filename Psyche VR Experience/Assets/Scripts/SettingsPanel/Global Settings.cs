using UnityEngine;
using static LocomotionSettings;

public static class GlobalSettings
{
    // a float from 0 - 1 to represent how loud the game's volume is
    public static float MASTER_VOLUME
    {
        get { return masterVolume; }
        set { masterVolume = value; }
    }

    private static float masterVolume;
}
