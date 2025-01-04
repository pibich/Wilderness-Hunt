using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectKey : MonoBehaviour
{
    public AudioSource audioSource;
    private bool isCollected = false;

    void OnMouseDown()
    {
        if (!isCollected)
        {
            isCollected = true;

            audioSource.Play();

            GameManager.instance.CollectKey();
        }
    }
}
