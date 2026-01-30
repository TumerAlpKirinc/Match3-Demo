using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Ses Kaynaðý")]
    public AudioSource sfxSource;

    [Header("Ses Dosyalarý (Audio Clips)")]
    public AudioClip successfulSwipeSound;
    public AudioClip failSwipeSound;
    public AudioClip bombSound;
    public AudioClip laserSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    private float lastSoundTime;
    [SerializeField] private float soundCooldown = .1f;
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }


    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (Time.time - lastSoundTime < soundCooldown)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
        lastSoundTime = Time.time;
    }
}