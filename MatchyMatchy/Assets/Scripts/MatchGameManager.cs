// Manages the gameplay flow, logic, and state of a memory card matching game.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct GridSize
{
    public int rows, cols;
    public GridSize(int r, int c) { rows = r; cols = c; }
}

public class MatchGameManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private BoardLayoutScaler layoutScaler;
    [SerializeField] private CardView cardPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite backSprite;
    [SerializeField] private List<Sprite> faceSprites;

    [Header("Level Layouts")]
    [SerializeField] private List<GridSize> levels = new()
    {
        new GridSize(2,2), new GridSize(4,4), new GridSize(5,6), new GridSize(3,3),
    };

    [Header("Start Reveal")]
    [SerializeField] private bool revealAllAtStart = true;
    [SerializeField] private float startRevealDuration = 2f;

    [Header("Odd Grid Handling")]
    [SerializeField] private bool allowOddByBlockingOneSlot = true;

    [Header("Reveal / Timing")]
    [SerializeField] private float revealBeforeCompare = 0.35f;
    [SerializeField] private float revealMismatchBeforeFlipDown = 0.55f;

    [Header("Scoring")]
    [SerializeField] private int matchScore = 10;

    [Header("Sound")]
    [SerializeField] private SoundContorller sounds;

    // ===================== PUBLIC PROPERTIES (for UI/other scripts to access) =====================
    public int Score => score;
    public int Turns => turns;
    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentRows => currentRows;
    public int CurrentCols => currentCols;
    public bool IsGameOver => isGameOver;
    public bool IsPaused => isPaused;

    // ===================== EVENTS (for UI updates) =====================
    public System.Action<int, int> OnStatsChanged;  // Called when score or turns change
    public System.Action OnGameOver;                // Called when all cards are matched
    public System.Action OnNewLevel;                // Called when starting a new level

    // ===================== GAME STATE VARIABLES =====================
    private int score, turns;
    private int currentLevelIndex, currentRows, currentCols;
    private bool isGameOver, isPaused;

    // ===================== CARD MANAGEMENT COLLECTIONS =====================
    private readonly List<CardView> all = new();                             // All cards in current level
    private readonly List<CardView> pending = new();                         // Cards flipped up, waiting to be paired
    private readonly Queue<(CardView a, CardView b)> compareQueue = new();   // Pairs waiting for comparison
    private readonly HashSet<CardView> reserved = new();                     // Cards being processed (prevent double-clicks)

    private bool compareWorkerRunning;    // Flag to ensure only one comparison coroutine runs
    private Coroutine startRevealRoutine; // Reference to the start reveal coroutine (so we can stop it)

    private void Start()
    {
        if (levels == null || levels.Count == 0)
            levels = new List<GridSize> { new GridSize(2, 2) };

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);

        // ===================== SAVE SYSTEM INTEGRATION =====================
        // Check if there's saved game data
        if (SaveManager.HasSave())
        {
            // Load saved game state including card positions and states
            SaveManager.LoadGame(out currentLevelIndex, out currentRows, out currentCols, out score, out turns,
                out List<int> savedStates, out List<int> savedFaceIds);

            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
            
            // Start level with saved data (restores exact board state)
            StartLevel(currentRows, currentCols, preserveStats: true, savedStates, savedFaceIds);
        }
        else
        {
            // No save exists, start fresh from first level
            var s = levels[currentLevelIndex];
            StartLevel(s.rows, s.cols, preserveStats: false);
        }
    }

    // ===================== LEVEL FLOW =====================
    public void StartNextLevel()
    {
        if (levels == null || levels.Count == 0) { StartLevel(2, 2, preserveStats: false); return; }
        
        // Cycle through levels (returns to first after last)
        currentLevelIndex = (currentLevelIndex + 1) % levels.Count;
        var s = levels[currentLevelIndex];
        StartLevel(s.rows, s.cols, preserveStats: false);
    }

    public void SetPaused(bool paused) => isPaused = paused;

    private void StartLevel(int rows, int cols, bool preserveStats,
        List<int> savedCardStates = null, List<int> savedCardFaceIds = null)
    {
        // ===================== RESET LEVEL STATE =====================
        isGameOver = false;
        isPaused = false;

        currentRows = rows;
        currentCols = cols;

        // Stop any running routines from previous level
        if (startRevealRoutine != null) StopCoroutine(startRevealRoutine);
        startRevealRoutine = null;

        // ===================== SETUP BOARD WITH SAVED DATA =====================
        ClearBoard();
        SetupBoard(rows, cols, savedCardStates, savedCardFaceIds);

        // Reset stats unless we're loading a saved game
        if (!preserveStats) { score = 0; turns = 0; }

        // Notify UI of new level and current stats
        OnNewLevel?.Invoke();
        OnStatsChanged?.Invoke(score, turns);

        // ===================== HANDLE START REVEAL =====================
        // Only show all cards at start if NOT loading a saved game
        if (revealAllAtStart && savedCardStates == null)
            startRevealRoutine = StartCoroutine(RevealAllCardsAtStart());

        // ===================== RESTORE SAVED GAME STATE =====================
        // If the save restored face-up unmatched cards, queue them for comparison
        // This handles the case where player had 1 or 2 cards face up when they saved
        if (pending.Count >= 2)
            EnsureCompareWorker();
    }

    // ===================== SAVE =====================
    public void GetSaveData(out int levelIndex, out int rows, out int cols,
        out int outScore, out int outTurns, out List<CardView> cards)
    {
        // Export current game state for saving
        levelIndex = currentLevelIndex;
        rows = currentRows;
        cols = currentCols;
        outScore = score;
        outTurns = turns;
        cards = all;  // Pass all cards so SaveManager can save their states
    }

    // ===================== CARD INTERACTION =====================
    public void TryFlip(CardView card)
    {
        // ===================== VALIDATION CHECKS =====================
        if (isGameOver || isPaused) return;
        if (card == null || card.IsMatched || card.IsAnimating || card.IsFaceUp) return;
        if (reserved.Contains(card)) return; // Already pending/queued/processing

        StartCoroutine(FlipUpFlow(card));
    }

    private IEnumerator FlipUpFlow(CardView card)
    {
        // ===================== RESERVE CARD (PREVENT DOUBLE-CLICKS) =====================
        reserved.Add(card);

        // Play sound and animate flip
        sounds?.PlayFlip();
        yield return card.FlipUp();

        // ===================== POST-FLIP VALIDATION =====================
        if (isGameOver || isPaused || card == null || card.IsMatched) 
        { 
            reserved.Remove(card); 
            yield break; 
        }

        // ===================== ADD TO PENDING MATCHES =====================
        pending.Add(card);

        // ===================== PAIR UP AVAILABLE CARDS =====================
        // Check if we have 2+ face-up cards to compare
        while (pending.Count >= 2)
        {
            var a = pending[0];
            var b = pending[1];
            pending.RemoveRange(0, 2);  // Remove from pending list

            turns++;  // Increment turn counter
            OnStatsChanged?.Invoke(score, turns);

            compareQueue.Enqueue((a, b));  // Add pair to comparison queue
        }

        // ===================== START COMPARISON IF NEEDED =====================
        EnsureCompareWorker();
    }

    private void EnsureCompareWorker()
    {
        // Start comparison coroutine if not already running and there are pairs to compare
        if (!compareWorkerRunning && compareQueue.Count > 0)
            StartCoroutine(CompareWorker());
    }

    // ===================== START REVEAL =====================
    private IEnumerator RevealAllCardsAtStart()
    {
        // ===================== CLEAN STATE FOR REVEAL =====================
        // Clear any pending comparisons during the reveal
        pending.Clear();
        compareQueue.Clear();
        reserved.Clear();

        // ===================== REVEAL ALL CARDS =====================
        foreach (var c in all) 
            if (c != null && !c.IsMatched) 
                c.ForceFaceUpInstant();
        
        yield return new WaitForSecondsRealtime(startRevealDuration);
        
        // ===================== HIDE ALL CARDS =====================
        foreach (var c in all) 
            if (c != null && !c.IsMatched) 
                c.ForceFaceDownInstant();
    }

    // ===================== MATCH COMPARISON =====================
    private IEnumerator CompareWorker()
    {
        compareWorkerRunning = true;

        // ===================== PROCESS ALL QUEUED PAIRS =====================
        while (compareQueue.Count > 0)
        {
            var (a, b) = compareQueue.Dequeue();

            // ===================== BRIEF PAUSE TO SHOW CARDS =====================
            // Let player see the cards before they're compared
            yield return new WaitForSecondsRealtime(revealBeforeCompare);

            // ===================== VALIDATION CHECKS =====================
            if (isGameOver || isPaused) continue;
            if (a == null || b == null) { reserved.Remove(a); reserved.Remove(b); continue; }
            if (a.IsMatched || b.IsMatched) { reserved.Remove(a); reserved.Remove(b); continue; }
            if (!a.IsFaceUp || !b.IsFaceUp) { reserved.Remove(a); reserved.Remove(b); continue; }

            // ===================== CHECK FOR MATCH =====================
            if (a.FaceId == b.FaceId)
            {
                // ===================== MATCH FOUND =====================
                a.SetMatched();
                b.SetMatched();

                score += matchScore;
                sounds?.PlayMatch();
                OnStatsChanged?.Invoke(score, turns);

                // Matched cards won't be flippable anyway; safe to unreserve
                reserved.Remove(a);
                reserved.Remove(b);
            }
            else
            {
                // ===================== NO MATCH =====================
                sounds?.PlayMismatch();
                
                // Brief pause so player can see the mismatch
                yield return new WaitForSecondsRealtime(revealMismatchBeforeFlipDown);

                // ===================== FLIP CARDS BACK DOWN =====================
                if (!isGameOver && !isPaused)
                {
                    if (!a.IsMatched && a.IsFaceUp) a.FlipDown();
                    if (!b.IsMatched && b.IsFaceUp) b.FlipDown();
                }

                // Now they can be flipped again in the future
                reserved.Remove(a);
                reserved.Remove(b);
            }

            // ===================== CHECK FOR LEVEL COMPLETION =====================
            if (!isGameOver && AllMatched())
                FinishLevel();
        }

        compareWorkerRunning = false;
    }

    private void FinishLevel()
    {
        // ===================== LEVEL COMPLETION =====================
        isGameOver = true;

        // Clear save since level is complete
        SaveManager.ClearSave();

        // Notify UI and play sound
        sounds?.PlayGameOver();
        OnGameOver?.Invoke();

        // Clean up collections
        pending.Clear();
        compareQueue.Clear();
        reserved.Clear();
    }

    // ===================== BOARD SETUP =====================
    private void SetupBoard(int rows, int cols,
        List<int> savedCardStates = null, List<int> savedCardFaceIds = null)
    {
        // ===================== VALIDATION =====================
        if (faceSprites == null || faceSprites.Count == 0)
        {
            Debug.LogError("MatchGameManager: Assign faceSprites in inspector!");
            return;
        }

        // ===================== CALCULATE BOARD SIZE =====================
        int totalSlots = rows * cols;
        int totalCardsToSpawn = (totalSlots % 2 == 1) ? totalSlots - 1 : totalSlots;

        if (totalCardsToSpawn < 4)
        {
            Debug.LogError($"Board too small: {rows}x{cols}. Need at least 4 cards.");
            return;
        }

        // ===================== LAYOUT ADJUSTMENT =====================
        layoutScaler?.Apply(rows, cols);

        // ===================== CREATE CARD DECK =====================
        // Use saved face IDs if loading game, otherwise create new shuffled deck
        var deck = (savedCardFaceIds != null && savedCardFaceIds.Count == totalCardsToSpawn)
            ? savedCardFaceIds
            : BuildDeckFaceIds(totalCardsToSpawn);

        // ===================== SPAWN CARDS WITH SAVED STATES =====================
        SpawnDeck(deck, savedCardStates);

        // ===================== HANDLE ODD GRID SIZES =====================
        if (allowOddByBlockingOneSlot && (totalSlots % 2 == 1))
            AddOddSpacer();

        // ===================== FORCE UI UPDATE =====================
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
    }

    private void SpawnDeck(List<int> deck, List<int> savedCardStates)
    {
        // ===================== CLEAR EXISTING STATE =====================
        pending.Clear();
        compareQueue.Clear();
        reserved.Clear();

        // ===================== CREATE EACH CARD =====================
        for (int i = 0; i < deck.Count; i++)
        {
            int id = deck[i];
            
            // ===================== DETERMINE CARD STATE FROM SAVE =====================
            // 0 = face down, 1 = face up (unmatched), 2 = matched (face up)
            int state = (savedCardStates != null && i < savedCardStates.Count) ? savedCardStates[i] : 0;
            bool startFaceDown = state == 0;

            // ===================== INSTANTIATE AND INITIALIZE CARD =====================
            var cv = Instantiate(cardPrefab, gridParent, false);
            cv.Init(id, faceSprites[id], backSprite, this, startFaceDown);

            // ===================== RESTORE SAVED STATE =====================
            if (state == 2)
            {
                // Card was matched in saved game
                cv.SetMatched();
                cv.ForceFaceUpInstant();
                reserved.Add(cv);
            }
            else if (state == 1)
            {
                // Card was face up but unmatched in saved game
                cv.ForceFaceUpInstant();
                pending.Add(cv);    // Treat as flipped-up waiting to be compared
                reserved.Add(cv);   // Prevent re-flip until resolved
            }

            all.Add(cv);
        }

        // ===================== PROCESS RESTORED FACE-UP CARDS =====================
        // If save restored 2+ face-up unmatched cards, queue them for comparison
        // This ensures the game continues exactly where it left off
        while (pending.Count >= 2)
        {
            var a = pending[0];
            var b = pending[1];
            pending.RemoveRange(0, 2);
            compareQueue.Enqueue((a, b));
        }
    }

    private void AddOddSpacer()
    {
        // Create an empty spacer for odd-numbered grids (makes grid work with pairs)
        var spacer = new GameObject("BlockedSlot", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(gridParent, false);
    }

    private List<int> BuildDeckFaceIds(int totalCards)
    {
        // ===================== CREATE PAIRS =====================
        int pairs = totalCards / 2;
        var deck = new List<int>(totalCards);

        for (int i = 0; i < pairs; i++)
        {
            int id = i % faceSprites.Count;  // Cycle through available sprites
            deck.Add(id);
            deck.Add(id);  // Add matching pair
        }

        // ===================== SHUFFLE DECK (Fisher–Yates algorithm) =====================
        for (int i = 0; i < deck.Count; i++)
        {
            int j = Random.Range(i, deck.Count);
            (deck[i], deck[j]) = (deck[j], deck[i]);  // Swap positions
        }

        return deck;
    }

    // ===================== CLEANUP =====================
    private void ClearBoard()
    {
        // ===================== CLEAR ALL COLLECTIONS =====================
        pending.Clear();
        compareQueue.Clear();
        reserved.Clear();
        compareWorkerRunning = false;

        // ===================== DESTROY ALL CARD GAMEOBJECTS =====================
        foreach (var c in all)
            if (c != null) Destroy(c.gameObject);
        all.Clear();

        // ===================== REMOVE SPACER OBJECTS =====================
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            var child = gridParent.GetChild(i);
            if (child.GetComponent<CardView>() == null)
                Destroy(child.gameObject);
        }
    }

    private bool AllMatched()
    {
        // Check if all cards on the board are matched
        foreach (var c in all)
            if (c != null && !c.IsMatched) return false;
        return all.Count > 0;  // Also ensure board isn't empty
    }
}