using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for using UI elements
using TMPro;

public class FlashlightController : MonoBehaviour
{
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;

    private Light flashlight;
    private AudioSource audioSource;

    // Reference to the player's camera (assign in the Inspector or dynamically)
    public Transform playerCamera;

    // Battery-related fields
    [SerializeField] private float maxBattery = 100f;
    // [SerializeField] private float batteryDrainRate = 1f; // Battery drain per second
    // [SerializeField] private float batteryDrainModifier = 1f; // Battery drain modifier

    [SerializeField] private float drainInterval = 4f;
    private float timeSinceLastDrain = 0f;
    private float currentBattery;

    // UI element to display the battery percentage
    [SerializeField] private TextMeshProUGUI batteryUIText;

    void Start()
    {
        flashlight = GetComponent<Light>();
        if (flashlight == null)
        {
            Debug.LogWarning("Light Component is not attached. Attach a Light component manually.");
        }
        else
        {
            flashlight.enabled = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Player Camera is not assigned. Please assign it in the Inspector.");
        }

        // Initialize battery
        currentBattery = maxBattery;
        UpdateBatteryUI();
    }

    void Update()
    {
        // Toggle flashlight with the "F" key
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (flashlight != null)
            {
                if (currentBattery > 0)
                {
                    flashlight.enabled = !flashlight.enabled;

                    if (flashlight.enabled)
                    {
                        PlayAudioEffect(turnOnSound);
                    }
                    else
                    {
                        PlayAudioEffect(turnOffSound);
                    }
                }
                else
                {
                    Debug.LogWarning("Battery is empty. Cannot toggle flashlight.");
                }
            }
            else
            {
                Debug.LogWarning("Cannot control flashlight as Light Component is not attached.");
            }
        }

        if (flashlight != null && flashlight.enabled)
        {
            // Adjust the time since last drain based on the modified drain interval
            timeSinceLastDrain += Time.deltaTime;
            // If enough time has passed, drain 1% of the battery
            if (timeSinceLastDrain >= drainInterval)
            {
                DrainBattery(1); // Drain 1% after the specified interval
                timeSinceLastDrain = 0f; // Reset the timer
            }
            // Align flashlight direction with the player's camera direction
            if (playerCamera != null)
            {
                transform.position = playerCamera.position;
                transform.rotation = Quaternion.Euler(playerCamera.rotation.eulerAngles);
            }
        }
    }

    void PlayAudioEffect(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void DrainBattery(float amount)
    {
        if (currentBattery > 0)
        {
            currentBattery -= amount;
            currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);

            if (currentBattery == 0)
            {
                flashlight.enabled = false; // Turn off flashlight if battery is depleted
                PlayAudioEffect(turnOffSound);
            }

            UpdateBatteryUI();
        }
    }


    public void DrainBatteryModifier()
    {
        if (GameManager.instance != null)
        {
            int keysCollected = GameManager.instance.KeysCollected;

            switch (keysCollected)
            {
                case 2:
                    drainInterval -= 1f; // Drain 1% every 3 seconds instead of 4 seconds
                    break;
                case 4:
                    drainInterval -= 0.5f; // Drain 1% every 2.5 seconds
                    break;
                case 7:
                    drainInterval -= 0.5f; // Drain 1% every 2 seconds
                    break;
                case 8:
                    drainInterval -= 1f;
                    break;    
                default:
                    drainInterval = 4f; // Default interval is 4 seconds for 1% drain
                    break;
            }
            // Ensure the interval doesn't go too low
            drainInterval = Mathf.Max(drainInterval, 1f); // Prevent the drain interval from going below 1 second
        }
    }

    void UpdateBatteryUI()
    {
        if (batteryUIText != null)
        {
            batteryUIText.text = $"{Mathf.Ceil(currentBattery)}%";
        }
        else
        {
            Debug.LogWarning("Battery UI Text is not assigned.");
        }
    }

}
