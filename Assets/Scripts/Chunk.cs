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

    private readonly float[,,] noiseValues = new float[DIMENSION, DIMENSION, DIMENSION];

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        GenerateNoiseValues();
        GenerateSpheres();
        GenerateMesh();
    }

    private void GenerateNoiseValues()
    {
        for (uint z = 0; z < DIMENSION; z++)
        {
            for (uint y = 0; y < DIMENSION; y++)
            {
                for (uint x = 0; x < DIMENSION; x++)
                {
                    float3 coordinate = NOISE_SCALE / (float)DIMENSION * new float3(x, y, z);
                    float value = math.unlerp(-1f, 1f, noise.snoise(coordinate));
                    noiseValues[z, y, x] = value;
                }
            }
        }
    }

    private void GenerateSpheres()
    {
        for (uint z = 0; z < DIMENSION; z++)
        {
            for (uint y = 0; y < DIMENSION; y++)
            {
                for (uint x = 0; x < DIMENSION; x++)
                {
                    float value = noiseValues[z, y, x];
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

    private void GenerateMesh()
    {
        for (uint z = 0; z < DIMENSION - 1; z++)
        {
            for (uint y = 0; y < DIMENSION - 1; y++)
            {
                for (uint x = 0; x < DIMENSION - 1; x++)
                {
                    //float frontBottomLeftValue = noiseValues[z, y, x];
                    //float frontTopLeftValue = noiseValues[z, y + 1, x];
                    //float frontTopRightValue = noiseValues[z, y + 1, x + 1];
                    //float frontBottomRightValue = noiseValues[z, y, x + 1];
                    //float backBottomLeftValue = noiseValues[z + 1, y, x];
                    //float backTopLeftValue = noiseValues[z + 1, y + 1, x];
                    //float backTopRightValue = noiseValues[z + 1, y + 1, x + 1];
                    //float backBottomRightValue = noiseValues[z + 1, y, x + 1];
                }
            }
        }
    }
}
