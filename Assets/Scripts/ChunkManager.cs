using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum CornersGeneration
{
    None,
    In,
    Out,
    All,
}

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [SerializeField]
    [Range(1, 20)]
    private int chunkViewDistance = 4;
    [SerializeField]
    private Transform chunksParent;
    [SerializeField]
    private Chunk chunkPrefab;
    [Range(0.00001f, 64f)]
    public float sideSize = 16f;
    [Range(1, 64)]
    public uint sideSegmentCount = 16;
    public float noiseScale = 1f;
    [Range(0.0f, 1.0f)]
    public float isosurfaceThreshold = 0.7f;
    public CornersGeneration cornersGeneration;
    public Mesh cornerMesh;
    public Material cornerMaterial;

    public new Camera camera;
    private readonly List<Chunk> chunks = new();

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
        camera = Camera.main;
    }

    private void Start()
    {
        LoadChunks();
    }

    private void LoadChunks()
    {
        Vector3Int cameraChunkCoordinate = Vector3Int.RoundToInt(camera.transform.position / sideSize);
        int radius = chunkViewDistance / 2;
        Vector3Int frontBottomLeft = new Vector3Int(cameraChunkCoordinate.x - radius,
            cameraChunkCoordinate.y - radius, cameraChunkCoordinate.z - radius);
        Vector3Int backTopRight = new Vector3Int(cameraChunkCoordinate.x + radius,
            cameraChunkCoordinate.y + radius, cameraChunkCoordinate.z + radius);
        for (int z = frontBottomLeft.z; z < backTopRight.z; z++)
        {
            for (int y = frontBottomLeft.y; y < backTopRight.y; y++)
            {
                for (int x = frontBottomLeft.x; x < backTopRight.x; x++)
                {
                    AddChunk(new Vector3Int(x, y, z));
                }
            }
        }
    }

    private void AddChunk(Vector3Int coordinate)
    {
        chunks.Add(Instantiate(chunkPrefab, (Vector3)coordinate * sideSize, Quaternion.identity, chunksParent));
    }

    public void Regenerate()
    {
        foreach (Chunk chunk in chunks)
        {
            chunk.Regenerate();
        }
    }
}
