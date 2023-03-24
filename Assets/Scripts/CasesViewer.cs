using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Data;

public class CasesViewer : MonoBehaviour
{
    enum VertexState
    {
        Uninitialized = 0,
        Empty = 1,
        Full = 2,
    }

    private void Start()
    {
        GenerateCases();
    }

    private void GenerateCases()
    {
        uint caseIndex = 0;
        VertexState[] verticesState = new VertexState[8];
        for (uint i = 0; i < 8; i++)
        {
            verticesState[i] = VertexState.Empty;
        }
        int vertexIndex = 8;
        while (true)
        {
            if (vertexIndex == -1)
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
            switch (verticesState[vertexIndex])
            {
                case VertexState.Uninitialized:
                    verticesState[vertexIndex] = VertexState.Empty;
                    vertexIndex++;
                    break;
                case VertexState.Empty:
                    verticesState[vertexIndex] = VertexState.Full;
                    vertexIndex++;
                    break;
                case VertexState.Full:
                    verticesState[vertexIndex] = VertexState.Uninitialized;
                    vertexIndex--;
                    break;
                default:
                    break;
            }
        }
    }

    private void GenerateCase(VertexState[] verticesState, uint caseIndex)
    {
        for (uint z = 0; z < 2; z++)
        {
            for (uint y = 0; y < 2; y++)
            {
                for (uint x = 0; x < 2; x++)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.parent = transform;
                    sphere.transform.localPosition = new(x + caseIndex * 3, y, z);
                    sphere.transform.localScale = 0.3f * Vector3.one;
                    if (verticesState[z * 4 + y * 2 + x] == VertexState.Full)
                    {
                        sphere.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                    }
                    else
                    {
                        sphere.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0f);
                    }
                }
            }
        }
    }
}
