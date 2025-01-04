using System.Collections;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject prefabToSpawn; // The prefab to spawn
    [SerializeField] private float spawnRadius = 5f;   // Radius within which to spawn
    [SerializeField] private float spawnInterval = 2f; // Time interval between spawns
    [SerializeField] private int maxSpawnCount = 10;   // Maximum number of prefabs to spawn
    [SerializeField] private bool spawnOnStart = true; // Should spawning start automatically?

    private int currentSpawnCount = 0; // Track the number of spawned objects

    private void Start()
    {
        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    /// <summary>
    /// Starts the spawning coroutine.
    /// </summary>
    public void StartSpawning()
    {
        StartCoroutine(SpawnObjects());
    }

    /// <summary>
    /// Stops the spawning coroutine.
    /// </summary>
    public void StopSpawning()
    {
        StopCoroutine(SpawnObjects());
    }

    private IEnumerator SpawnObjects()
    {
        while (currentSpawnCount < maxSpawnCount)
        {
            SpawnPrefab();
            currentSpawnCount++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// Spawns the prefab at a random position within the radius.
    /// </summary>
    private void SpawnPrefab()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("Prefab to spawn is not assigned!");
            return;
        }

        // Generate a random position within the radius
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = transform.position.y; // Maintain the y-axis (ground level)

        // Instantiate the prefab
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }

    /// <summary>
    /// Resets the spawn count to allow for more spawns.
    /// </summary>
    public void ResetSpawner()
    {
        currentSpawnCount = 0;
    }

    // Gizmos to visualize the spawn radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
