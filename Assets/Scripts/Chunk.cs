using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Threading.Tasks;
using UnityEngine.Assertions;

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
        noiseValues = new float[manager.axisSegmentCount + 1, manager.axisSegmentCount + 1,
            manager.axisSegmentCount + 1];
        meshData = new MeshData(new List<Vector3>(), new List<int>());
    }

    public async void RegenerateAsync(Vector3Int coordinate)
    {
        if (meshFilter.mesh != null)
        {
            meshFilter.mesh.Clear();
        }
        this.coordinate = coordinate;
        transform.position = (Vector3)coordinate * manager.axisSize;
        await Task.Run(() =>
        {
            RegenerateNoiseValues();
            RegenerateMeshData();
        });
        if (this == null)
        {
            return;
        }
        Mesh mesh = new()
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void RegenerateNoiseValues()
    {
        for (uint z = 0; z <= manager.axisSegmentCount; z++)
        {
            for (uint y = 0; y <= manager.axisSegmentCount; y++)
            {
                for (uint x = 0; x <= manager.axisSegmentCount; x++)
                {
                    Vector3 pointCoordinate = coordinate + new Vector3(x, y, z) / (float)manager.axisSegmentCount;
                    float3 noiseCoordinate = pointCoordinate * manager.noiseScale;
                    float value = math.unlerp(-1f, 1f, noise.snoise(noiseCoordinate));
                    noiseValues[z, y, x] = value;
                }
            }
        }
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
