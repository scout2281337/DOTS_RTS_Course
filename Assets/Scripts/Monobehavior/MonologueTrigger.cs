using System.Collections;
using UnityEngine;

public class MonologueTrigger : MonoBehaviour
{
    [SerializeField] private MonologueSequence sequence;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnce = true;
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField] private KeyCode debugKey = KeyCode.None;

    private bool hasPlayed;

    private void Start()
    {
        if (!playOnStart)
            return;

        if (startDelay > 0f)
            StartCoroutine(PlayDelayed());
        else
            Play();
    }

    private void Update()
    {
        if (debugKey == KeyCode.None || !Input.GetKeyDown(debugKey))
            return;

        Play();
    }

    [ContextMenu("Play Monologue")]
    public void Play()
    {
        if (sequence == null)
            return;

        if (playOnce && hasPlayed)
            return;

        MonologueManager manager = MonologueManager.Instance;
        if (manager == null)
            manager = FindFirstObjectByType<MonologueManager>();

        if (manager == null)
        {
            Debug.LogWarning($"{nameof(MonologueTrigger)} could not find a {nameof(MonologueManager)} in the scene.");
            return;
        }

        hasPlayed = true;
        manager.PlaySequence(sequence);
    }

    private IEnumerator PlayDelayed()
    {
        yield return new WaitForSeconds(startDelay);
        Play();
    }
}
