// Handles visuals + interaction of a single card

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image backImage;
    [SerializeField] private Image frontImage;
    [SerializeField] private float flipDuration = 0.18f;

    public int FaceId { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsAnimating { get; private set; }

    private MatchGameManager game;

    // Called when a card is created by the GameManager
    public void Init(int faceId, Sprite faceSprite, Sprite backSprite, MatchGameManager manager, bool forceFaceDown = true)
    {
        FaceId = faceId;
        game = manager;

        if (forceFaceDown)
        {
            IsMatched = false;
            IsFaceUp = false;
        }
    
        IsAnimating = false;

        backImage.sprite = backSprite;
        frontImage.sprite = faceSprite;

        // Only set visual to face down if forcing it
        if (forceFaceDown)
        {
            SetVisual(false);
        }
    
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData _)
    {
        game?.TryFlip(this);
    }

    public void SetMatched()
    {
        IsMatched = true;
        SetFace(true, instant: true);
    }

    public Coroutine FlipUp()   => StartCoroutine(Flip(true));
    public Coroutine FlipDown() => StartCoroutine(Flip(false));

    public void ForceFaceUpInstant()   => SetFace(true, true);
    public void ForceFaceDownInstant() => SetFace(false, true);

    private void SetFace(bool up, bool instant)
    {
        
        if (IsMatched && !up) return;

        IsFaceUp = up;
        SetVisual(up);

        if (instant)
            transform.localScale = Vector3.one;
    }

    private IEnumerator Flip(bool toFaceUp)
    {
        if (IsAnimating || IsMatched || toFaceUp == IsFaceUp)
            yield break;

        IsAnimating = true;

        float half = Mathf.Max(0.01f, flipDuration * 0.5f);
        float startTime = Time.realtimeSinceStartup;

        // First half: shrink to flat
        while (Time.realtimeSinceStartup - startTime < half)
        {
            float t = (Time.realtimeSinceStartup - startTime) / half;
            transform.localScale = new Vector3(Mathf.Lerp(1f, 0f, t), 1f, 1f);
            yield return null;
        }

        // Change face
        SetFace(toFaceUp, false);

        startTime = Time.realtimeSinceStartup;
    
        // Second half: expand back
        while (Time.realtimeSinceStartup - startTime < half)
        {
            float t = (Time.realtimeSinceStartup - startTime) / half;
            transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, t), 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        IsAnimating = false;
    }

    private void SetVisual(bool faceUp)
    {
        backImage.enabled  = !faceUp;
        frontImage.enabled = faceUp;
    }
}