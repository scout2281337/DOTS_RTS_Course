using System;
using System.Collections.Generic;
using UnityEngine;

public static class ZombieSpawnPointEvents
{
    public static event Action<int, IReadOnlyList<Vector3>> OnSceneSpawnPointsChanged;

    private static readonly List<Vector3> LatestPoints = new();

    public static int Version { get; private set; }
    public static bool HasPoints => LatestPoints.Count > 0;

    public static IReadOnlyList<Vector3> GetLatestPoints()
    {
        return LatestPoints;
    }

    public static void PublishSceneSpawnPoints(IReadOnlyList<Vector3> points)
    {
        LatestPoints.Clear();

        if (points != null)
        {
            for (int i = 0; i < points.Count; i++)
            {
                LatestPoints.Add(points[i]);
            }
        }

        Version++;
        OnSceneSpawnPointsChanged?.Invoke(Version, LatestPoints);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        LatestPoints.Clear();
        Version = 0;
        OnSceneSpawnPointsChanged = null;
    }
}
