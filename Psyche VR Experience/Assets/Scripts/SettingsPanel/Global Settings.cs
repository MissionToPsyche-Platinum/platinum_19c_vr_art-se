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

    // The multiplier for how big the text should be
    public static float TEXT_SIZE_MULTIPLIER
    {
        get { return textSizeMultplier; }
        set { textSizeMultplier = value; }
    }

    private static float masterVolume = 1f;
    private static float musicVolume = 1f;
    private static float textSizeMultplier = 1f;
}
