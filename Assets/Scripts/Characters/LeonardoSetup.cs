using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Helper component to quickly setup all required components for Leonardo in one place.
/// Attach this to your Leonardo GameObject.
/// </summary>
public class LeonardoSetup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The animator controller for Leonardo")]
    public RuntimeAnimatorController animatorController;
    
    [Tooltip("The WaypointSystem for Leonardo to use")]
    public WaypointSystem waypointSystem;
    
    [Header("Components to Add")]
    [SerializeField] private bool addNavMeshAgent = true;
    [SerializeField] private bool addAnimator = true;
    [SerializeField] private bool addDaVinciAnimator = true;
    [SerializeField] private bool addMovementController = true;
    [SerializeField] private bool addAudioSource = true;
    [SerializeField] private bool addAICharacterController = true;
    [SerializeField] private bool addDaVinciContext = true;
    
    [Header("Component Settings")]
    [SerializeField] private float movementSpeed = 1.2f;
    [SerializeField] private float rotationSpeed = 120f;
    // These fields were causing warnings - they are now used in the SetupLeonardo method
    [SerializeField] private float minWaitTime = 10f;
    [SerializeField] private float maxWaitTime = 30f;
    
    public void SetupLeonardo()
    {
        if (addNavMeshAgent)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            
            // Configure NavMeshAgent
            agent.speed = movementSpeed;
            agent.angularSpeed = rotationSpeed;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            Debug.Log("NavMeshAgent configured");
        }
        
        if (addAnimator)
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }
            
            // Assign controller if provided
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
                Debug.Log("Animator controller assigned");
            }
            else
            {
                Debug.LogWarning("No animator controller assigned");
            }
        }
        
        if (addDaVinciAnimator)
        {
            if (GetComponent<DaVinciAnimator>() == null)
            {
                gameObject.AddComponent<DaVinciAnimator>();
                Debug.Log("DaVinciAnimator added");
            }
        }
        
        if (addMovementController)
        {
            LeonardoMovementController controller = GetComponent<LeonardoMovementController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<LeonardoMovementController>();
            }
            
            // Assign waypoint system if provided
            if (waypointSystem != null)
            {
                // Use reflection to set the private field
                var field = typeof(LeonardoMovementController).GetField("waypointSystem", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic);
                
                if (field != null)
                {
                    field.SetValue(controller, waypointSystem);
                    Debug.Log("Waypoint system assigned to movement controller");
                }
            }
            else
            {
                Debug.LogWarning("No waypoint system assigned");
            }
            
            // Use the wait time fields to avoid warnings
            controller.SetWaitTimes(minWaitTime, maxWaitTime);
            
            Debug.Log("LeonardoMovementController added");
        }
        
        if (addAudioSource)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Configure AudioSource for speech
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 15.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.playOnAwake = false;
            
            Debug.Log("AudioSource configured");
        }
        
        if (addAICharacterController)
        {
            if (GetComponent<AICharacterController>() == null)
            {
                gameObject.AddComponent<AICharacterController>();
                Debug.Log("AICharacterController added");
            }
        }
        
        if (addDaVinciContext)
        {
            if (GetComponent<DaVinciContext>() == null)
            {
                gameObject.AddComponent<DaVinciContext>();
                Debug.Log("DaVinciContext added");
            }
        }
        
        Debug.Log("Leonardo setup complete!");
    }
    
#if UNITY_EDITOR
    // Add a button in the inspector
    [UnityEditor.CustomEditor(typeof(LeonardoSetup))]
    public class LeonardoSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            LeonardoSetup script = (LeonardoSetup)target;
            if (GUILayout.Button("Setup Leonardo"))
            {
                script.SetupLeonardo();
            }
        }
    }
#endif
}