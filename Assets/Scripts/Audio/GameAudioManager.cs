using System.Collections.Generic;
using UnityEngine;

public class GameAudioManager : Singleton<GameAudioManager>
{

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 24;
    [SerializeField] private int maxPoolSize = 96;

    private readonly List<AudioSource> sources = new();
    private readonly Dictionary<AudioCueSO, float> cooldowns = new();
    private readonly List<AudioCueSO> expiredCooldowns = new();

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

        expiredCooldowns.Clear();

        foreach (var pair in cooldowns)
        {
            if (pair.Value <= Time.time)
                expiredCooldowns.Add(pair.Key);
        }

        for (int i = 0; i < expiredCooldowns.Count; i++)
            cooldowns.Remove(expiredCooldowns[i]);
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
            cooldowns[cue] = Time.time + cue.cooldown;
    }

    private bool IsOnCooldown(AudioCueSO cue)
    {
        if (cue.cooldown <= 0f)
            return false;

        if (!cooldowns.TryGetValue(cue, out float cooldownEndTime))
            return false;

        if (cooldownEndTime > Time.time)
            return true;

        cooldowns.Remove(cue);
        return false;
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
