using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // This class coordinates scene transitions and persists data between scenes
    
    // Static instance for easy access
    public static SceneTransitionManager Instance;
    
    // Track current and next scenes
    private string currentScene;
    private string nextScene;
    
    private void Awake()
    {
        // Singleton pattern - persist between scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Store current scene name
            currentScene = SceneManager.GetActiveScene().name;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Method to transition to the wormhole scene with destination info
    public void TransitionViaWormhole(string destination)
    {
        // Store destination in PlayerPrefs for the wormhole scene to read
        PlayerPrefs.SetString("DestinationScene", destination);
        PlayerPrefs.Save();
        
        // Store next scene for reference
        nextScene = destination;
        
        // Load the wormhole scene
        SceneManager.LoadScene("CutSceneWorm");
    }
    
    // Direct transition to another scene (without wormhole)
    public void TransitionToScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene(sceneName);
    }
    
    // Get opposite scene (Lab <-> Workshop)
    public string GetOppositeScene()
    {
        currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == "Lab")
        {
            return "Workshop";
        }
        else if (currentScene == "Workshop")
        {
            return "Lab";
        }
        
        // Default
        return "Lab";
    }
}