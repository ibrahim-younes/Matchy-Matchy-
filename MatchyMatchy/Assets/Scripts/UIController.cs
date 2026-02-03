// Controls all UI elements, menus, and HUD updates based on the game state.

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatchGameManager game;

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text turnsText;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameplayRootToDisable; // BoardContainer/Game Grid root
    [SerializeField] private int mainMenuSceneIndex = 0;

    [Header("Well Done UI")]
    [SerializeField] private Canvas gameplayCanvas; // disable when game finishes
    [SerializeField] private GameObject wellDoneObject;

    private bool paused;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (wellDoneObject != null) wellDoneObject.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.enabled = true;
    }

    private void OnEnable()
    {
        if (game == null) return;

        game.OnStatsChanged += HandleStatsChanged;
        game.OnGameOver += HandleGameOver;
        game.OnNewLevel += HandleNewLevel;
    }

    private void OnDisable()
    {
        if (game == null) return;

        game.OnStatsChanged -= HandleStatsChanged;
        game.OnGameOver -= HandleGameOver;
        game.OnNewLevel -= HandleNewLevel;
    }

    private void HandleNewLevel()
    {
        // ensure normal UI state on new level
        if (gameplayCanvas != null) gameplayCanvas.enabled = true;
        if (wellDoneObject != null) wellDoneObject.SetActive(false);
        SetPaused(false);
    }

    private void HandleStatsChanged(int score, int turns)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
        if (turnsText != null) turnsText.text = $"TURNS: {turns}";
    }

    private void HandleGameOver()
    {
        if (gameplayCanvas != null) gameplayCanvas.enabled = false;
        if (wellDoneObject != null) wellDoneObject.SetActive(true);
    }

    // ---- UI Buttons ----

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void SaveAndQuitToMainMenu()
    {
        if (game != null)
        {
            game.GetSaveData(out int levelIndex, out int rows, out int cols, out int score, out int turns);
            SaveManager.SaveGame(levelIndex, rows, cols, score, turns);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneIndex);
    }

    public void ContinueNextLevel()
    {
        if (gameplayCanvas != null) gameplayCanvas.enabled = true;
        if (wellDoneObject != null) wellDoneObject.SetActive(false);
        game?.StartNextLevel();
    }

    // ---- Pause logic ----

    private void SetPaused(bool value)
    {
        paused = value;

        game?.SetPaused(value);

        if (gameplayRootToDisable != null)
            gameplayRootToDisable.SetActive(!value);

        if (pausePanel != null)
            pausePanel.SetActive(value);

        Time.timeScale = value ? 0f : 1f;
    }
    
}