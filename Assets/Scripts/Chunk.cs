using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private enum CornersGeneration
    {
        None,
        In,
        Out,
        All,
    }

    [SerializeField]
    [Range(0f, 64f)]
    private float sideSize = 16f;
    [SerializeField]
    [Range(2, 64)]
    private uint sideDimension = 16;
    [SerializeField]
    private float noiseScale = 1f;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float isosurfaceThreshold = 0.7f;
    [SerializeField]
    private CornersGeneration cornersGeneration;
    [SerializeField]
    private Mesh cornerMesh;
    [SerializeField]
    private Material cornerMaterial;

    private MeshFilter meshFilter;

    private float[,,] noiseValues;
    private List<List<Matrix4x4>> cornerMatricesBatches = null;
    private List<MaterialPropertyBlock> cornerPropertiesBatches = null;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        Regenerate();
    }

    private void Update()
    {
        DrawCorners();
    }

    private void DrawCorners()
    {
        if (cornerMatricesBatches == null)
        {
            return;
        }
        for (int i = 0; i < cornerMatricesBatches.Count; i++)
        {
            List<Matrix4x4> cornerMatricesBatch = cornerMatricesBatches[i];
            MaterialPropertyBlock cornerPropertiesBatch = cornerPropertiesBatches[i];
            for (int j = 0; j < cornerMesh.subMeshCount; j++)
            {
                Graphics.DrawMeshInstanced(cornerMesh, j, cornerMaterial, cornerMatricesBatch, cornerPropertiesBatch);
            }
        }
    }

    public void Regenerate()
    {
        GenerateNoiseValues();
        GenerateCorners();
        GenerateMesh();
    }

    private void GenerateNoiseValues()
    {
        noiseValues = new float[sideDimension, sideDimension, sideDimension];
        for (uint z = 0; z < sideDimension; z++)
        {
            for (uint y = 0; y < sideDimension; y++)
            {
                for (uint x = 0; x < sideDimension; x++)
                {
                    float3 coordinate = noiseScale / (float)sideDimension * new float3(x, y, z);
                    float value = math.unlerp(-1f, 1f, noise.snoise(coordinate));
                    noiseValues[z, y, x] = value;
                }
            }
        }
    }

    private void GenerateCorners()
    {
        if (cornersGeneration == CornersGeneration.None)
        {
            cornerMatricesBatches = null;
            cornerPropertiesBatches = null;
            return;
        }
        int maxCornersCount = (int)(sideDimension * sideDimension * sideDimension);
        const int INSTANCES_COUNT_LIMIT = 1020;
        int batchesCount = maxCornersCount / INSTANCES_COUNT_LIMIT;
        if (maxCornersCount % INSTANCES_COUNT_LIMIT != 0)
        {
            batchesCount++;
        }
        cornerMatricesBatches = new List<List<Matrix4x4>>(batchesCount);
        cornerPropertiesBatches = new List<MaterialPropertyBlock>(batchesCount);
        List<Vector4> colors = null;
        int batch = 0;
        for (uint z = 0; z < sideDimension; z++)
        {
            for (uint y = 0; y < sideDimension; y++)
            {
                for (uint x = 0; x < sideDimension; x++)
                {
                    float value = noiseValues[z, y, x];
                    if ((cornersGeneration == CornersGeneration.In && value < isosurfaceThreshold)
                        || (cornersGeneration == CornersGeneration.Out && isosurfaceThreshold <= value))
                    {
                        continue;
                    }
                    if (batch == cornerMatricesBatches.Count)
                    {
                        cornerMatricesBatches.Add(new List<Matrix4x4>(INSTANCES_COUNT_LIMIT));
                        if (cornerPropertiesBatches.Count > 0)
                        {
                            cornerPropertiesBatches[^1]
                                .SetVectorArray("_BaseColor", colors);
                        }
                        cornerPropertiesBatches.Add(new MaterialPropertyBlock());
                        colors = new List<Vector4>(INSTANCES_COUNT_LIMIT);
                    }
                    Vector3 localPosition = sideSize / (float)sideDimension * new Vector3(x, y, z);
                    cornerMatricesBatches[batch].Add(Matrix4x4.TRS(transform.position + localPosition,
                        Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f)));
                    colors.Add(new Vector4(value, value, value, 1f));
                    if (cornerMatricesBatches[batch].Count == INSTANCES_COUNT_LIMIT)
                    {
                        batch++;
                    }
                }
            }
        }
        cornerPropertiesBatches[^1].SetVectorArray("_BaseColor", colors);
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int z = 0; z < sideDimension - 1; z++)
        {
            for (int y = 0; y < sideDimension - 1; y++)
            {
                for (int x = 0; x < sideDimension - 1; x++)
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
                frontBottomLeft.x + corner.x] >= isosurfaceThreshold)
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
                Vector3 vertex = sideSize / (float)sideDimension
                    * ((Vector3)(cornerA + cornerB) / 2f + frontBottomLeft);
                vertices.Add(vertex);
            }
            triangles.Add(previousTrianglesCount + i + 2);
            triangles.Add(previousTrianglesCount + i + 1);
            triangles.Add(previousTrianglesCount + i + 0);
        }
    }
}
