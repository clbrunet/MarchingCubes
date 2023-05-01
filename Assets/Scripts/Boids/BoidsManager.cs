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

    [HideInInspector]
    public readonly List<Boid> boids = new();

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    public void AddBoid(Vector3 position)
    {
        boids.Add(Instantiate(boidPrefab, position, Random.rotation, boidsParent));
    }
}
