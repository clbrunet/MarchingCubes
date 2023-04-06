using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [SerializeField, Range(1, 12)]
    private int chunkViewDistance = 4;
    [SerializeField]
    private Transform chunksParent;
    [SerializeField]
    private Chunk chunkPrefab;
    [SerializeField, Range(1, 100)]
    private int maxChunksRecycledPerFrame = 20;
    [Range(0.00001f, 64f)]
    public float axisSize = 16f;
    [Range(4, 16)]
    public uint axisSegmentCount = 16;
    public float noiseScale = 1f;
    [Range(0.0f, 1.0f)]
    public float isosurfaceThreshold = 0.7f;

    private Transform viewer;
    private float chunksUpdateMinimumViewerMovementsSquared;
    private Vector3 lastChunksUpdateViewerPosition;
    private readonly Dictionary<Vector3Int, Chunk> chunks = new();
    private readonly HashSet<Vector3Int> chunksToAdd = new();
    private readonly HashSet<Vector3Int> chunksToRemove = new();

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
        viewer = Camera.main.transform;
    }

    private void Start()
    {
        ReloadChunks();
    }

    public void ReloadChunks()
    {
        foreach (Chunk chunk in chunks.Values)
        {
            Destroy(chunk.gameObject);
        }
        chunks.Clear();
        chunksToRemove.Clear();
        chunksToAdd.Clear();
        chunksUpdateMinimumViewerMovementsSquared = axisSize * axisSize / 4f;
        UpdateChunksToAddAndToRemove();
        foreach (Vector3Int chunkToAdd in chunksToAdd)
        {
            Chunk chunk = Instantiate(chunkPrefab, (Vector3)chunkToAdd * axisSize, Quaternion.identity, chunksParent);
            chunk.RegenerateAsync(chunkToAdd);
            chunks.Add(chunkToAdd, chunk);
        }
        chunksToAdd.Clear();
    }

    private void Update()
    {
        if ((viewer.position - lastChunksUpdateViewerPosition).sqrMagnitude
            > chunksUpdateMinimumViewerMovementsSquared)
        {
            UpdateChunksToAddAndToRemove();
        }
        RecycleChunks();
    }

    private void UpdateChunksToAddAndToRemove()
    {
        chunksToRemove.UnionWith(chunks.Keys);
        chunksToAdd.Clear();
        Vector3Int cameraChunkCoordinate = Vector3Int.RoundToInt(viewer.position / axisSize);
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
    }

    private void RecycleChunks()
    {
        for (int i = Mathf.Min(maxChunksRecycledPerFrame, chunksToAdd.Count); i > 0; i--)
        {
            Vector3Int chunkToRemove = chunksToRemove.ElementAt(0);
            Vector3Int chunkToAdd = chunksToAdd.ElementAt(0);
            Chunk chunk = chunks[chunkToRemove];
            chunk.RegenerateAsync(chunkToAdd);
            chunks[chunkToAdd] = chunk;
            chunksToAdd.Remove(chunkToAdd);
            chunksToRemove.Remove(chunkToRemove);
            chunks.Remove(chunkToRemove);
        }
    }
}
