// Static class responsible for saving and loading game progress using Unity's PlayerPrefs.

using UnityEngine;
public static class SaveManager
{
    // PlayerPrefs keys (constants to avoid typos)
    private const string HAS_SAVE = "HAS_SAVE";     // Flag to check if a save exists
    private const string LEVEL_INDEX = "LEVEL_INDEX"; // Current level index
    private const string ROWS = "ROWS";             // Grid rows count
    private const string COLS = "COLS";             // Grid columns count
    private const string SCORE = "SCORE";           // Player score
    private const string TURNS = "TURNS";           // Number of turns taken

    // Saves the current game state into PlayerPrefs.
    public static void SaveGame(
        int levelIndex,
        int rows,
        int cols,
        int score,
        int turns
    )
    {
        // Mark that a save file exists
        PlayerPrefs.SetInt(HAS_SAVE, 1);

        // Store all game-related values
        PlayerPrefs.SetInt(LEVEL_INDEX, levelIndex);
        PlayerPrefs.SetInt(ROWS, rows);
        PlayerPrefs.SetInt(COLS, cols);
        PlayerPrefs.SetInt(SCORE, score);
        PlayerPrefs.SetInt(TURNS, turns);

        // Force PlayerPrefs to write data to disk
        PlayerPrefs.Save();

        // Debug message for confirmation
        Debug.Log("Game Saved");
    }

    // Checks whether a saved game exists.
    public static bool HasSave()
    {
        // Returns true only if HAS_SAVE key is set to 1
        return PlayerPrefs.GetInt(HAS_SAVE, 0) == 1;
    }

    // Loads the saved game values from PlayerPrefs.
    public static void LoadGame(
        out int levelIndex,
        out int rows,
        out int cols,
        out int score,
        out int turns
    )
    {
        // Load values, using default fallbacks if keys do not exist
        levelIndex = PlayerPrefs.GetInt(LEVEL_INDEX, 0);
        rows = PlayerPrefs.GetInt(ROWS, 2);
        cols = PlayerPrefs.GetInt(COLS, 2);
        score = PlayerPrefs.GetInt(SCORE, 0);
        turns = PlayerPrefs.GetInt(TURNS, 0);
    }

    // Clears the saved game by removing the save flag.
    public static void ClearSave()
    {
        // Removing this key effectively disables the save
        PlayerPrefs.DeleteKey(HAS_SAVE);
    }
}
