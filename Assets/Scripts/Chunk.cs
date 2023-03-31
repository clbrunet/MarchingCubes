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

    private struct CornerBatchData
    {
        public int capacity;
        public List<Matrix4x4> matrices;
        public List<Vector4> colors;

        public CornerBatchData(int capacity)
        {
            this.capacity = capacity;
            matrices = new List<Matrix4x4>(capacity);
            colors = new List<Vector4>(capacity);
        }
    }

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
    private List<CornerBatch> cornerBatches = null;
    private Vector3Int coordinate;
    private Vector3 position;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        manager = ChunkManager.Instance;
        coordinate = Vector3Int.RoundToInt(transform.position / manager.sideSize);
        position = transform.position;
        RegenerateAsync();
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

    public async void RegenerateAsync()
    {
        (MeshData meshData, List<CornerBatchData> cornerBatchesData) = await Task.Run(() =>
        {
            GenerateNoiseValues();
            return (GenerateMeshData(), GenerateCornersData());
        });
        ApplyGenerationData(meshData, cornerBatchesData);
    }

    public void Regenerate()
    {
        GenerateNoiseValues();
        MeshData meshData = GenerateMeshData();
        List<CornerBatchData> cornerBatchesData = GenerateCornersData();
        ApplyGenerationData(meshData, cornerBatchesData);
    }

    private void ApplyGenerationData(MeshData meshData, List<CornerBatchData> cornerBatchesData)
    {
        meshFilter.mesh.vertices = meshData.vertices.ToArray();
        meshFilter.mesh.triangles = meshData.triangles.ToArray();
        meshFilter.mesh.RecalculateNormals();
        if (cornerBatchesData == null)
        {
            cornerBatches = null;
            return;
        }
        int colorPropertyId = Shader.PropertyToID("_BaseColor");
        cornerBatches = new List<CornerBatch>(cornerBatchesData.Count);
        foreach (CornerBatchData cornerBatchData in cornerBatchesData)
        {
            MaterialPropertyBlock properties = new();
            properties.SetVectorArray(colorPropertyId, cornerBatchData.colors);
            cornerBatches.Add(new CornerBatch(cornerBatchData.matrices, properties));
        }
    }

    private void GenerateNoiseValues()
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

    private List<CornerBatchData> GenerateCornersData()
    {
        if (manager.cornersGeneration == CornersGeneration.None)
        {
            return null;
        }
        int maxCornersCount = (int)(manager.sideSegmentCount + 1 * manager.sideSegmentCount + 1 * manager.sideSegmentCount + 1);
        const int INSTANCES_COUNT_LIMIT = 1020;
        int batchesCount = maxCornersCount / INSTANCES_COUNT_LIMIT;
        if (maxCornersCount % INSTANCES_COUNT_LIMIT != 0)
        {
            batchesCount++;
        }
        List<CornerBatchData> cornerBatchesData = new(batchesCount);
        int batch = 0;
        for (uint z = 0; z < manager.sideSegmentCount + 1; z++)
        {
            for (uint y = 0; y < manager.sideSegmentCount + 1; y++)
            {
                for (uint x = 0; x < manager.sideSegmentCount + 1; x++)
                {
                    float value = noiseValues[z, y, x];
                    if ((manager.cornersGeneration == CornersGeneration.In && value < manager.isosurfaceThreshold)
                        || (manager.cornersGeneration == CornersGeneration.Out && manager.isosurfaceThreshold <= value))
                    {
                        continue;
                    }
                    if (batch == cornerBatchesData.Count)
                    {
                        cornerBatchesData.Add(new CornerBatchData(INSTANCES_COUNT_LIMIT));
                    }
                    Vector3 localPosition = manager.sideSize / (float)manager.sideSegmentCount * new Vector3(x, y, z);
                    cornerBatchesData[batch].matrices.Add(Matrix4x4.TRS(position + localPosition,
                        Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f)));
                    cornerBatchesData[batch].colors.Add(new Vector4(value, value, value, 1f));
                    if (cornerBatchesData[batch].matrices.Count == INSTANCES_COUNT_LIMIT)
                    {
                        batch++;
                    }
                }
            }
        }
        return cornerBatchesData;
    }

    private MeshData GenerateMeshData()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int z = 0; z < manager.sideSegmentCount; z++)
        {
            for (int y = 0; y < manager.sideSegmentCount; y++)
            {
                for (int x = 0; x < manager.sideSegmentCount; x++)
                {
                    GenerateMeshDataCube(new Vector3Int(x, y, z), vertices, triangles);
                }
            }
        }
        return new MeshData(vertices, triangles);
    }

    private void GenerateMeshDataCube(Vector3Int frontBottomLeft, List<Vector3> vertices, List<int> triangles)
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
