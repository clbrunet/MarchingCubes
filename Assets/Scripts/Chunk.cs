using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private ChunkManager manager;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private float[,,] noiseValues;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        manager = ChunkManager.Instance;
        Assert.IsNotNull(manager);
        noiseValues = new float[manager.axisSegmentCount + 1, manager.axisSegmentCount + 1,
            manager.axisSegmentCount + 1];
    }

    public void Regenerate(Vector3Int coordinate, ComputeBuffer noiseValuesBuffer, ChunkTriangle[] triangles,
        int trianglesCount)
    {
        if (meshFilter.mesh != null)
        {
            meshFilter.sharedMesh.Clear();
        }
        transform.position = (Vector3)coordinate * manager.axisSize;
        noiseValuesBuffer.GetData(noiseValues);
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
}
