using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public partial class LeonardoMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float minWaitTime = 10f;
    [SerializeField] private float maxWaitTime = 15f;
    [SerializeField] private float movementSpeed = 1.0f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float interactionPauseTime = 15f;
    [SerializeField] private float checkMovementInterval = 0.1f; // How often to check if actually moving
    
    [Header("References")]
    [SerializeField] private WaypointSystem waypointSystem;
    
    // Components
    private NavMeshAgent navAgent;
    private DaVinciAnimator animator;
    private bool isMoving = false;
    private bool isInteracting = false;
    private bool isPaused = false;
    private Vector3 lastPosition;
    private float movementThreshold = 0.01f; // Minimum distance to consider as moving
    
    private void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<DaVinciAnimator>();
        
        if (navAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
            enabled = false;
            return;
        }
        
        // Configure NavMeshAgent
        navAgent.speed = movementSpeed;
        navAgent.angularSpeed = rotationSpeed;
        navAgent.stoppingDistance = 0.5f;
        
        // Store initial position
        lastPosition = transform.position;
        
        // Try to find waypoint system if not assigned
        if (waypointSystem == null)
        {
            // Update from FindObjectOfType to FindFirstObjectByType as recommended
            waypointSystem = Object.FindFirstObjectByType<WaypointSystem>();
            if (waypointSystem == null)
            {
                Debug.LogWarning("No WaypointSystem found in scene. Random movement will be used.");
            }
        }
        
        // Subscribe to animation events
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger += HandleAnimationMarker;
        }
        
        // Start movement routine
        StartCoroutine(MovementRoutine());
        
        // Start continuous movement check for animation
        StartCoroutine(ContinuousMovementCheck());
    }
    
    // New coroutine to continuously check if actually moving
    private IEnumerator ContinuousMovementCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(checkMovementInterval);
        
        while (true)
        {
            // Check if actually moving by comparing positions
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            bool actuallyMoving = distanceMoved > movementThreshold;
            
            // If we're supposed to be moving but not actually moving, make sure animation is correct
            if (isMoving != actuallyMoving)
            {
                isMoving = actuallyMoving;
                
                // Update animation state
                if (animator != null)
                {
                    animator.SetWalking(isMoving);
                    Debug.Log($"Movement status updated: {(isMoving ? "Walking" : "Stopped")}");
                }
            }
            
            // Update last position
            lastPosition = transform.position;
            
            yield return wait;
        }
    }
    
    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            // Don't move if interacting with user
            if (isInteracting || isPaused)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            // Pick a destination
            Vector3 destination;
            if (waypointSystem != null && waypointSystem.GetWaypointCount() > 0)
            {
                // Use waypoint
                Transform waypoint = waypointSystem.GetRandomWaypoint();
                destination = waypoint.position;
            }
            else
            {
                // Use random position
                destination = GetRandomNavMeshPosition(5f);
            }
            
            // Move to destination
            MoveToPosition(destination);
            
            // Wait until arrival or interruption
            while (isMoving && !isInteracting && !isPaused)
            {
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    StopMoving();
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            
            // Pause at destination if we arrived successfully
            if (!isInteracting && !isPaused)
            {
                float waitTime = Random.Range(minWaitTime, maxWaitTime);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
    
    private void MoveToPosition(Vector3 position)
    {
        navAgent.SetDestination(position);
        isMoving = true;
        
        // Set animation
        if (animator != null)
        {
            animator.SetWalking(true);
            Debug.Log("Setting walking animation to TRUE");
        }
    }
    
    private void StopMoving()
    {
        isMoving = false;
        
        // Stop animation
        if (animator != null)
        {
            animator.SetWalking(false);
            Debug.Log("Setting walking animation to FALSE");
        }
    }
    
    private Vector3 GetRandomNavMeshPosition(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas);
        
        return hit.position;
    }
    
    private void HandleAnimationMarker(string marker)
    {
        // User is interacting with Leonardo
        isInteracting = true;
        
        // Stop moving
        if (isMoving)
        {
            StopMoving();
            navAgent.ResetPath();
        }
        
        // Schedule end of interaction
        StartCoroutine(EndInteractionAfterDelay());
    }
    
    private IEnumerator EndInteractionAfterDelay()
    {
        yield return new WaitForSeconds(interactionPauseTime);
        isInteracting = false;
    }
    
    // Public method to manually pause/resume movement
    public void SetPaused(bool pause)
    {
        isPaused = pause;
        if (pause && isMoving)
        {
            StopMoving();
            navAgent.ResetPath();
        }
    }
    
    // Public method to manually move to a position
    public void GoToPosition(Vector3 position)
    {
        isInteracting = false;
        isPaused = false;
        
        StopAllCoroutines();
        MoveToPosition(position);
        
        // Restart the movement routine after we arrive
        StartCoroutine(ResumeRoutineAfterArrival());
        
        // Also restart the movement check
        StartCoroutine(ContinuousMovementCheck());
    }
    
    private IEnumerator ResumeRoutineAfterArrival()
    {
        // Wait until arrival
        while (isMoving)
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                StopMoving();
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }
        
        // Restart main routine
        StartCoroutine(MovementRoutine());
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger -= HandleAnimationMarker;
        }
        
        // Stop all coroutines
        StopAllCoroutines();
    } 
}

// Second part of the partial class
public partial class LeonardoMovementController : MonoBehaviour
{
    // Method to set wait times (fixes warnings in LeonardoSetup.cs)
    public void SetWaitTimes(float min, float max)
    {
        minWaitTime = min;
        maxWaitTime = max;
        Debug.Log($"Wait times set to min: {minWaitTime}, max: {maxWaitTime}");
    }
}