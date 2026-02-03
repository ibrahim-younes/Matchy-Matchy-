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
        new GridSize(2,2),
        new GridSize(4,4),
        new GridSize(5,6),
        new GridSize(3,3),
    };

    [Tooltip("If true: choose next level randomly. If false: cycle in order.")]
    [SerializeField] private bool randomizeNextLevel = false;

    [Header("Odd Grid Handling")]
    [SerializeField] private bool allowOddByBlockingOneSlot = true;

    [Header("Reveal / Timing")]
    [SerializeField] private float revealBeforeCompare = 0.35f;
    [SerializeField] private float revealMismatchBeforeFlipDown = 0.55f;

    [Header("Scoring")]
    [SerializeField] private int matchScore = 10;

    [Header("Sound")]
    [SerializeField] private SoundContorller sounds;

    // ---- Public state (read-only) ----
    public int Score => score;
    public int Turns => turns;
    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentRows => currentRows;
    public int CurrentCols => currentCols;
    public bool IsGameOver => isGameOver;
    public bool IsPaused => isPaused;

    // ---- Events for UIController ----
    public System.Action<int, int> OnStatsChanged; // score, turns
    public System.Action OnGameOver;
    public System.Action OnNewLevel;

    // ---- Internal state ----
    private int score;
    private int turns;

    private readonly List<CardView> all = new();
    private readonly List<CardView> faceUpUnmatched = new();
    private readonly Queue<(CardView a, CardView b)> compareQueue = new();

    private bool compareWorkerRunning;
    private bool isGameOver;
    private bool isPaused;

    private int currentLevelIndex;
    private int currentRows;
    private int currentCols;

    private void Start()
    {
        if (!ValidateRefs()) return;

        if (levels == null || levels.Count == 0)
            levels = new List<GridSize> { new GridSize(2, 2) };

        // Load save if exists
        if (SaveManager.HasSave())
        {
            SaveManager.LoadGame(out currentLevelIndex, out currentRows, out currentCols, out score, out turns);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
            StartLevel(currentRows, currentCols, preserveStats: true);
        }
        else
        {
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
            var s = levels[currentLevelIndex];
            StartLevel(s.rows, s.cols, preserveStats: false);
        }
    }

    // Called by UIController "Continue" button (after Well Done)
    public void StartNextLevel()
    {
        if (levels == null || levels.Count == 0)
        {
            StartLevel(2, 2, preserveStats: false);
            return;
        }

        currentLevelIndex = randomizeNextLevel
            ? Random.Range(0, levels.Count)
            : (currentLevelIndex + 1) % levels.Count;

        var s = levels[currentLevelIndex];
        StartLevel(s.rows, s.cols, preserveStats: false);
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        // We keep timeScale control in UIController, so game logic stays pure.
    }

    private void StartLevel(int rows, int cols, bool preserveStats)
    {
        isGameOver = false;
        isPaused = false;

        currentRows = rows;
        currentCols = cols;

        ClearBoard();
        SetupBoard(rows, cols);

        if (!preserveStats)
        {
            score = 0;
            turns = 0;
        }

        OnNewLevel?.Invoke();
        OnStatsChanged?.Invoke(score, turns);
    }

    // Used by UIController when saving
    public void GetSaveData(out int levelIndex, out int rows, out int cols, out int outScore, out int outTurns)
    {
        levelIndex = currentLevelIndex;
        rows = currentRows;
        cols = currentCols;
        outScore = score;
        outTurns = turns;
    }

    public void TryFlip(CardView card)
    {
        if (isGameOver || isPaused) return;
        if (card == null || card.IsMatched || card.IsAnimating || card.IsFaceUp) return;

        StartCoroutine(FlipUpFlow(card));
    }

    private IEnumerator FlipUpFlow(CardView card)
    {
        sounds?.PlayFlip();
        yield return card.FlipUp();

        if (isGameOver || isPaused || card.IsMatched) yield break;

        faceUpUnmatched.Add(card);

        while (faceUpUnmatched.Count >= 2)
        {
            var a = faceUpUnmatched[0];
            var b = faceUpUnmatched[1];
            faceUpUnmatched.RemoveRange(0, 2);

            turns++;
            OnStatsChanged?.Invoke(score, turns);

            compareQueue.Enqueue((a, b));

            if (!compareWorkerRunning)
                StartCoroutine(CompareWorker());
        }
    }

    private IEnumerator CompareWorker()
    {
        compareWorkerRunning = true;

        while (compareQueue.Count > 0)
        {
            var (a, b) = compareQueue.Dequeue();

            yield return new WaitForSecondsRealtime(revealBeforeCompare);

            if (isGameOver || isPaused) continue;
            if (a == null || b == null) continue;
            if (a.IsMatched || b.IsMatched) continue;
            if (!a.IsFaceUp || !b.IsFaceUp) continue;

            bool isMatch = a.FaceId == b.FaceId;

            if (isMatch)
            {
                a.SetMatched();
                b.SetMatched();

                score += matchScore;
                sounds?.PlayMatch();
                OnStatsChanged?.Invoke(score, turns);
            }
            else
            {
                sounds?.PlayMismatch();

                yield return new WaitForSecondsRealtime(revealMismatchBeforeFlipDown);

                if (!isGameOver && !isPaused)
                {
                    if (!a.IsMatched && a.IsFaceUp) a.FlipDown();
                    if (!b.IsMatched && b.IsFaceUp) b.FlipDown();
                }
            }

            if (!isGameOver && AllMatched())
                FinishLevel();
        }

        compareWorkerRunning = false;
    }

    private void FinishLevel()
    {
        isGameOver = true;

        // finishing a level invalidates save (optional but recommended)
        SaveManager.ClearSave();

        sounds?.PlayGameOver();
        OnGameOver?.Invoke();

        compareQueue.Clear();
        faceUpUnmatched.Clear();
    }

    private void SetupBoard(int rows, int cols)
    {
        if (faceSprites == null || faceSprites.Count == 0)
        {
            Debug.LogError("MatchGameManager: Assign faceSprites in inspector!");
            return;
        }

        int totalSlots = rows * cols;
        int totalCardsToSpawn = (totalSlots % 2 == 1) ? totalSlots - 1 : totalSlots;

        if (totalCardsToSpawn < 4)
        {
            Debug.LogError($"Board too small: {rows}x{cols}. Need at least 4 cards.");
            return;
        }

        layoutScaler?.Apply(rows, cols);

        var deck = BuildDeckFaceIds(totalCardsToSpawn);
        SpawnDeck(deck);

        if (allowOddByBlockingOneSlot && (totalSlots % 2 == 1))
            AddOddSpacer();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
    }

    private void SpawnDeck(List<int> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int id = deck[i];
            Sprite face = faceSprites[id];

            var cv = Instantiate(cardPrefab, gridParent, false);
            cv.Init(id, face, backSprite, this);
            all.Add(cv);
        }
    }

    private void AddOddSpacer()
    {
        var spacer = new GameObject("BlockedSlot", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(gridParent, false);
    }

    private List<int> BuildDeckFaceIds(int totalCards)
    {
        int pairs = totalCards / 2;

        var deck = new List<int>(totalCards);
        for (int i = 0; i < pairs; i++)
        {
            int id = i % faceSprites.Count;
            deck.Add(id);
            deck.Add(id);
        }

        for (int i = 0; i < deck.Count; i++)
        {
            int j = Random.Range(i, deck.Count);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return deck;
    }

    private void ClearBoard()
    {
        faceUpUnmatched.Clear();
        compareQueue.Clear();
        compareWorkerRunning = false;

        for (int i = 0; i < all.Count; i++)
            if (all[i] != null) Destroy(all[i].gameObject);
        all.Clear();

        // remove non-card children (spacers)
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            var child = gridParent.GetChild(i);
            if (child.GetComponent<CardView>() == null)
                Destroy(child.gameObject);
        }
    }

    private bool AllMatched()
    {
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && !all[i].IsMatched)
                return false;

        return all.Count > 0;
    }

    private bool ValidateRefs()
    {
        if (gridParent == null || cardPrefab == null || backSprite == null)
        {
            Debug.LogError("MatchGameManager: Missing references (gridParent/cardPrefab/backSprite).");
            return false;
        }
        return true;
    }
}