using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public int amountToSpawn = 20;
    public GameObject cubePrefab;
    public float timerMax = 0.5f;
    private float timer = 0;

    public List<GameObject> cubes = new List<GameObject>();
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timerMax) 
        {
            timer = 0;
            SpawnCubes();
        }
    }
    private void SpawnCubes() 
    {
        for (int i = 0; i < amountToSpawn; i++) 
        {
            GameObject cube =  Instantiate(cubePrefab,new Vector3(Random.Range(-30, 30), 30 , 0), Quaternion.identity);
            cubes.Add(cube);
        }
    
    }
    
}
