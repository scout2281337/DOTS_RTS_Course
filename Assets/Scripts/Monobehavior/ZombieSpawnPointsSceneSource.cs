using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawnPointsSceneSource : MonoBehaviour
{
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _spawnPointsRoot;
    [SerializeField] private bool _includeInactiveChildren;

    private readonly List<Vector3> _points = new();

    private void Start()
    {
        PublishSpawnPoints();
    }

    public void PublishSpawnPoints()
    {
        _points.Clear();

        if (_spawnPoints != null)
        {
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (_spawnPoints[i] == null)
                    continue;

                _points.Add(_spawnPoints[i].position);
            }
        }

        if (_spawnPointsRoot != null)
        {
            for (int i = 0; i < _spawnPointsRoot.childCount; i++)
            {
                Transform child = _spawnPointsRoot.GetChild(i);
                if (!_includeInactiveChildren && !child.gameObject.activeInHierarchy)
                    continue;

                _points.Add(child.position);
            }
        }

        if (_points.Count == 0)
        {
            _points.Add(transform.position);
        }

        ZombieSpawnPointEvents.PublishSceneSpawnPoints(_points);
        Debug.Log($"Published {_points.Count} zombie spawn points from scene '{gameObject.scene.name}'.");
    }
}
