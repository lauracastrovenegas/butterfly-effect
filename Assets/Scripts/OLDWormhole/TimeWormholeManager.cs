using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeWormholeManager : MonoBehaviour
{
    [SerializeField] private string wormholeSceneName = "WormHole";
    
    public void StartTimeTravel()
    {
        // Load the wormhole scene
        SceneManager.LoadScene(wormholeSceneName);
    }
}