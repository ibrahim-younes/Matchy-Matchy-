// Handles main menu actions such as Play, Continue, and Quit. Also controls the visibility of the Continue button based on save data.

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private int gameSceneBuildIndex = 1;

    [Header("Continue Button")]
    [SerializeField] private GameObject continueButton;

    private void Start()
    {
        // Enable the Continue button only if a saved game exists
        if (continueButton != null)
            continueButton.SetActive(SaveManager.HasSave());
    }
    
    public void PlayGame()
    {
        // Remove previous save to start fresh
        SaveManager.ClearSave();

        // Load the main game scene
        SceneManager.LoadScene(gameSceneBuildIndex);
    }

   
    // Continues the game from the last saved state.
    public void ContinueGame()
    {
        // Safety check in case the button is triggered without a save
        if (!SaveManager.HasSave())
        {
            Debug.Log("No save found");
            return;
        }

        // Load the main game scene (data will be loaded there)
        SceneManager.LoadScene(gameSceneBuildIndex);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}