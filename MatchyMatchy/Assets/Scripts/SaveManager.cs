// Handles saving and loading game progress using PlayerPrefs.

using UnityEngine;

public static class SaveManager
{
    private const string HAS_SAVE = "HAS_SAVE";
    private const string LEVEL_INDEX = "LEVEL_INDEX";
    private const string ROWS = "ROWS";
    private const string COLS = "COLS";
    private const string SCORE = "SCORE";
    private const string TURNS = "TURNS";

    public static void SaveGame(int levelIndex, int rows, int cols, int score, int turns)
    {
        PlayerPrefs.SetInt(HAS_SAVE, 1);
        PlayerPrefs.SetInt(LEVEL_INDEX, levelIndex);
        PlayerPrefs.SetInt(ROWS, rows);
        PlayerPrefs.SetInt(COLS, cols);
        PlayerPrefs.SetInt(SCORE, score);
        PlayerPrefs.SetInt(TURNS, turns);

        PlayerPrefs.Save();
        Debug.Log("[SaveManager] Game saved");
    }

    public static bool HasSave()
    {
        return PlayerPrefs.GetInt(HAS_SAVE, 0) == 1;
    }

    public static void LoadGame(
        out int levelIndex,
        out int rows,
        out int cols,
        out int score,
        out int turns
    )
    {
        levelIndex = PlayerPrefs.GetInt(LEVEL_INDEX, 0);
        rows       = PlayerPrefs.GetInt(ROWS, 2);
        cols       = PlayerPrefs.GetInt(COLS, 2);
        score      = PlayerPrefs.GetInt(SCORE, 0);
        turns      = PlayerPrefs.GetInt(TURNS, 0);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(HAS_SAVE);
        Debug.Log("[SaveManager] Save cleared");
    }
}