using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectScript : MonoBehaviour
{

    // Insert an array of audios as a serialized field and then on mouse down, make it play a random clip from the array and only allowing it to play once only an audioclip is finished?

    [SerializeField] private AudioClip[] audioClips;
    public AudioSource audioSource;
    private bool isPlaying = false;

    void OnMouseDown()
    {
        // Trigger audio playback only if no audio is currently playing
        if (!isPlaying && audioClips.Length > 0)
        {
            StartCoroutine(PlayRandomClip());
        }
    }

    private IEnumerator PlayRandomClip()
    {
        isPlaying = true;

        // Select a random clip from the array
        int randomIndex = Random.Range(0, audioClips.Length);
        AudioClip selectedClip = audioClips[randomIndex];

        // Play the selected clip
        audioSource.clip = selectedClip;
        audioSource.Play();

        // Wait for the clip to finish playing
        yield return new WaitForSeconds(selectedClip.length);

        // Reset playing state
        isPlaying = false;
    }
}
