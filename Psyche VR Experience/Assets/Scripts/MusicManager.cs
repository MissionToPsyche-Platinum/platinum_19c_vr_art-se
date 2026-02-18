using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] public AudioClip[] musicPlaylist;
    private Stack<int> previousIndexes;
    public bool shuffle = false;
    private int songIndex = 0;
    bool isPlaying;
    bool togglePlay = true;

    public SphereCollider artFrameCollider;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        //Ensure the toggle is set to true for the music to play at start-up
        isPlaying = true;
        audioSource.volume = GlobalSettings.MUSIC_VOLUME;
        audioSource.clip = musicPlaylist[songIndex];
        previousIndexes = new Stack<int>();
    }

    void Update()
    {
        //Check to see if you just set the toggle to positive
        if (isPlaying == true && togglePlay == true)
        {
            Debug.Log("Started playing");
            audioSource.Play();
            //Ensure audio doesn’t play more than once
            togglePlay = false;
        }
        //Check if you just set the toggle to false
        if (isPlaying == false && togglePlay == true)
        {
            Debug.Log("Stopped Playing");
            audioSource.Stop();
            //Ensure audio doesn’t play more than once
            togglePlay = false;
        }

        if (!audioSource.isPlaying && togglePlay == false)
        {
            PlayNextClip();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == artFrameCollider)
        {
            MusicFadeOut();
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider == artFrameCollider)
        {
            MusicFadeIn();
        }
    }

    public void PlayNextClip()
    {
        Debug.Log("Now playing the next song... ");
        previousIndexes.Push(songIndex);
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

    public void PlayPreviousClip()
    {
        Debug.Log("Playing the previous Clip");
        songIndex = previousIndexes.Pop();
        audioSource.clip = musicPlaylist[songIndex];
        audioSource.Play();
    }
    public string getSongName()
    {
        return musicPlaylist[songIndex].name;
    }

    private IEnumerator MusicFadeOut()
    {
        Debug.Log("Music is fading out");
        while (audioSource.volume > 0)
        {
            yield return new WaitForSeconds(0.1f);
            audioSource.volume -= 1;
        }
    }

    private IEnumerator MusicFadeIn()
    {
        Debug.Log("Music is fading in");
        while (audioSource.volume < GlobalSettings.MUSIC_VOLUME) 
        {
            yield return new WaitForSeconds(0.1f);
            audioSource.volume += 1;    
        }
    }
}
