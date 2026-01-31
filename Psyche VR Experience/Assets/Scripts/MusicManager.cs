using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] private AudioClip[] musicPlaylist;

    public bool shuffle = false;
    private int songIndex = 0;
    bool isPlaying;
    bool togglePlay;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        //Ensure the toggle is set to true for the music to play at start-up
        isPlaying = true;
    }

    void Update()
    {
        //Check to see if you just set the toggle to positive
        if (isPlaying == true && togglePlay == true)
        {
            audioSource.Play();
            //Ensure audio doesn’t play more than once
            togglePlay = false;
        }
        //Check if you just set the toggle to false
        if (isPlaying == false && togglePlay == true)
        {
            audioSource.Stop();
            //Ensure audio doesn’t play more than once
            togglePlay = false;
        }

        if (!audioSource.isPlaying && togglePlay)
        {
            PlayNextClip();
        }
    }

    void OnGUI()
    {
        //Switch this toggle to activate and deactivate the parent GameObject
        isPlaying = GUI.Toggle(new Rect(10, 10, 100, 30), isPlaying, "Play Music");

        //Detect if there is a change with the toggle
        if (GUI.changed)
        {
            //Change to true to show that there was just a change in the toggle state
            togglePlay = true;
        }
    }

    void PlayNextClip()
    {
       Debug.Log("Now playing the next song... ");
        if (shuffle)
        {
            songIndex = Random.Range(0, musicPlaylist.Length);
        }
        else
        {
            songIndex = (songIndex + 1) % musicPlaylist.Length;
        }

        audioSource.clip = musicPlaylist[songIndex];
        Debug.Log(audioSource.clip.name);
        audioSource.Play();
    }
}
