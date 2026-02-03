// Handles saving and loading game progress using PlayerPrefs.

using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private const string HAS_SAVE = "HAS_SAVE";
    private const string LEVEL_INDEX = "LEVEL_INDEX";
    private const string ROWS = "ROWS";
    private const string COLS = "COLS";
    private const string SCORE = "SCORE";
    private const string TURNS = "TURNS";
    private const string CARD_COUNT = "CARD_COUNT";
    private const string CARD_PREFIX = "CARD_";

    public static void SaveGame(int levelIndex, int rows, int cols, int score, int turns, List<CardView> cards)
    {
        PlayerPrefs.SetInt(HAS_SAVE, 1);
        PlayerPrefs.SetInt(LEVEL_INDEX, levelIndex);
        PlayerPrefs.SetInt(ROWS, rows);
        PlayerPrefs.SetInt(COLS, cols);
        PlayerPrefs.SetInt(SCORE, score);
        PlayerPrefs.SetInt(TURNS, turns);
    
        // Save card states
        PlayerPrefs.SetInt(CARD_COUNT, cards.Count);
    
        Debug.Log($"[SaveManager] Saving {cards.Count} cards");
        for (int i = 0; i < cards.Count; i++)
        {
            string key = $"{CARD_PREFIX}{i}";
            // Save: 0 = face down, 1 = face up (unmatched), 2 = matched
            int state = 0;
            if (cards[i] != null)
            {
                if (cards[i].IsMatched)
                    state = 2;
                else if (cards[i].IsFaceUp)
                    state = 1;
            
                Debug.Log($"[SaveManager] Card {i}: FaceId={cards[i].FaceId}, State={state}, IsMatched={cards[i].IsMatched}, IsFaceUp={cards[i].IsFaceUp}");
            }
            PlayerPrefs.SetInt(key, state);
        
            // Save face ID for each card
            if (cards[i] != null)
            {
                PlayerPrefs.SetInt($"{key}_FACEID", cards[i].FaceId);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("[SaveManager] Game saved with card states");
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
        out int turns,
        out List<int> cardStates,
        out List<int> cardFaceIds
    )
    {
        levelIndex = PlayerPrefs.GetInt(LEVEL_INDEX, 0);
        rows = PlayerPrefs.GetInt(ROWS, 2);
        cols = PlayerPrefs.GetInt(COLS, 2);
        score = PlayerPrefs.GetInt(SCORE, 0);
        turns = PlayerPrefs.GetInt(TURNS, 0);
        
        // Load card states
        int cardCount = PlayerPrefs.GetInt(CARD_COUNT, 0);
        cardStates = new List<int>(cardCount);
        cardFaceIds = new List<int>(cardCount);
        
        for (int i = 0; i < cardCount; i++)
        {
            string key = $"{CARD_PREFIX}{i}";
            cardStates.Add(PlayerPrefs.GetInt(key, 0));
            cardFaceIds.Add(PlayerPrefs.GetInt($"{key}_FACEID", 0));
        }
    }

    public static void ClearSave()
    {
        // Clear card data too
        int cardCount = PlayerPrefs.GetInt(CARD_COUNT, 0);
        for (int i = 0; i < cardCount; i++)
        {
            PlayerPrefs.DeleteKey($"{CARD_PREFIX}{i}");
            PlayerPrefs.DeleteKey($"{CARD_PREFIX}{i}_FACEID");
        }
        
        PlayerPrefs.DeleteKey(CARD_COUNT);
        PlayerPrefs.DeleteKey(HAS_SAVE);
        Debug.Log("[SaveManager] Save cleared");
    }
}