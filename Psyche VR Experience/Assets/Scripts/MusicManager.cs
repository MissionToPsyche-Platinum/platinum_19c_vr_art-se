using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class MusicManager : MonoBehaviour
{
    [Tooltip("Link this to the audio source on the XR Origin Main Camera MusicSource")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] public AudioClip[] musicPlaylist;
    [SerializeField] public AudioClip menuTheme;
    private Stack<int> previousIndexes = new Stack<int>();   
    public bool shuffle = false;
    private int songIndex = 0;
    bool isPlaying;
    bool togglePlay = true;

    //public BoxCollider artFrameCollider;

    bool inMenu = true;

    void Start()
    {
        //audioSource = GetComponent<AudioSource>();
        //Ensure the toggle is set to true for the music to play at start-up
        //isPlaying = true;
        audioSource.volume = GlobalSettings.MUSIC_VOLUME;
        Debug.Log("AudioSource Volume is:" + audioSource.volume);
        //audioSource.clip = musicPlaylist[Random.Range(0, musicPlaylist.Length)];
    }

    void Update()
    {
        if (!audioSource.isPlaying)
        {
            PlayNextClip();
        }
    }

    public void PlayNextClip()
    {
        if (inMenu == true)
        {
            PlayTannernetSpace();
        }

        Debug.Log("Now playing the next song... ");
        previousIndexes.Push(songIndex);
        if (shuffle)
        {
            int newSong = Random.Range(0, musicPlaylist.Length);
            //Attempt to find a new song, if it hits the same song just play the next in the list
            if (newSong != songIndex)
            {
                songIndex = newSong;
            } 
            else
            {
                songIndex = (songIndex + 1) % musicPlaylist.Length;
            }
        }
        else
        {
            songIndex = (songIndex + 1) % musicPlaylist.Length;
        }

        audioSource.clip = musicPlaylist[songIndex];
        Debug.Log(audioSource.clip.name);
        audioSource.Play();
    }

    public void PlayPreviousClip()
    {
        Debug.Log("Playing the previous Clip");
        if (previousIndexes.Count > 0) {
            songIndex = previousIndexes.Pop();    
        } 
        else
        {
            songIndex = 0;
        }
        
        audioSource.clip = musicPlaylist[songIndex];
        audioSource.Play();
    }

    public string getSongName()
    {
        return musicPlaylist[songIndex].name;
    }

    //Simple setter to see if we are in a menu
    public void setInMenu(bool inMenu)
    {
        this.inMenu = inMenu;
    }

    public bool getInMenu()
    {
        return inMenu;
    }
    public void StartMuseumPlaylist()
    {
        PlayNextClip();
    }

    public void PlayTannernetSpace()
    {
        audioSource.clip = menuTheme;
    }

    public void SetAudioVolume(float vol)
    {
        audioSource.volume = vol;
    }

}
