using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MusicPlaylistPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopPlaylist = true;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 2f;
    [SerializeField, Min(0f)] private float silenceBetweenTracks = 0.75f;

    private AudioSource source;
    private int currentTrackIndex;
    private bool transitionInProgress;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.volume = volume;
        source.outputAudioMixerGroup = musicMixerGroup;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (playOnStart)
            PlayTrack(0);
    }

    private void Update()
    {
        if (transitionInProgress || source.clip == null)
            return;

        if (source.isPlaying)
        {
            float timeLeft = source.clip.length - source.time;
            if (timeLeft <= fadeOutDuration)
                StartTrackTransition(GetNextTrackIndex());

            return;
        }

        StartTrackTransition(GetNextTrackIndex(), skipFadeOut: true);
    }

    public void PlayNextTrack()
    {
        StartTrackTransition(GetNextTrackIndex());
    }

    public void PlayTrack(int index)
    {
        if (playlist == null || playlist.Length == 0)
            return;

        if (index < 0 || index >= playlist.Length)
            return;

        StopTransitionIfNeeded();
        currentTrackIndex = index;
        source.clip = playlist[currentTrackIndex];
        source.volume = 0f;
        source.Play();
        transitionRoutine = StartCoroutine(FadeVolume(volume, fadeInDuration));
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        StopTransitionIfNeeded();
        transitionRoutine = StartCoroutine(StopWithFade(fadeDuration >= 0f ? fadeDuration : fadeOutDuration));
    }

    private void StartTrackTransition(int nextIndex, bool skipFadeOut = false)
    {
        if (nextIndex < 0)
            return;

        StopTransitionIfNeeded();
        transitionRoutine = StartCoroutine(TransitionToTrack(nextIndex, skipFadeOut));
    }

    private IEnumerator TransitionToTrack(int nextIndex, bool skipFadeOut)
    {
        transitionInProgress = true;

        if (!skipFadeOut && source.isPlaying)
            yield return FadeVolume(0f, fadeOutDuration);

        source.Stop();

        if (silenceBetweenTracks > 0f)
            yield return new WaitForSeconds(silenceBetweenTracks);

        currentTrackIndex = nextIndex;
        source.clip = playlist[currentTrackIndex];
        source.volume = 0f;
        source.Play();

        yield return FadeVolume(volume, fadeInDuration);

        transitionInProgress = false;
        transitionRoutine = null;
    }

    private IEnumerator StopWithFade(float fadeDuration)
    {
        transitionInProgress = true;

        if (source.isPlaying)
            yield return FadeVolume(0f, fadeDuration);

        source.Stop();
        transitionInProgress = false;
        transitionRoutine = null;
    }

    private IEnumerator FadeVolume(float targetVolume, float duration)
    {
        float startVolume = source.volume;

        if (duration <= 0f)
        {
            source.volume = targetVolume;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        source.volume = targetVolume;
    }

    private int GetNextTrackIndex()
    {
        if (playlist == null || playlist.Length == 0)
            return -1;

        int nextIndex = currentTrackIndex + 1;
        if (nextIndex < playlist.Length)
            return nextIndex;

        return loopPlaylist ? 0 : -1;
    }

    private void StopTransitionIfNeeded()
    {
        if (transitionRoutine == null)
            return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
        transitionInProgress = false;
    }
}
