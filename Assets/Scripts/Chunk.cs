using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    private const uint DIMENSION = 8;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        for (uint z = 0; z < DIMENSION; z++)
        {
            for (uint y = 0; y < DIMENSION; y++)
            {
                for (uint x = 0; x < DIMENSION; x++)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.parent = transform;
                    sphere.transform.localPosition = new(x, y, z);
                    sphere.transform.localScale = 0.3f * Vector3.one;
                }
            }
        }
    }
}
