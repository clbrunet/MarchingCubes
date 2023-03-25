using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Data;

public class CasesViewer : MonoBehaviour
{
    private enum VertexState
    {
        Uninitialized = 0,
        Empty = 1,
        Full = 2,
    }

    private static readonly Vector3Int[] CORNERS = new Vector3Int[8]
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 0, 0),
        new Vector3Int(0, 1, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
    };

    private void Start()
    {
        GenerateCases();
    }

    private void GenerateCases()
    {
        uint caseIndex = 0;
        VertexState[,,] verticesState = new VertexState[2,2,2]
        {
            {
                {
                    VertexState.Empty,
                    VertexState.Empty,
                },
                {
                    VertexState.Empty,
                    VertexState.Empty,
                },
            },
            {
                {
                    VertexState.Empty,
                    VertexState.Empty,
                },
                {
                    VertexState.Empty,
                    VertexState.Empty,
                },
            },
        };
        int cornerIndex = 8;
        while (true)
        {
            if (cornerIndex < 0)
            {
                break;
            }
            if (cornerIndex == 8)
            {
                GenerateCase(verticesState, caseIndex);
                caseIndex++;
                cornerIndex--;
                continue;
            }
            Vector3Int corner = CORNERS[cornerIndex];
            switch (verticesState[corner.z, corner.y, corner.x])
            {
                case VertexState.Uninitialized:
                    verticesState[corner.z, corner.y, corner.x] = VertexState.Empty;
                    cornerIndex++;
                    break;
                case VertexState.Empty:
                    verticesState[corner.z, corner.y, corner.x] = VertexState.Full;
                    cornerIndex++;
                    break;
                case VertexState.Full:
                    verticesState[corner.z, corner.y, corner.x] = VertexState.Uninitialized;
                    cornerIndex--;
                    break;
                default:
                    break;
            }
        }
    }

    private void GenerateCase(VertexState[,,] verticesState, uint caseIndex)
    {
        Transform parent = new GameObject(caseIndex.ToString()).transform;
        parent.transform.Translate(new Vector3(caseIndex * 3f, 0f, 0f));
        parent.parent = transform;
        GenerateCaseCorners(verticesState, parent);
        GenerateCaseMeshVertices(verticesState, parent);
        GenerateCaseMesh(verticesState, parent);
    }

    private void GenerateCaseCorners(VertexState[,,] verticesState, Transform parent)
    {
        foreach (Vector3Int corner in CORNERS)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = parent;
            sphere.transform.localPosition = corner;
            sphere.transform.localScale = 0.3f * Vector3.one;
            if (verticesState[corner.z, corner.y, corner.x] == VertexState.Full)
            {
                sphere.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
            }
            else
            {
                sphere.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0f);
            }
        }
    }

    private void GenerateCaseMeshVertices(VertexState[,,] verticesState, Transform parent)
    {
        Vector3Int[] edgeTestsStartVertices = {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 0, 1),
        };
        foreach (Vector3Int edgeTestsStartVertex in edgeTestsStartVertices)
        {
            int x = edgeTestsStartVertex.x;
            int y = edgeTestsStartVertex.y;
            int z = edgeTestsStartVertex.z;
            VertexState start = verticesState[z, y, x];
            if ((edgeTestsStartVertex.x > 0 && start != verticesState[z, y, x - 1])
                || (edgeTestsStartVertex.x < 1 && start != verticesState[z, y, x + 1]))
            {
                GenerateCaseMeshVertex(new Vector3(0.5f, y, z), parent);
            }
            if ((edgeTestsStartVertex.z > 0 && start != verticesState[z - 1, y, x])
                || (edgeTestsStartVertex.z < 1 && start != verticesState[z + 1, y, x]))
            {
                GenerateCaseMeshVertex(new Vector3(x, y, 0.5f), parent);
            }
            if ((edgeTestsStartVertex.y > 0 && start != verticesState[z, y - 1, x])
                || (edgeTestsStartVertex.y < 1 && start != verticesState[z, y + 1, x]))
            {
                GenerateCaseMeshVertex(new Vector3(x, 0.5f, z), parent);
            }
        }
    }

    private void GenerateCaseMeshVertex(Vector3 localPosition, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = parent;
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = 0.2f * Vector3.one;
        sphere.GetComponent<MeshRenderer>().material.color = new Color(1f, 0f, 0f);
    }

    private void GenerateCaseMesh(VertexState[,,] verticesState, Transform parent)
    {
        int lookupCaseIndex = 0;
        int i = 0b0000_0001;
        foreach (Vector3Int corner in CORNERS)
        {
            if (verticesState[corner.z, corner.y, corner.x] == VertexState.Full)
            {
                lookupCaseIndex |= i;
            }
            i <<= 1;
        }
        List<Vector3> vertices = new();
        List<int> triangles = new();
        int[] trianglesEdges = LookupTables.TRIANGULATION[lookupCaseIndex];
        for (i = 0; trianglesEdges[i] >= 0; i += 3)
        {
            for (int j = i; j < i + 3; j++)
            {
                int triangleEdge = trianglesEdges[j];
                Vector3Int cornerA = CORNERS[LookupTables.EDGE_TO_CORNER_A[triangleEdge]];
                Vector3Int cornerB = CORNERS[LookupTables.EDGE_TO_CORNER_B[triangleEdge]];
                Vector3 vertex = ((Vector3)(cornerA + cornerB)) / 2f;
                vertices.Add(vertex);
            }
            triangles.Add(i + 2);
            triangles.Add(i + 1);
            triangles.Add(i + 0);
        }
        GameObject meshGameObject = new("Mesh");
        meshGameObject.transform.parent = parent;
        meshGameObject.transform.localPosition = Vector3.zero;
        Mesh mesh  = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        meshGameObject.AddComponent<MeshFilter>().mesh = mesh;
        meshGameObject.AddComponent<MeshRenderer>().material.color = new(0f, 1f, 0f);
    }
}
