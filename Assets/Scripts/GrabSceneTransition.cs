using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabSceneTransition : MonoBehaviour
{
    public string sceneToLoad = "YourNextSceneName";
    public float transitionDelay = 1.0f; // Optional delay before loading new scene
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    
    void Start()
    {
        // Get the XRGrabInteractable component
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        if (grabInteractable == null)
        {
            Debug.LogError("No XRGrabInteractable found on this object!");
            return;
        }
        
        // Subscribe to the selectEntered event (triggered when object is grabbed)
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }
    
    public void OnGrabbed(SelectEnterEventArgs args)
    {
        // Start the scene transition
        StartCoroutine(LoadSceneAfterDelay());
    }
    
    IEnumerator LoadSceneAfterDelay()
    {
        // Optional: Add visual/audio feedback here
        
        // Wait for specified delay
        yield return new WaitForSeconds(transitionDelay);
        
        // Load the new scene
        SceneManager.LoadScene(sceneToLoad);
    }
    
    void OnDestroy()
    {
        // Clean up event subscription when object is destroyed
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }
}