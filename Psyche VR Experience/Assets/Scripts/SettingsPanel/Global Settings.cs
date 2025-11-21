using UnityEngine;

public static class GlobalSettings
{
    // a float from 0 - 1 to represent how loud the game's volume is
    public static float MASTER_VOLUME
    {
        get { return masterVolume; }
        set { masterVolume = value; }
    }

    // a float from 0 - 1 to represent how loud the game's music is
    public static float MUSIC_VOLUME
    {
        get { return musicVolume; }
        set { musicVolume = value; }
    }

    private static float masterVolume;
    private static float musicVolume;
}
