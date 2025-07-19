using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public CubeSpawner cubeSpawner;
    
    // Update is called once per frame
    void Update()
    {
        moveCubes();
    }

    private void moveCubes() 
    {
        List<GameObject> cubesList = cubeSpawner.cubes;
        for (int i = cubesList.Count - 1; i >= 0; i--)
        {
            GameObject cube = cubesList[i];
            cube.transform.position += new Vector3(0, -9, 0) * Time.deltaTime;

            if (cube.transform.position.y < 0)
            {
                
                cubesList.RemoveAt(i); // безопасно удалять с конца
                Destroy(cube);
            }
        }

    }
}
