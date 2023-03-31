using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;
using System.Threading.Tasks;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private struct MeshData
    {
        public List<Vector3> vertices;
        public List<int> triangles;

        public MeshData(List<Vector3> vertices, List<int> triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
    }

    private MeshFilter meshFilter;
    private ChunkManager manager;
    private float[,,] noiseValues;
    private Vector3Int coordinate;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        manager = ChunkManager.Instance;
        coordinate = Vector3Int.RoundToInt(transform.position / manager.sideSize);
        RegenerateAsync();
    }

    public async void RegenerateAsync()
    {
        MeshData meshData = await Task.Run(() =>
        {
            RegenerateNoiseValues();
            return RegenerateMeshData();
        });
        ApplyGenerationData(meshData);
    }

    private void ApplyGenerationData(MeshData meshData)
    {
        meshFilter.mesh.vertices = meshData.vertices.ToArray();
        meshFilter.mesh.triangles = meshData.triangles.ToArray();
        meshFilter.mesh.RecalculateNormals();
    }

    private void RegenerateNoiseValues()
    {
        noiseValues = new float[manager.sideSegmentCount + 1, manager.sideSegmentCount + 1, manager.sideSegmentCount + 1];
        for (uint z = 0; z < manager.sideSegmentCount + 1; z++)
        {
            for (uint y = 0; y < manager.sideSegmentCount + 1; y++)
            {
                for (uint x = 0; x < manager.sideSegmentCount + 1; x++)
                {
                    float3 noiseCoordinate = (float3)(Vector3)coordinate +
                        manager.noiseScale / (float)manager.sideSegmentCount * new float3(x, y, z);
                    float value = math.unlerp(-1f, 1f, noise.snoise(noiseCoordinate));
                    noiseValues[z, y, x] = value;
                }
            }
        }
    }

    private MeshData RegenerateMeshData()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int z = 0; z < manager.sideSegmentCount; z++)
        {
            for (int y = 0; y < manager.sideSegmentCount; y++)
            {
                for (int x = 0; x < manager.sideSegmentCount; x++)
                {
                    RegenerateMeshDataCube(new Vector3Int(x, y, z), vertices, triangles);
                }
            }
        }
        return new MeshData(vertices, triangles);
    }

    private void RegenerateMeshDataCube(Vector3Int frontBottomLeft, List<Vector3> vertices, List<int> triangles)
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
                Vector3 vertex = manager.sideSize / (float)manager.sideSegmentCount
                    * ((Vector3)(cornerA + cornerB) / 2f + frontBottomLeft);
                vertices.Add(vertex);
            }
            triangles.Add(previousTrianglesCount + i + 2);
            triangles.Add(previousTrianglesCount + i + 1);
            triangles.Add(previousTrianglesCount + i + 0);
        }
    }
}
