using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainManager : MonoBehaviour
{
    public static RainManager instance { get; set; } //Singleton

    [Header("Droplets")]
    [SerializeField] bool rainDropsSpawning;
    [SerializeField] GameObject DropletParent;
    [SerializeField] GameObject DropletPrefab;

    [SerializeField] GameObject waterObject;
    Vector3 startWaterPosition = new Vector3(800, -240, 600);
    public float waterInDroplet = 0.01f;
    public float waterLevel = 0;

    [SerializeField] List<GameObject> Droplets_Pool;

    Vector2 minSize = Vector2.zero;
    Vector2 maxSize = Vector2.zero;

    [Header("Stats")]
    public float dropletLifetime = 5;
    public float dropletSpeed = 1;
    [SerializeField] float maxHeightAboveMesh = 150;
    public float maxHeightUnderMesh = -100;
    [SerializeField] float spawnTime = 1;
    float timeToSpawn = 0;


    //--------------------


    private void Awake()
    {
        //Singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        //Set SpawnTime
        timeToSpawn = spawnTime;
    }
    private void Start()
    {
        //Get Size of Mesh
        minSize.x = PointCloudVisualize.instance.meshToSpawn.vertices[0].x;
        minSize.y = PointCloudVisualize.instance.meshToSpawn.vertices[0].y;
        maxSize.x = PointCloudVisualize.instance.meshToSpawn.vertices[PointCloudVisualize.instance.meshToSpawn.vertices.Length - 1].x;
        maxSize.y = PointCloudVisualize.instance.meshToSpawn.vertices[PointCloudVisualize.instance.meshToSpawn.vertices.Length - 1].y;
    }
    private void Update()
    {
        if (rainDropsSpawning)
        {
            timeToSpawn -= Time.deltaTime;

            if (timeToSpawn <= 0)
            {
                if (PointCloudVisualize.instance.meshToSpawn)
                {
                    SpawnDroplet();
                }
            }
        }

        IncreaseWaterLevel();
    }


    //--------------------


    void SpawnDroplet()
    {
        timeToSpawn = spawnTime;

        // Spawn the prefab at the calculated position
        int amount = 0;
        for (int i = 0; i < Droplets_Pool.Count; i++)
        {
            if (!Droplets_Pool[i].activeInHierarchy)
            {
                amount++;

                Droplets_Pool[i].transform.position = GetRandomSpawnPosition();
                Droplets_Pool[i].GetComponent<Ball>().ResetDropletLifetime();
                
                break;
            }
        }

        if (amount <= 0)
        {
            Droplets_Pool.Add(Instantiate(DropletPrefab, GetRandomSpawnPosition(), Quaternion.identity) as GameObject);
            Droplets_Pool[Droplets_Pool.Count - 1].transform.parent = DropletParent.transform;

            Droplets_Pool[Droplets_Pool.Count - 1].GetComponent<Ball>().ResetDropletLifetime();
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        MeshFilter meshFilter = PointCloudVisualize.instance.GetComponent<MeshFilter>();
        Bounds meshBounds = meshFilter.mesh.bounds;

        // Calculate the random position above the Mesh
        Vector3 randomPosition = new Vector3
        (
            Random.Range(meshBounds.min.x, meshBounds.max.x),

            transform.position.y + maxHeightAboveMesh, //Adjust the height

            Random.Range(meshBounds.min.z, meshBounds.max.z)
        );

        return randomPosition;
    }

    void IncreaseWaterLevel()
    {
        waterObject.transform.position = new Vector3(startWaterPosition.x, startWaterPosition.y + waterLevel, startWaterPosition.z);
    }
}
