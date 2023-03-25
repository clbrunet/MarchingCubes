using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private const uint DIMENSION = 8;
    private const float NOISE_SCALE = 1f;
    private const float ISOSURFACE_THRESHOLD = 0.7f;

    private MeshFilter meshFilter;

    private readonly float[,,] noiseValues = new float[DIMENSION, DIMENSION, DIMENSION];

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

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
                    if (value < ISOSURFACE_THRESHOLD)
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
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int z = 0; z < DIMENSION - 1; z++)
        {
            for (int y = 0; y < DIMENSION - 1; y++)
            {
                for (int x = 0; x < DIMENSION - 1; x++)
                {
                    GenerateMeshCube(new Vector3Int(x, y, z), vertices, triangles);
                }
            }
        }
        Mesh mesh  = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void GenerateMeshCube(Vector3Int frontBottomLeft, List<Vector3> vertices, List<int> triangles)
    {
        int lookupCaseIndex = 0;
        int i = 0b0000_0001;
        foreach (Vector3Int corner in LookupTables.CORNERS)
        {
            if (noiseValues[frontBottomLeft.z + corner.z, frontBottomLeft.y + corner.y,
                frontBottomLeft.x + corner.x] >= ISOSURFACE_THRESHOLD)
            {
                lookupCaseIndex |= i;
            }
            i <<= 1;
        }
        int previousTrianglesCount = triangles.Count;
        int[] trianglesEdges = LookupTables.TRIANGULATION[lookupCaseIndex];
        for (i = 0; trianglesEdges[i] >= 0; i += 3)
        {
            for (int j = i; j < i + 3; j++)
            {
                int triangleEdge = trianglesEdges[j];
                Vector3Int cornerA = LookupTables.CORNERS[LookupTables.EDGE_TO_CORNER_A[triangleEdge]];
                Vector3Int cornerB = LookupTables.CORNERS[LookupTables.EDGE_TO_CORNER_B[triangleEdge]];
                Vector3 vertex = ((Vector3)(cornerA + cornerB)) / 2f + frontBottomLeft;
                vertices.Add(vertex);
            }
            triangles.Add(previousTrianglesCount + i + 2);
            triangles.Add(previousTrianglesCount + i + 1);
            triangles.Add(previousTrianglesCount + i + 0);
        }
    }
}
