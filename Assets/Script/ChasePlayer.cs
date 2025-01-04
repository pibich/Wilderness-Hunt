using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChasePlayer : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 25f; // Speed of rotation

    private Transform player; // To store the player's Transform
    private Animator animator; // To store the Animator
    private NavMeshAgent navMeshAgent;

    private void Start()
    {
        // Cache the NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Try to find the player GameObject by name
        FindPlayer();

        // Get the Animator component from the parent object
        animator = GetComponentInParent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on the parent object!");
        }

        // Ensure NavMeshAgent is configured properly
        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false; // Disable automatic NavMesh rotation
        }
    }

    private void Update()
    {
        // Retry finding the player if it is still null
        if (player == null)
        {
            FindPlayer();
            if (player == null) return; // Exit early if still not found
        }

        // Calculate the distance between the zombie and the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if the zombie is in the "Idle" animation state
        if (animator != null)
        {
            bool isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Z_Idle");
        }

        RotateTowardsPlayer();
        navMeshAgent.updateRotation = true;
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;

        // Disable NavMeshAgent rotation to manually rotate
        navMeshAgent.updateRotation = false;

        // Calculate direction to the player
        Vector3 direction = (player.position - transform.position).normalized;

        // Determine the rotation step based on speed
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player GameObject not found. Retrying...");
        }
    }
}
