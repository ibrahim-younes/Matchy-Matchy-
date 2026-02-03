using UnityEngine;

public class SoundContorller : MonoBehaviour
{
    [Header("Clips (only 4)")]
    public AudioClip flipClip;
    public AudioClip matchClip;
    public AudioClip mismatchClip;
    public AudioClip gameOverClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float flipVolume = 1f;
    [Range(0f, 1f)] public float matchVolume = 1f;
    [Range(0f, 1f)] public float mismatchVolume = 1f;
    [Range(0f, 1f)] public float gameOverVolume = 1f;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
    }

    public void PlayFlip()     => Play(flipClip, flipVolume);
    public void PlayMatch()    => Play(matchClip, matchVolume);
    public void PlayMismatch() => Play(mismatchClip, mismatchVolume);
    public void PlayGameOver() => Play(gameOverClip, gameOverVolume);

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || src == null) return;
        src.PlayOneShot(clip, volume * masterVolume);
    }
}