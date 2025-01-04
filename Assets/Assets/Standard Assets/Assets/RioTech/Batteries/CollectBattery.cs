using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectBattery : MonoBehaviour
{
    public AudioSource audioSource;
    private bool isCollected = false;

    void OnMouseDown()
    {
        if (!isCollected)
        {
            isCollected = true;

            audioSource.Play();

            // Destroy key after the sound finishes playing
            Destroy(gameObject, audioSource.clip.length);
        }
    }
}
