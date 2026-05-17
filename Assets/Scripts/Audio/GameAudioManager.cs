using System.Collections.Generic;
using UnityEngine;

public class GameAudioManager : Singleton<GameAudioManager>
{

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 24;
    [SerializeField] private int maxPoolSize = 96;

    private readonly List<AudioSource> sources = new();
    private readonly Dictionary<AudioCueSO, float> cooldowns = new();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        for (int i = 0; i < initialPoolSize; i++)
            CreateSource();
    }

    private void Update()
    {
        if (cooldowns.Count == 0)
            return;

        List<AudioCueSO> finished = null;

        foreach (var pair in cooldowns)
        {
            float timeLeft = pair.Value - Time.deltaTime;
            cooldowns[pair.Key] = timeLeft;

            if (timeLeft <= 0f)
            {
                finished ??= new List<AudioCueSO>();
                finished.Add(pair.Key);
            }
        }

        if (finished == null)
            return;

        for (int i = 0; i < finished.Count; i++)
            cooldowns.Remove(finished[i]);
    }

    public void Play2D(AudioCueSO cue)
    {
        if (cue == null)
            return;

        Play(cue, Vector3.zero, force2D: true);
    }

    public void Play3D(AudioCueSO cue, Vector3 position)
    {
        if (cue == null)
            return;

        Play(cue, position, force2D: false);
    }

    public void Play(AudioCueSO cue, Vector3 position)
    {
        if (cue == null)
            return;

        Play(cue, position, force2D: !cue.Is3D);
    }

    private void Play(AudioCueSO cue, Vector3 position, bool force2D)
    {
        if (cue == null)
            return;

        if (IsOnCooldown(cue))
            return;

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return;

        AudioSource source = GetFreeSource();
        if (source == null)
            return;

        source.transform.position = position;
        source.outputAudioMixerGroup = cue.mixerGroup;
        source.clip = clip;
        source.volume = cue.GetVolume();
        source.pitch = cue.GetPitch();
        source.loop = cue.loop;
        source.spatialBlend = force2D ? 0f : 1f;
        source.minDistance = cue.minDistance;
        source.maxDistance = cue.maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        if (cue.cooldown > 0f)
            cooldowns[cue] = cue.cooldown;
    }

    private bool IsOnCooldown(AudioCueSO cue)
    {
        return cue.cooldown > 0f && cooldowns.ContainsKey(cue);
    }

    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            if (!sources[i].isPlaying)
                return sources[i];
        }

        if (sources.Count >= maxPoolSize)
            return null;

        return CreateSource();
    }

    private AudioSource CreateSource()
    {
        GameObject sourceObject = new GameObject($"Audio Source {sources.Count:00}");
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;

        sources.Add(source);
        return source;
    }
}
