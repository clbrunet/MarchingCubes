using UnityEngine;
using UnityEngine.Assertions;

public class BoidsManager : MonoBehaviour
{
    public static BoidsManager Instance { get; private set; }

    [SerializeField]
    private Boid boidPrefab;

    [HideInInspector]
    public readonly Boid[] boids = new Boid[100];

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Start()
    {
        for(int i = 0; i < boids.Length; i++)
        {
            boids[i] = Instantiate(boidPrefab,
                new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), Random.rotation);
        }
    }
}
