using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Handles visuals + interaction of a single card
public class CardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image backImage;
    [SerializeField] Image frontImage;

    // How long the flip animation takes
    [SerializeField] float flipDuration = 0.18f;

    // Public state (read-only from outside)
    public int FaceId { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsAnimating { get; private set; }

    // Reference to the game manager
    MatchGameManager game;

    void Awake()
    {
        // Auto-find images if not assigned in Inspector
        backImage  ??= transform.Find("Back")?.GetComponent<Image>();
        frontImage ??= transform.Find("Front")?.GetComponent<Image>();

        // Warn if something is missing
        if (!backImage || !frontImage)
            Debug.LogError(
                "CardView: Assign Back/Front Images (children named 'Back' and 'Front')."
            );
    }

    // Called when a card is created by the GameManager
    public void Init(int faceId, Sprite faceSprite, Sprite backSprite, MatchGameManager manager)
    {
        FaceId = faceId;
        game = manager;

        // Reset state
        IsMatched = false;
        IsAnimating = false;
        IsFaceUp = false;

        // Assign sprites
        if (backImage)  backImage.sprite = backSprite;
        if (frontImage) frontImage.sprite = faceSprite;

        // Start face down
        SetVisual(false);
        transform.localScale = Vector3.one;
    }

    // Called when the user clicks the card
    public void OnPointerClick(PointerEventData _)
    {
        // Let the GameManager decide if this card can flip
        game?.TryFlip(this);
    }

    // Mark the card as matched and keep it face up
    public void SetMatched()
    {
        IsMatched = true;
        SetFace(true, instant: true);
    }

    // Public flip helpers used by GameManager
    public Coroutine FlipUp()   => StartCoroutine(Flip(true));
    public Coroutine FlipDown() => StartCoroutine(Flip(false));

    // Instantly show card face up (no animation)
    public void ForceFaceUpInstant()   => SetFace(true, instant: true);

    // Instantly show card face down (no animation)
    public void ForceFaceDownInstant() => SetFace(false, instant: true);

    // Sets the face state (with or without animation)
    void SetFace(bool up, bool instant)
    {
        if (IsMatched) return;

        IsFaceUp = up;
        SetVisual(up);

        if (instant)
            transform.localScale = Vector3.one;
    }

    // Flip animation coroutine
    IEnumerator Flip(bool toFaceUp)
    {
        // Safety checks
        if (IsAnimating || IsMatched || toFaceUp == IsFaceUp) yield break;
        if (!backImage || !frontImage) yield break;

        IsAnimating = true;

        float half = Mathf.Max(0.01f, flipDuration * 0.5f);

        // 1) Shrink X scale to 0 (card edge)
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            transform.localScale = new Vector3(Mathf.Lerp(1f, 0f, k), 1f, 1f);
            yield return null;
        }

        // Switch which side is visible
        SetFace(toFaceUp, instant: false);

        // 2) Expand X scale back to 1
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, k), 1f, 1f);
            yield return null;
        }

        // Finish animation
        transform.localScale = Vector3.one;
        IsAnimating = false;
    }

    // Enable front or back image based on face state
    void SetVisual(bool faceUp)
    {
        if (backImage)  backImage.enabled = !faceUp;
        if (frontImage) frontImage.enabled = faceUp;
    }
}
