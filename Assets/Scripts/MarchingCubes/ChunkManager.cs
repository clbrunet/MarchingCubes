using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public struct ChunkTriangle
{
    public Vector3 vertexA;
    public Vector3 vertexB;
    public Vector3 vertexC;
};

[RequireComponent(typeof(MeshCollider))]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [SerializeField, Range(1, 12)]
    private int chunkViewDistance = 6;
    [SerializeField]
    private Transform chunksParent;
    [SerializeField]
    private Chunk chunkPrefab;
    [SerializeField, Range(1, 32)]
    private int maxChunksRecycledPerFrame = 8;
    [Range(0.00001f, 64f)]
    public float axisSize = 16f;
    [Range(4, 16)]
    public uint axisSegmentCount = 16;
    public float noiseScale = 1f;
    [Range(0.0f, 1.0f)]
    public float isosurfaceThreshold = 0.5f;

    [SerializeField]
    private ComputeShader chunkComputeShader;
    private int generateNoiseValuesKernel;
    private int generateMeshDataKernel;
    private readonly int coordinateId = Shader.PropertyToID("_Coordinate");
    private ComputeBuffer noiseValuesBuffer;
    private const int CASE_MAX_TRIANGLES_COUNT = 15;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer trianglesCountBuffer;
    private readonly int[] trianglesCounts = new int[1];
    private ChunkTriangle[] triangles;

    private MeshCollider meshCollider;
    private Transform viewer;
    private float chunksUpdateMinimumViewerMovementsSquared;
    private Vector3 lastChunksUpdateViewerPosition;
    private readonly Dictionary<Vector3Int, Chunk> chunks = new();
    private readonly HashSet<Vector3Int> chunksToAdd = new();
    private readonly HashSet<Vector3Int> chunksToRemove = new();

    [HideInInspector]
    public float editRadius;
    private const float EDIT_SPEED = 2f;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        generateNoiseValuesKernel = chunkComputeShader.FindKernel("GenerateNoiseValues");
        generateMeshDataKernel = chunkComputeShader.FindKernel("GenerateMeshData");

        meshCollider = GetComponent<MeshCollider>();
        viewer = Camera.main.transform;
    }

    private void Start()
    {
        chunksUpdateMinimumViewerMovementsSquared = axisSize * axisSize / 4f;
        chunkComputeShader.SetInt("_AxisSegmentCount", (int)axisSegmentCount);
        chunkComputeShader.SetFloat("_NoiseScale", noiseScale);
        chunkComputeShader.SetFloat("_AxisSize", axisSize);
        chunkComputeShader.SetFloat("_IsosurfaceThreshold", isosurfaceThreshold);
        noiseValuesBuffer = new ComputeBuffer((int)Mathf.Pow(axisSegmentCount + 1, 3), sizeof(float));
        chunkComputeShader.SetBuffer(generateNoiseValuesKernel, "_NoiseValues", noiseValuesBuffer);
        chunkComputeShader.SetBuffer(generateMeshDataKernel, "_NoiseValues", noiseValuesBuffer);
        trianglesBuffer = new ComputeBuffer((int)Mathf.Pow(axisSegmentCount, 3) * CASE_MAX_TRIANGLES_COUNT,
            3 * 3 * sizeof(float), ComputeBufferType.Append);
        chunkComputeShader.SetBuffer(generateMeshDataKernel, "_Triangles", trianglesBuffer);
        triangles = new ChunkTriangle[(int)Mathf.Pow(axisSegmentCount, 3) * CASE_MAX_TRIANGLES_COUNT];
        trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        UpdateChunksToAddAndToRemove();
        foreach (Vector3Int chunkToAdd in chunksToAdd)
        {
            Chunk chunk = Instantiate(chunkPrefab, (Vector3)chunkToAdd * axisSize, Quaternion.identity, chunksParent);
            GenerateChunk(chunk, chunkToAdd);
            chunks.Add(chunkToAdd, chunk);
        }
        chunksToAdd.Clear();
        editRadius = axisSize / 2f;
        Edit(viewer.position, -1.00001f + isosurfaceThreshold);
        editRadius = axisSize / 4f;
    }

    private void OnDestroy()
    {
        noiseValuesBuffer.Release();
        trianglesBuffer.Release();
        trianglesCountBuffer.Release();
    }

    private void Update()
    {
        if ((viewer.position - lastChunksUpdateViewerPosition).sqrMagnitude
            > chunksUpdateMinimumViewerMovementsSquared)
        {
            UpdateChunksToAddAndToRemove();
        }
        RecycleChunks();
        if (Input.GetMouseButton(0) != Input.GetMouseButton(1))
        {
            if (Physics.Raycast(viewer.position, viewer.forward, out RaycastHit hit))
            {
                float change = EDIT_SPEED * Time.deltaTime;
                if (Input.GetMouseButton(0))
                {
                    change = -change;
                }
                Edit(hit.point, change);
            }
        }
    }

    private void UpdateChunksToAddAndToRemove()
    {
        chunksToRemove.UnionWith(chunks.Keys);
        chunksToAdd.Clear();
        Vector3Int cameraChunkCoordinate = Vector3Int.FloorToInt(viewer.position / axisSize);
        int radius = chunkViewDistance - 1;
        Vector3Int frontBottomLeft = new(cameraChunkCoordinate.x - radius,
            cameraChunkCoordinate.y - radius, cameraChunkCoordinate.z - radius);
        Vector3Int backTopRight = new(cameraChunkCoordinate.x + radius,
            cameraChunkCoordinate.y + radius, cameraChunkCoordinate.z + radius);
        for (int z = frontBottomLeft.z; z <= backTopRight.z; z++)
        {
            for (int y = frontBottomLeft.y; y <= backTopRight.y; y++)
            {
                for (int x = frontBottomLeft.x; x <= backTopRight.x; x++)
                {
                    Vector3Int coordinate = new(x, y, z);
                    if (!chunks.ContainsKey(coordinate))
                    {
                        chunksToAdd.Add(coordinate);
                    }
                    chunksToRemove.Remove(coordinate);
                }
            }
        }
        lastChunksUpdateViewerPosition = viewer.position;
        UpdateBorder(frontBottomLeft, backTopRight);
    }

    private void UpdateBorder(Vector3Int frontBottomLeftCoordinate, Vector3Int backTopRightCoordinate)
    {
        Vector3 frontBottomLeft = axisSize * (Vector3)frontBottomLeftCoordinate;
        Vector3 backTopRight = axisSize * (Vector3)(backTopRightCoordinate + Vector3Int.one);
        Mesh collisionMesh = new()
        {
            vertices = new[]
            {
                // bottom
                new Vector3(frontBottomLeft.x, frontBottomLeft.y, frontBottomLeft.z),
                new Vector3(frontBottomLeft.x, frontBottomLeft.y, backTopRight.z),
                new Vector3(backTopRight.x,    frontBottomLeft.y, backTopRight.z),
                new Vector3(backTopRight.x,    frontBottomLeft.y, frontBottomLeft.z),
                // top
                new Vector3(frontBottomLeft.x, backTopRight.y, frontBottomLeft.z),
                new Vector3(frontBottomLeft.x, backTopRight.y, backTopRight.z),
                new Vector3(backTopRight.x,    backTopRight.y, backTopRight.z),
                new Vector3(backTopRight.x,    backTopRight.y, frontBottomLeft.z),
            },
            triangles = new[]
            {
                // bottom
                0, 1, 2,
                0, 2, 3,
                // top
                6, 5, 4,
                6, 4, 7,
                // front
                0, 3, 4,
                7, 4, 3,
                // back
                1, 5, 2,
                2, 5, 6,
                // left
                0, 4, 1,
                1, 4, 5,
                // right
                2, 6, 7,
                2, 7, 3,
            },
        };
        collisionMesh.RecalculateNormals();
        meshCollider.sharedMesh = collisionMesh;
    }

    private void RecycleChunks()
    {
        for (int i = Mathf.Min(maxChunksRecycledPerFrame, chunksToAdd.Count); i > 0; i--)
        {
            Vector3Int chunkToRemove = chunksToRemove.ElementAt(0);
            Vector3Int chunkToAdd = chunksToAdd.ElementAt(0);
            Chunk chunk = chunks[chunkToRemove];
            GenerateChunk(chunk, chunkToAdd);
            chunks[chunkToAdd] = chunk;
            chunksToAdd.Remove(chunkToAdd);
            chunksToRemove.Remove(chunkToRemove);
            chunks.Remove(chunkToRemove);
        }
    }

    private void GenerateChunk(Chunk chunk, Vector3Int coordinate)
    {
        chunkComputeShader.SetVector(coordinateId, (Vector3)coordinate);
        trianglesBuffer.SetCounterValue(0);
        int generateNoiseValuesThreadGroups = Mathf.CeilToInt((float)(axisSegmentCount + 1) / 4f);
        int generateMeshDataThreadGroups = Mathf.CeilToInt((float)axisSegmentCount / 4f);
        chunkComputeShader.Dispatch(generateNoiseValuesKernel, generateNoiseValuesThreadGroups,
            generateNoiseValuesThreadGroups, generateNoiseValuesThreadGroups);
        chunkComputeShader.Dispatch(generateMeshDataKernel, generateMeshDataThreadGroups,
            generateMeshDataThreadGroups, generateMeshDataThreadGroups);

        ComputeBuffer.CopyCount(trianglesBuffer, trianglesCountBuffer, 0);
        trianglesCountBuffer.GetData(trianglesCounts);
        int trianglesCount = trianglesCounts[0];
        trianglesBuffer.GetData(triangles, 0, 0, trianglesCount);
        chunk.Regenerate(coordinate, noiseValuesBuffer, triangles, trianglesCount);
    }

    private void Edit(Vector3 center, float change)
    {
        Vector3 centerOffset = new Vector3(editRadius, editRadius, editRadius)
            + (axisSize / axisSegmentCount) * Vector3.one;
        Vector3Int frontBottomLeft = Vector3Int.FloorToInt((center - centerOffset) / axisSize);
        Vector3Int backTopRight = Vector3Int.FloorToInt((center + centerOffset) / axisSize);
        if (!chunks.TryGetValue(frontBottomLeft, out _)
            || !chunks.TryGetValue(backTopRight, out _))
        {
            return;
        }
        for (int z = frontBottomLeft.z; z <= backTopRight.z; z++)
        {
            for (int y = frontBottomLeft.y; y <= backTopRight.y; y++)
            {
                for (int x = frontBottomLeft.x; x <= backTopRight.x; x++)
                {
                    Vector3Int coordinate = new(x, y, z);
                    Chunk chunk = chunks[coordinate];
                    if (chunk.Edit(center - axisSize * (Vector3)coordinate, change, noiseValuesBuffer))
                    {
                        GenerateChunkMesh(chunk);
                    }
                }
            }
        }
    }

    private void GenerateChunkMesh(Chunk chunk)
    {
        trianglesBuffer.SetCounterValue(0);
        int generateMeshDataThreadGroups = Mathf.CeilToInt((float)axisSegmentCount / 4f);
        chunkComputeShader.Dispatch(generateMeshDataKernel, generateMeshDataThreadGroups,
            generateMeshDataThreadGroups, generateMeshDataThreadGroups);

        ComputeBuffer.CopyCount(trianglesBuffer, trianglesCountBuffer, 0);
        trianglesCountBuffer.GetData(trianglesCounts);
        int trianglesCount = trianglesCounts[0];
        trianglesBuffer.GetData(triangles, 0, 0, trianglesCount);
        chunk.RegenerateMesh(triangles, trianglesCount);
    }
}
