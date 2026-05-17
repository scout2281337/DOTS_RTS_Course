using UnityEngine;
using UnityEngine.Audio;

public enum AudioCueSpatialMode
{
    TwoD,
    ThreeD
}

[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue")]
public class AudioCueSO : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Mixer")]
    public AudioMixerGroup mixerGroup;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float volumeRandom = 0.05f;

    [Header("Pitch")]
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchRandom = 0.08f;

    [Header("Spatial")]
    public AudioCueSpatialMode spatialMode = AudioCueSpatialMode.ThreeD;
    [Min(0f)] public float minDistance = 3f;
    [Min(0.01f)] public float maxDistance = 35f;

    [Header("Playback")]
    [Min(0f)] public float cooldown = 0.05f;
    public bool loop;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetVolume()
    {
        if (volumeRandom <= 0f)
            return volume;

        return Mathf.Clamp01(volume + Random.Range(-volumeRandom, volumeRandom));
    }

    public float GetPitch()
    {
        if (pitchRandom <= 0f)
            return pitch;

        return Mathf.Max(0.1f, pitch + Random.Range(-pitchRandom, pitchRandom));
    }

    public bool Is3D => spatialMode == AudioCueSpatialMode.ThreeD;
}