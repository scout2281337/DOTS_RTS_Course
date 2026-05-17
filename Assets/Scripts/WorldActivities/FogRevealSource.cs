using System.Collections.Generic;
using UnityEngine;

public class FogRevealSource : MonoBehaviour
{
    private static readonly List<FogRevealSource> activeSources = new();

    [SerializeField] private float radius = 18f;
    [SerializeField] private bool activeOnStart;
    [SerializeField] private bool respectObstacles = true;

    private bool isRevealing;

    public static IReadOnlyList<FogRevealSource> ActiveSources => activeSources;
    public float Radius => Mathf.Max(0f, radius);
    public bool IsRevealing => isRevealing && isActiveAndEnabled;
    public bool RespectObstacles => respectObstacles;
    public Vector3 Position => transform.position;

    private void OnEnable()
    {
        if (!activeSources.Contains(this))
            activeSources.Add(this);

        isRevealing = activeOnStart || isRevealing;
    }

    private void OnDisable()
    {
        activeSources.Remove(this);
    }

    public void SetRadius(float value)
    {
        radius = Mathf.Max(0f, value);
    }

    public void SetRevealing(bool value)
    {
        isRevealing = value;
    }
}
