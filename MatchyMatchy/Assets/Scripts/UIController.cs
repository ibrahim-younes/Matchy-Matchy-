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
    [SerializeField] private RectTransform wellDonePanelRect;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popDuration = 0.22f;

    private Coroutine popRoutine;
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
        StopPop();
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
        StartPop();
    }

    // ---- UI Buttons ----

    // Pause button OnClick()
    public void Pause()
    {
        SetPaused(true);
    }

    // Resume button OnClick()
    public void Resume()
    {
        SetPaused(false);
    }

    // Save & Quit button OnClick()
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

    // Continue button on Well Done panel
    public void ContinueNextLevel()
    {
        if (gameplayCanvas != null) gameplayCanvas.enabled = true;
        if (wellDoneObject != null) wellDoneObject.SetActive(false);
        StopPop();

        game?.StartNextLevel();
    }

    // ---- Pause logic ----

    private void SetPaused(bool value)
    {
        paused = value;

        // Tell game logic to ignore input
        game?.SetPaused(value);

        // Disable gameplay objects so UI buttons are easy to click
        if (gameplayRootToDisable != null)
            gameplayRootToDisable.SetActive(!value);

        if (pausePanel != null)
            pausePanel.SetActive(value);

        Time.timeScale = value ? 0f : 1f;
    }

    // ---- Pop animation ----

    private void StartPop()
    {
        if (wellDonePanelRect == null) return;
        StopPop();
        popRoutine = StartCoroutine(Pop());
    }

    private void StopPop()
    {
        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }
        if (wellDonePanelRect != null)
            wellDonePanelRect.localScale = Vector3.one;
    }

    private IEnumerator Pop()
    {
        Vector3 start = Vector3.one;
        Vector3 peak = Vector3.one * Mathf.Max(1f, popScale);
        float half = Mathf.Max(0.01f, popDuration * 0.5f);

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            wellDonePanelRect.localScale = Vector3.Lerp(start, peak, t / half);
            yield return null;
        }

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            wellDonePanelRect.localScale = Vector3.Lerp(peak, start, t / half);
            yield return null;
        }

        wellDonePanelRect.localScale = Vector3.one;
        popRoutine = null;
    }
}