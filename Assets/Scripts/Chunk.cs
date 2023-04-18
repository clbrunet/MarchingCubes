using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

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

    [SerializeField]
    private ComputeShader computeShader;
    private readonly static int axisSegmentCountId = Shader.PropertyToID("_AxisSegmentCount");
    private readonly static int noiseScaleId = Shader.PropertyToID("_NoiseScale");
    private readonly static int coordinateId = Shader.PropertyToID("_Coordinate");
    private readonly static int noiseValuesId = Shader.PropertyToID("_NoiseValues");
    private ComputeBuffer noiseValuesBuffer;

    private MeshFilter meshFilter;
    private ChunkManager manager;
    private Vector3Int coordinate;
    private float[,,] noiseValues;
    private MeshData meshData;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        manager = ChunkManager.Instance;
        Assert.IsNotNull(manager);
        noiseValuesBuffer = new ComputeBuffer((int)Mathf.Pow(manager.axisSegmentCount + 1, 3), sizeof(float));
        noiseValues = new float[manager.axisSegmentCount + 1, manager.axisSegmentCount + 1,
            manager.axisSegmentCount + 1];
        meshData = new MeshData(new List<Vector3>(), new List<int>());
    }

    private void OnDestroy()
    {
        noiseValuesBuffer.Release();
    }

    public void Regenerate(Vector3Int coordinate)
    {
        if (meshFilter.mesh != null)
        {
            meshFilter.mesh.Clear();
        }
        this.coordinate = coordinate;
        transform.position = (Vector3)coordinate * manager.axisSize;
        RegenerateNoiseValues(() =>
        {
            RegenerateMeshData();
            Mesh mesh = new()
            {
                vertices = meshData.vertices.ToArray(),
                triangles = meshData.triangles.ToArray(),
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        });
    }

    private void RegenerateNoiseValues(Action callback)
    {
        computeShader.SetInt(axisSegmentCountId, (int)manager.axisSegmentCount);
        computeShader.SetFloat(noiseScaleId, manager.noiseScale);
        computeShader.SetVector(coordinateId, (Vector3)coordinate);
        computeShader.SetBuffer(0, noiseValuesId, noiseValuesBuffer);
        int threadGroups = Mathf.CeilToInt((float)manager.axisSegmentCount + 1 / 4f);
        computeShader.Dispatch(0, threadGroups, threadGroups, threadGroups);
        AsyncGPUReadback.Request(noiseValuesBuffer, (req) =>
        {
            noiseValuesBuffer.GetData(noiseValues);
            callback?.Invoke();
        });

    }

    private void RegenerateMeshData()
    {
        meshData.vertices.Clear();
        meshData.triangles.Clear();
        for (int z = 0; z < manager.axisSegmentCount; z++)
        {
            for (int y = 0; y < manager.axisSegmentCount; y++)
            {
                for (int x = 0; x < manager.axisSegmentCount; x++)
                {
                    RegenerateMeshDataCube(new Vector3Int(x, y, z), meshData.vertices, meshData.triangles);
                }
            }
        }
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
                Vector3 vertex = manager.axisSize / (float)manager.axisSegmentCount
                    * ((Vector3)(cornerA + cornerB) / 2f + frontBottomLeft);
                vertices.Add(vertex);
            }
            triangles.Add(previousTrianglesCount + i + 2);
            triangles.Add(previousTrianglesCount + i + 1);
            triangles.Add(previousTrianglesCount + i + 0);
        }
    }
}
