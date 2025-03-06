using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Load the game scene
        SceneManager.LoadScene("Lab");
    }

    public void QuitGame()
    {
        // Quit the game
        Application.Quit();
    }
}

