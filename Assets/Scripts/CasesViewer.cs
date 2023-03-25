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

    private static readonly Vector3Int[] VERTICES = new Vector3Int[8]
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
        int vertexIndex = 8;
        while (true)
        {
            if (vertexIndex < 0)
            {
                break;
            }
            if (vertexIndex == 8)
            {
                GenerateCase(verticesState, caseIndex);
                caseIndex++;
                vertexIndex--;
                continue;
            }
            Vector3Int vertex = VERTICES[vertexIndex];
            switch (verticesState[vertex.z, vertex.y, vertex.x])
            {
                case VertexState.Uninitialized:
                    verticesState[vertex.z, vertex.y, vertex.x] = VertexState.Empty;
                    vertexIndex++;
                    break;
                case VertexState.Empty:
                    verticesState[vertex.z, vertex.y, vertex.x] = VertexState.Full;
                    vertexIndex++;
                    break;
                case VertexState.Full:
                    verticesState[vertex.z, vertex.y, vertex.x] = VertexState.Uninitialized;
                    vertexIndex--;
                    break;
                default:
                    break;
            }
        }
    }

    private void GenerateCase(VertexState[,,] verticesState, uint caseIndex)
    {
        Transform parent = new GameObject(caseIndex.ToString()).transform;
        parent.parent = transform;
        GenerateCaseCorners(verticesState, caseIndex, parent);
        GenerateCaseMeshVertices(verticesState, caseIndex, parent);
    }

    private void GenerateCaseCorners(VertexState[,,] verticesState, uint caseIndex, Transform parent)
    {
        foreach (Vector3Int vertex in VERTICES)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = parent;
            sphere.transform.localPosition = new(vertex.x + caseIndex * 3, vertex.y, vertex.z);
            sphere.transform.localScale = 0.3f * Vector3.one;
            if (verticesState[vertex.z, vertex.y, vertex.x] == VertexState.Full)
            {
                sphere.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
            }
            else
            {
                sphere.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0f);
            }
        }
    }

    private void GenerateCaseMeshVertices(VertexState[,,] verticesState, uint caseIndex, Transform parent)
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
                GenerateCaseMeshVertex(new Vector3(0.5f + caseIndex * 3, y, z), parent);
            }
            if ((edgeTestsStartVertex.z > 0 && start != verticesState[z - 1, y, x])
                || (edgeTestsStartVertex.z < 1 && start != verticesState[z + 1, y, x]))
            {
                GenerateCaseMeshVertex(new Vector3(x + caseIndex * 3, y, 0.5f), parent);
            }
            if ((edgeTestsStartVertex.y > 0 && start != verticesState[z, y - 1, x])
                || (edgeTestsStartVertex.y < 1 && start != verticesState[z, y + 1, x]))
            {
                GenerateCaseMeshVertex(new Vector3(x + caseIndex * 3, 0.5f, z), parent);
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
}
