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

    private void Start()
    {
        ChunksManager.Instance.OnBorderUpdated += ChunksManager_OnBorderUpdated;
    }

    private void OnDestroy()
    {
        ChunksManager.Instance.OnBorderUpdated -= ChunksManager_OnBorderUpdated;
    }

    public void AddBoid(Vector3 position)
    {
        boids.Add(Instantiate(boidPrefab, position, Random.rotation, boidsParent));
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
    }
}
