using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private struct Triangle
    {
        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;
    };

    private const int CASE_MAX_TRIANGLES_COUNT = 15;

    private MeshFilter meshFilter;
    private ChunkManager manager;
    private float[,,] noiseValues;
    private readonly int[] trianglesCounts = new int[1];
    private int trianglesCount;
    private Triangle[] triangles;

    [SerializeField]
    private ComputeShader computeShader;
    private static int generateNoiseValuesKernel;
    private static int generateMeshDataKernel;
    private readonly static int axisSegmentCountId = Shader.PropertyToID("_AxisSegmentCount");
    private readonly static int noiseScaleId = Shader.PropertyToID("_NoiseScale");
    private readonly static int coordinateId = Shader.PropertyToID("_Coordinate");
    private ComputeBuffer noiseValuesBuffer;
    private readonly static int noiseValuesId = Shader.PropertyToID("_NoiseValues");
    private readonly static int axisSizeId = Shader.PropertyToID("_AxisSize");
    private readonly static int isosurfaceThresholdId = Shader.PropertyToID("_IsosurfaceThreshold");
    private ComputeBuffer trianglesBuffer;
    private readonly static int trianglesId = Shader.PropertyToID("_Triangles");
    private ComputeBuffer trianglesCountBuffer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        manager = ChunkManager.Instance;
        Assert.IsNotNull(manager);
        noiseValues = new float[manager.axisSegmentCount + 1, manager.axisSegmentCount + 1,
            manager.axisSegmentCount + 1];
        triangles = new Triangle[(int)Mathf.Pow(manager.axisSegmentCount, 3) * CASE_MAX_TRIANGLES_COUNT];

        generateNoiseValuesKernel = computeShader.FindKernel("GenerateNoiseValues");
        generateMeshDataKernel = computeShader.FindKernel("GenerateMeshData");
        computeShader.SetInt(axisSegmentCountId, (int)manager.axisSegmentCount);
        computeShader.SetFloat(noiseScaleId, manager.noiseScale);
        noiseValuesBuffer = new ComputeBuffer((int)Mathf.Pow(manager.axisSegmentCount + 1, 3), sizeof(float));
        computeShader.SetFloat(axisSizeId, manager.axisSize);
        computeShader.SetFloat(isosurfaceThresholdId, manager.isosurfaceThreshold);
        trianglesBuffer = new ComputeBuffer((int)Mathf.Pow(manager.axisSegmentCount, 3) * CASE_MAX_TRIANGLES_COUNT,
            3 * 3 * sizeof(float), ComputeBufferType.Append);
        trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    private void OnDestroy()
    {
        noiseValuesBuffer.Release();
        trianglesBuffer.Release();
        trianglesCountBuffer.Release();
    }

    public void Regenerate(Vector3Int coordinate)
    {
        if (meshFilter.mesh != null)
        {
            meshFilter.mesh.Clear();
        }
        transform.position = (Vector3)coordinate * manager.axisSize;
        computeShader.SetVector(coordinateId, (Vector3)coordinate);
        RegenerateNoiseValues(() =>
        {
            RegenerateMeshData(() =>
            {
                Vector3[] vertices = new Vector3[trianglesCount * 3];
                int[] trianglesIndices = new int[trianglesCount * 3];
                int i = 0;
                for (int j = 0; j < trianglesCount; j++)
                {
                    vertices[i + 0] = triangles[j].vertexA;
                    vertices[i + 1] = triangles[j].vertexB;
                    vertices[i + 2] = triangles[j].vertexC;
                    trianglesIndices[i + 0] = i + 0;
                    trianglesIndices[i + 1] = i + 1;
                    trianglesIndices[i + 2] = i + 2;
                    i += 3;
                }
                Mesh mesh = new()
                {
                    vertices = vertices,
                    triangles = trianglesIndices,
                };
                mesh.RecalculateNormals();
                meshFilter.mesh = mesh;
            });
        });
    }

    private void RegenerateNoiseValues(Action callback)
    {
        computeShader.SetBuffer(generateNoiseValuesKernel, noiseValuesId, noiseValuesBuffer);
        int threadGroups = Mathf.CeilToInt((float)(manager.axisSegmentCount + 1) / 4f);
        computeShader.Dispatch(generateNoiseValuesKernel, threadGroups, threadGroups, threadGroups);
        AsyncGPUReadback.Request(noiseValuesBuffer, (req) =>
        {
            noiseValuesBuffer.GetData(noiseValues);
            callback?.Invoke();
        });
    }

    private void RegenerateMeshData(Action callback)
    {
        computeShader.SetBuffer(generateMeshDataKernel, noiseValuesId, noiseValuesBuffer);
        trianglesBuffer.SetCounterValue(0);
        computeShader.SetBuffer(generateMeshDataKernel, trianglesId, trianglesBuffer);
        int threadGroups = Mathf.CeilToInt((float)manager.axisSegmentCount / 4f);
        computeShader.Dispatch(generateMeshDataKernel, threadGroups, threadGroups, threadGroups);
        AsyncGPUReadback.Request(trianglesBuffer, (req) =>
        {
            ComputeBuffer.CopyCount(trianglesBuffer, trianglesCountBuffer, 0);
            trianglesCountBuffer.GetData(trianglesCounts);
            trianglesCount = trianglesCounts[0];
            trianglesBuffer.GetData(triangles, 0, 0, trianglesCount);
            callback?.Invoke();
        });
    }
}
