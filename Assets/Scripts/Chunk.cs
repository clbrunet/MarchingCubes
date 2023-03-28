using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private struct CornerBatch
    {
        public List<Matrix4x4> matrices;
        public MaterialPropertyBlock properties;

        public CornerBatch(List<Matrix4x4> matrices, MaterialPropertyBlock properties)
        {
            this.matrices = matrices;
            this.properties = properties;
        }
    }

    private MeshFilter meshFilter;
    private ChunkManager manager;
    private float[,,] noiseValues;
    private List<CornerBatch> cornerBatches = null;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        manager = ChunkManager.Instance;
        Regenerate();
    }

    private void Update()
    {
        DrawCorners();
    }

    private void DrawCorners()
    {
        if (cornerBatches == null)
        {
            return;
        }
        foreach (CornerBatch cornerBatch in cornerBatches)
        {
            for (int i = 0; i < manager.cornerMesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstanced(manager.cornerMesh, i, manager.cornerMaterial,
                    cornerBatch.matrices, cornerBatch.properties);
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
        noiseValues = new float[manager.sideDimension, manager.sideDimension, manager.sideDimension];
        for (uint z = 0; z < manager.sideDimension; z++)
        {
            for (uint y = 0; y < manager.sideDimension; y++)
            {
                for (uint x = 0; x < manager.sideDimension; x++)
                {
                    float3 coordinate = manager.noiseScale / (float)manager.sideDimension * new float3(x, y, z);
                    float value = math.unlerp(-1f, 1f, noise.snoise(coordinate));
                    noiseValues[z, y, x] = value;
                }
            }
        }
    }

    private void GenerateCorners()
    {
        if (manager.cornersGeneration == CornersGeneration.None)
        {
            cornerBatches = null;
            return;
        }
        int maxCornersCount = (int)(manager.sideDimension * manager.sideDimension * manager.sideDimension);
        const int INSTANCES_COUNT_LIMIT = 1020;
        int batchesCount = maxCornersCount / INSTANCES_COUNT_LIMIT;
        if (maxCornersCount % INSTANCES_COUNT_LIMIT != 0)
        {
            batchesCount++;
        }
        cornerBatches = new List<CornerBatch>(batchesCount);
        List<Vector4> colors = null;
        int colorPropertyId = Shader.PropertyToID("_BaseColor");
        int batch = 0;
        for (uint z = 0; z < manager.sideDimension; z++)
        {
            for (uint y = 0; y < manager.sideDimension; y++)
            {
                for (uint x = 0; x < manager.sideDimension; x++)
                {
                    float value = noiseValues[z, y, x];
                    if ((manager.cornersGeneration == CornersGeneration.In && value < manager.isosurfaceThreshold)
                        || (manager.cornersGeneration == CornersGeneration.Out && manager.isosurfaceThreshold <= value))
                    {
                        continue;
                    }
                    if (batch == cornerBatches.Count)
                    {
                        cornerBatches.Add(new CornerBatch(new List<Matrix4x4>(INSTANCES_COUNT_LIMIT),
                            new MaterialPropertyBlock()));
                        colors = new List<Vector4>(INSTANCES_COUNT_LIMIT);
                    }
                    Vector3 localPosition = manager.sideSize / (float)manager.sideDimension * new Vector3(x, y, z);
                    cornerBatches[batch].matrices.Add(Matrix4x4.TRS(transform.position + localPosition,
                        Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f)));
                    colors.Add(new Vector4(value, value, value, 1f));
                    if (colors.Count == INSTANCES_COUNT_LIMIT)
                    {
                        cornerBatches[batch].properties.SetVectorArray(colorPropertyId, colors);
                        batch++;
                    }
                }
            }
        }
        cornerBatches[^1].properties.SetVectorArray(colorPropertyId, colors);
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int z = 0; z < manager.sideDimension - 1; z++)
        {
            for (int y = 0; y < manager.sideDimension - 1; y++)
            {
                for (int x = 0; x < manager.sideDimension - 1; x++)
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
                frontBottomLeft.x + corner.x] >= manager.isosurfaceThreshold)
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
                Vector3 vertex = manager.sideSize / (float)manager.sideDimension
                    * ((Vector3)(cornerA + cornerB) / 2f + frontBottomLeft);
                vertices.Add(vertex);
            }
            triangles.Add(previousTrianglesCount + i + 2);
            triangles.Add(previousTrianglesCount + i + 1);
            triangles.Add(previousTrianglesCount + i + 0);
        }
    }
}
