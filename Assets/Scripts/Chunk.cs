using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class Chunk : MonoBehaviour
{
    private const uint DIMENSION = 8;
    private const float NOISE_SCALE = 1f;
    private const float NOISE_THRESHOLD = 0.7f;

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
                    float3 coordinate = NOISE_SCALE / (float)DIMENSION * new float3(x, y, z);
                    float value = math.unlerp(-1f, 1f, noise.snoise(coordinate));
                    if (value < NOISE_THRESHOLD)
                    {
                        continue;
                    }
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.parent = transform;
                    sphere.transform.localPosition = new(x, y, z);
                    sphere.transform.localScale = 0.3f * Vector3.one;
                    sphere.GetComponent<MeshRenderer>().material.color = new Color(value, value, value);
                }
            }
        }
    }
}
