using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI Elements")]
    public Text keyText;

    [Header("Spawners and Collectible")]
    public GameObject[] spawners; // Array of spawner GameObjects
    public GameObject keyPrefab;  // Prefab for the collectible key

    [Header("Exit Portal")]
    public GameObject portalPrefab;
    public Transform[] portalPositions;
    private GameObject portalInstance;

    public FlashlightController flashlightController;

    public GameObject pauseMenuUI;

    public GameObject gameOverUI;

    public bool isGamePaused { get; set; } = false;

    public bool isGameLost { get; set; } = false;

    private HashSet<int> visitedSpawners = new HashSet<int>(); // Tracks visited spawners
    private GameObject currentKey; // Tracks the current key instance
    private int currentSpawnerIndex = -1; // Tracks the current spawner index

    private int keysCollected = 0;
    private int totalKeys = 8;

    private Color32 dimYellow = new Color32(255, 153, 0, 255);

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        gameOverUI.SetActive(false);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        UpdateKeyUI();
        SpawnKey();
    }

    public void CollectKey()
    {
        // Increment the collected key count
        keysCollected++;
        UpdateKeyUI();

        // Set the light of the current spawner to visited (red and dim)
        if (currentSpawnerIndex != -1)
        {
            SetSpawnerLight(currentSpawnerIndex, Color.red, 0.5f, true, 1.85f, 2.05f);
        }

        // Destroy the current key and spawn a new one
        if (currentKey != null)
        {
            Destroy(currentKey);
        }

        if (flashlightController != null)
        {
            flashlightController.DrainBatteryModifier();
        }

        if (keysCollected < totalKeys)
        {
            SpawnKey();
        }

        if (keysCollected >= totalKeys && portalInstance == null)
        {
            SpawnExitPortal();
        }
    }

    void Update()
    {
        // Only allow pause/resume if the game is not lost
        if (!isGameLost && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }


    public void PauseGame()
    {
        if (isGameLost != true)
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            isGamePaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            return;
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure the game is unfrozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the current scene
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Ensure the game is unfrozen
        SceneManager.LoadScene("MainMenu"); // Load the main menu scene
    }

    public void VictoryScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TestPlane");
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        // Apply absolute freeze which cannot be unpaused.
        // Show the UI for game over, and the two buttons to restart / main menu.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isGamePaused = true;
        isGameLost = true;

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        gameOverUI.SetActive(true);
    }

    void UpdateKeyUI()
    {
        keyText.text = $"{keysCollected}/{totalKeys}";
    }

    private void SpawnExitPortal()
    {
        // Ensure there is at least one position in the array
        if (portalPositions.Length == 0)
        {
            Debug.LogError("No portal positions assigned!");
            return;
        }

        // Choose a random position from the available ones
        Transform randomPosition = portalPositions[Random.Range(0, portalPositions.Length)];

        // Instantiate the portal at the chosen position
        portalInstance = Instantiate(portalPrefab, randomPosition.position, Quaternion.identity);

        // Optionally, you can also add logic to make the portal visible or activate its behavior
        Debug.Log("Exit portal spawned at: " + randomPosition.position);
    }

    private void SpawnKey()
    {
        // Check if all spawners have been visited
        if (visitedSpawners.Count >= spawners.Length)
        {
            Debug.Log("All keys collected!");
            return;
        }

        // Choose a random unvisited spawner
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, spawners.Length);
        } while (visitedSpawners.Contains(randomIndex));

        // Mark the spawner as visited
        visitedSpawners.Add(randomIndex);
        currentSpawnerIndex = randomIndex;

        // Activate the light for the chosen spawner
        SetSpawnerLight(randomIndex, Color.white, 1f, true, 2.25f, 5.55f);

        // Spawn the key at the chosen spawner's position
        GameObject spawner = spawners[randomIndex];
        Transform spawnPoint = spawner.transform.Find("ItemPosition"); // The empty GameObject for positioning
        if (spawnPoint != null)
        {
            currentKey = Instantiate(keyPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"Spawner {spawner.name} is missing an 'ItemPosition' child!");
        }
    }

    private void SetSpawnerLight(int spawnerIndex, Color lightColor, float lightIntensity, bool enableLight, float range = 10f, float intensity = 1f)
    {
        // Retrieve the Light component from the LightObject in the spawner
        Light spawnerLight = spawners[spawnerIndex].transform.Find("Lamp/LightObject").GetComponent<Light>();

        if (spawnerLight != null)
        {
            // Set light properties
            spawnerLight.color = lightColor;
            spawnerLight.intensity = enableLight ? intensity : 0f; // Use the given intensity when enabled
            spawnerLight.range = range;                           // Update the range
            spawnerLight.enabled = enableLight;
        }
        else
        {
            Debug.LogError($"Light component not found in spawner {spawnerIndex}");
        }
    }

    // Export data to flashlight as getter.
    public int KeysCollected
    {
        get { return keysCollected; }
    }
}
