using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonologueManager : MonoBehaviour
{
    [Header("Startup")]
    [SerializeField] private MonologueSequence playOnStart;
    [SerializeField, Min(0f)] private float playOnStartDelay;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private readonly Queue<MonologueSequence> queuedSequences = new();
    private Coroutine playbackCoroutine;
    private bool advanceRequested;
    private MonologueSequence activeSequence;

    public static MonologueManager Instance { get; private set; }

    public bool IsPlaying => playbackCoroutine != null;
    public MonologueSequence ActiveSequence => activeSequence;

    public event Action<MonologueLine, int, int> OnLineStarted;
    public event Action<MonologueLine> OnLineEnded;
    public event Action<float> OnLineProgressChanged;
    public event Action<MonologueSequence> OnSequenceEnded;
    public event Action OnHidden;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (playOnStart == null)
            return;

        if (playOnStartDelay > 0f)
            StartCoroutine(PlayStartSequenceDelayed());
        else
            PlaySequence(playOnStart, playOnStart.InterruptCurrent);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlaySequence(MonologueSequence sequence)
    {
        if (sequence == null)
            return;

        PlaySequence(sequence, sequence.InterruptCurrent);
    }

    public void PlaySequence(MonologueSequence sequence, bool interrupt)
    {
        if (sequence == null || sequence.Lines == null || sequence.Lines.Length == 0)
            return;

        if (IsPlaying && !interrupt)
        {
            if (sequence.QueueIfBusy)
                queuedSequences.Enqueue(sequence);

            return;
        }

        if (interrupt)
            StopPlayback();

        playbackCoroutine = StartCoroutine(PlaySequenceRoutine(sequence));
    }

    public void QueueSequence(MonologueSequence sequence)
    {
        if (sequence == null)
            return;

        if (!IsPlaying)
        {
            PlaySequence(sequence, false);
            return;
        }

        queuedSequences.Enqueue(sequence);
    }

    public void ShowHint(string text, string speaker = "COMMAND", float duration = 4f, MonologueMood mood = MonologueMood.Info)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        MonologueSequence runtimeSequence = ScriptableObject.CreateInstance<MonologueSequence>();
        runtimeSequence.name = "Runtime Monologue Hint";
        runtimeSequence.hideFlags = HideFlags.HideAndDontSave;
        runtimeSequence.InterruptCurrent = true;
        runtimeSequence.Lines = new[]
        {
            new MonologueLine
            {
                Speaker = speaker,
                Text = text,
                Mood = mood,
                Duration = duration,
                Hint = string.Empty
            }
        };

        PlaySequence(runtimeSequence, true);
    }

    public void Advance()
    {
        advanceRequested = true;
    }

    public void Clear()
    {
        queuedSequences.Clear();
        StopPlayback();
        OnHidden?.Invoke();
    }

    private IEnumerator PlayStartSequenceDelayed()
    {
        yield return new WaitForSeconds(playOnStartDelay);

        if (playOnStart != null)
            PlaySequence(playOnStart, playOnStart.InterruptCurrent);
    }

    private IEnumerator PlaySequenceRoutine(MonologueSequence sequence)
    {
        activeSequence = sequence;
        int totalLines = sequence.Lines.Length;

        for (int i = 0; i < totalLines; i++)
        {
            MonologueLine line = sequence.Lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.Text))
                continue;

            if (line.StartDelay > 0f)
                yield return new WaitForSeconds(line.StartDelay);

            advanceRequested = false;
            PlayVoice(line);
            OnLineProgressChanged?.Invoke(0f);
            OnLineStarted?.Invoke(line, i + 1, totalLines);

            float duration = ResolveLineDuration(line);
            float timer = 0f;

            while (timer < duration)
            {
                if (advanceRequested)
                    break;

                timer += Time.deltaTime;
                OnLineProgressChanged?.Invoke(duration <= 0f ? 1f : Mathf.Clamp01(timer / duration));
                yield return null;
            }

            if (line.WaitForClick)
            {
                OnLineProgressChanged?.Invoke(1f);

                while (!advanceRequested)
                    yield return null;
            }

            OnLineEnded?.Invoke(line);
        }

        MonologueSequence finishedSequence = activeSequence;
        playbackCoroutine = null;
        activeSequence = null;
        OnHidden?.Invoke();
        OnSequenceEnded?.Invoke(finishedSequence);
        Log($"Sequence finished: {finishedSequence.name}");

        if (finishedSequence != null && (finishedSequence.hideFlags & HideFlags.DontSave) != 0)
            Destroy(finishedSequence);

        PlayNextQueuedSequence();
    }

    private void PlayVoice(MonologueLine line)
    {
        if (audioSource == null || line.VoiceClip == null)
            return;

        audioSource.Stop();
        audioSource.clip = line.VoiceClip;
        audioSource.Play();
    }

    private float ResolveLineDuration(MonologueLine line)
    {
        if (line.Duration > 0f)
            return line.Duration;

        if (line.VoiceClip != null)
            return Mathf.Max(0.5f, line.VoiceClip.length + 0.15f);

        return 3.5f;
    }

    private void StopPlayback()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        activeSequence = null;
        advanceRequested = false;
    }

    private void PlayNextQueuedSequence()
    {
        if (queuedSequences.Count == 0)
            return;

        PlaySequence(queuedSequences.Dequeue(), false);
    }

    private void Log(string message)
    {
        if (logDebug)
            Debug.Log($"[{nameof(MonologueManager)}] {message}");
    }
}
