using UnityEngine;

public class SoundContorller : MonoBehaviour
{
    [Header("Clips (only 4)")]
    public AudioClip flipClip;
    public AudioClip matchClip;
    public AudioClip mismatchClip;
    public AudioClip gameOverClip;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
    }

    public void PlayFlip()     => Play(flipClip);
    public void PlayMatch()    => Play(matchClip);
    public void PlayMismatch() => Play(mismatchClip);
    public void PlayGameOver() => Play(gameOverClip);

    private void Play(AudioClip clip)
    {
        if (clip == null || src == null) return;
        src.PlayOneShot(clip);
    }
}