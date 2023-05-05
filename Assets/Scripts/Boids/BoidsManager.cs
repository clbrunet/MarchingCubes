using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BoidsManager : MonoBehaviour
{
    public static BoidsManager Instance { get; private set; }

    [SerializeField]
    private Transform boidsParent;
    [SerializeField]
    private Boid boidPrefab;
    public float boidThreshold = 0.1f;
    public int maxBoidsPerChunk = 2;
    private int maxBoidsCount = int.MaxValue;

    [HideInInspector]
    public readonly List<Boid> boids = new();
    private bool firstUpdateAfterBoidsChange = true;

    private struct BoidInput
    {
        public Vector3 position;
        public Vector3 targetForward;
    }
    private BoidInput[] boidsInputs;

    private struct BoidOutput
    {
        public Vector3 targetForward;
    }
    private BoidOutput[] boidsOutputs;

    [SerializeField]
    private ComputeShader computeShader;
    private int boidAIKernel;
    private ComputeBuffer boidsInputsBuffer;
    private ComputeBuffer boidsOuputsBuffer;
    private readonly int boidsCountId = Shader.PropertyToID("_BoidsCount");

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Start()
    {
        boidAIKernel = computeShader.FindKernel("BoidAI");
        maxBoidsCount = ChunksManager.Instance.GetChunksCount() * maxBoidsPerChunk;
        boidsInputs = new BoidInput[maxBoidsCount];
        boidsOutputs = new BoidOutput[maxBoidsCount];
        boidsInputsBuffer = new ComputeBuffer(maxBoidsCount, sizeof(float) * 3 * 2);
        computeShader.SetBuffer(boidAIKernel, "_BoidsInputs", boidsInputsBuffer);
        boidsOuputsBuffer = new ComputeBuffer(maxBoidsCount, sizeof(float) * 3);
        computeShader.SetBuffer(boidAIKernel, "_BoidsOutputs", boidsOuputsBuffer);
        ChunksManager.Instance.OnBorderUpdated += ChunksManager_OnBorderUpdated;
        firstUpdateAfterBoidsChange = true;
    }

    private void OnDestroy()
    {
        ChunksManager.Instance.OnBorderUpdated -= ChunksManager_OnBorderUpdated;
        boidsOuputsBuffer.Release();
        boidsInputsBuffer.Release();
    }

    private void Update()
    {
        if (boids.Count == 0)
        {
            return;
        }
        if (firstUpdateAfterBoidsChange)
        {
            for (int i = 0; i < boids.Count; i++)
            {
                boidsInputs[i].position = boids[i].transform.position;
                boidsInputs[i].targetForward = boids[i].targetForward;
            }
            boidsInputsBuffer.SetData(boidsInputs);
            computeShader.SetInt(boidsCountId, boids.Count);
            computeShader.Dispatch(boidAIKernel, Mathf.CeilToInt((float)boids.Count / 64f), 1, 1);
            firstUpdateAfterBoidsChange = false;
        }
        boidsOuputsBuffer.GetData(boidsOutputs);
        for (int i = 0; i < boids.Count; i++)
        {
            Boid boid = boids[i];
            boid.UpdateBoid(boidsOutputs[i].targetForward);
            boidsInputs[i].position = boid.transform.position;
            boidsInputs[i].targetForward = boid.targetForward;
        }
        boidsInputsBuffer.SetData(boidsInputs);
        computeShader.Dispatch(boidAIKernel, Mathf.CeilToInt((float)boids.Count / 64f), 1, 1);
    }

    public void AddBoid(Vector3 position)
    {
        if (boids.Count >= maxBoidsCount)
        {
            return;
        }
        boids.Add(Instantiate(boidPrefab, position, Random.rotation, boidsParent));
        firstUpdateAfterBoidsChange = true;
    }

    private void ChunksManager_OnBorderUpdated(Vector3 frontBottomLeft, Vector3 backTopRight)
    {
        boids.RemoveAll((Boid boid) =>
        {
            Vector3 position = boid.transform.position;
            if (position.x <= frontBottomLeft.x || position.y <= frontBottomLeft.y || position.z <= frontBottomLeft.z
            || position.x >= backTopRight.x || position.y >= backTopRight.y || position.z >= backTopRight.z)
            {
                Destroy(boid.gameObject);
                return true;
            }
            return false;
        });
        firstUpdateAfterBoidsChange = true;
    }
}
