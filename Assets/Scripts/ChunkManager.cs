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

    private readonly List<Chunk> chunks = new();

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Start()
    {
        AddChunk(new Vector3(0f, 0f, 0f));
        AddChunk(new Vector3(0f, -1, 0f));
        AddChunk(new Vector3(-1, -1, 0f));
        AddChunk(new Vector3(-1, 0f, 0f));
        AddChunk(new Vector3(0f, 0f, -1));
        AddChunk(new Vector3(0f, -1, -1));
        AddChunk(new Vector3(-1, -1, -1));
        AddChunk(new Vector3(-1, 0f, -1));
    }

    private void AddChunk(Vector3 coordinate)
    {
        chunks.Add(Instantiate(chunkPrefab, coordinate * sideSize, Quaternion.identity, chunksParent));
    }

    public void Regenerate()
    {
        foreach (Chunk chunk in chunks)
        {
            chunk.Regenerate();
        }
    }
}
