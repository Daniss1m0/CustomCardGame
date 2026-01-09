using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip placeCardClip, attackImpactClip, heroHitClip, drawCardClip, spellCastClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayPlaceCard() => PlaySound(placeCardClip);
    public void PlayAttack() => PlaySound(attackImpactClip);
    public void PlayHeroHit() => PlaySound(heroHitClip);
    public void PlaySpellCast() => PlaySound(spellCastClip);
}