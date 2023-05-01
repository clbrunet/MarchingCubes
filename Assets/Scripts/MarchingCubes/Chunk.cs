using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private ChunksManager manager;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private float[,,] noiseValues;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        manager = ChunksManager.Instance;
        Assert.IsNotNull(manager);
        noiseValues = new float[manager.axisSegmentCount + 1, manager.axisSegmentCount + 1,
            manager.axisSegmentCount + 1];
    }

    public void Regenerate(Vector3Int coordinate, ComputeBuffer noiseValuesBuffer, ChunkTriangle[] triangles,
        int trianglesCount)
    {
        transform.position = (Vector3)coordinate * manager.axisSize;
        noiseValuesBuffer.GetData(noiseValues);
        RegenerateMesh(triangles, trianglesCount);
        GenerateBoids(coordinate);
    }

    public void GenerateBoids(Vector3 coordinate)
    {
        for (int z = 0; z < noiseValues.GetLength(0); z++)
        {
            for (int y = 0; y < noiseValues.GetLength(1); y++)
            {
                for (int x = 0; x < noiseValues.GetLength(2); x++)
                {
                    if (noiseValues[z, y, x] <= BoidsManager.Instance.boidThreshold)
                    {
                        Vector3 position = coordinate + new Vector3(x, y, z) * (manager.axisSize / manager.axisSegmentCount);
                        for (int i = 0; i < BoidsManager.Instance.maxBoidsPerChunk; i++)
                        {
                            BoidsManager.Instance.AddBoid(position);
                        }
                        return;
                    }
                }
            }
        }
    }

    public void RegenerateMesh(ChunkTriangle[] triangles, int trianglesCount)
    {
        if (meshFilter.mesh != null)
        {
            meshFilter.sharedMesh.Clear();
        }
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
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public bool Edit(Vector3 localCenter, float change, ComputeBuffer noiseValuesBuffer)
    {
        Vector3Int frontBottomLeft = Vector3Int.FloorToInt((localCenter - manager.editRadius * Vector3.one)
            / (manager.axisSize / manager.axisSegmentCount));
        Vector3Int backTopRight = Vector3Int.CeilToInt((localCenter + manager.editRadius * Vector3.one)
            / (manager.axisSize / manager.axisSegmentCount));
        if (frontBottomLeft.x > manager.axisSegmentCount || frontBottomLeft.y > manager.axisSegmentCount
            || frontBottomLeft.z > manager.axisSegmentCount || backTopRight.x < 0 || backTopRight.y < 0
            || backTopRight.z < 0)
        {
            return false;
        }
        frontBottomLeft.Clamp(Vector3Int.zero, new Vector3Int((int)manager.axisSegmentCount,
            (int)manager.axisSegmentCount, (int)manager.axisSegmentCount));
        backTopRight.Clamp(Vector3Int.zero, new Vector3Int((int)manager.axisSegmentCount,
            (int)manager.axisSegmentCount, (int)manager.axisSegmentCount));
        for (int z = frontBottomLeft.z; z <= backTopRight.z; z++)
        {
            for (int y = frontBottomLeft.y; y <= backTopRight.y; y++)
            {
                for (int x = frontBottomLeft.x; x <= backTopRight.x; x++)
                {
                    float distance = Vector3.Distance(localCenter,
                        new Vector3(x, y, z) * (manager.axisSize / manager.axisSegmentCount));
                    if (distance <= manager.editRadius)
                    {
                        noiseValues[z, y, x] = Mathf.Clamp01(noiseValues[z, y, x] + change);
                    }
                }
            }
        }
        noiseValuesBuffer.SetData(noiseValues);
        return true;
    }
}
