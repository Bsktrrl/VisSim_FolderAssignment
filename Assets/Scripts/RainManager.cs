using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainManager : MonoBehaviour
{
    public static RainManager instance { get; set; } //Singleton


    //--------------------


    #region Variables
    [Header("Droplets")]
    [SerializeField] bool spawning = true;
    [SerializeField] bool spawningAtRandom = true;
    [SerializeField] Vector3 spawningPosition = new Vector3(700, 300, 200);
    [SerializeField] float spawningOffset = 50;

    [Space(10)]

    public float dropletLifetime = 5;
    public float dropletSpeed = 80;

    [Space(10)]

    [SerializeField] float spawningTime = 0.001f;
    float timeToSpawn = 0;
    [SerializeField] float spawningHeightAboveMesh = 200;
    public float despawningHeightUnderMesh = -50;

    [Space(10)]

    public float waterInDroplet = 0.01f;

    [Space(10)]

    [SerializeField] GameObject DropletParent;
    [SerializeField] GameObject DropletPrefab;

    [Space(10)]

    [SerializeField] List<GameObject> Droplets_Pool;

    [Header("Water")]
    [SerializeField] GameObject waterObject;
    Vector3 startWaterPosition = new Vector3(800, -241, 600);
    public float waterLevel = 0;

    //Other
    Vector2 minSize = Vector2.zero;
    Vector2 maxSize = Vector2.zero;
    #endregion


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

        //Set Droplet's SpawnTime 
        timeToSpawn = spawningTime;
    }
    private void Start()
    {
        //Get Size of Mesh
        if (PointCloudVisualize.instance.tempMesh.vertices.Length > 0)
        {
            minSize.x = PointCloudVisualize.instance.tempMesh.vertices[0].x;
            minSize.y = PointCloudVisualize.instance.tempMesh.vertices[0].y;
            maxSize.x = PointCloudVisualize.instance.tempMesh.vertices[PointCloudVisualize.instance.tempMesh.vertices.Length - 1].x;
            maxSize.y = PointCloudVisualize.instance.tempMesh.vertices[PointCloudVisualize.instance.tempMesh.vertices.Length - 1].y;
        }

        //Enable waterObject
        waterObject.SetActive(true);
    }
    private void Update()
    {
        //Check if Droplet where to spawn
        if (spawning)
        {
            //Countdown to next spawn
            timeToSpawn -= Time.deltaTime;

            //Spawn Droplet at a random position above the mesh, if mesh exists
            if (timeToSpawn <= 0)
            {
                if (PointCloudVisualize.instance.tempMesh)
                {
                    SpawnDroplet_ObjectPooling();
                }
            }
        }

        //Check if waterlevel increases
        IncreaseWaterLevel();
    }


    //--------------------


    void SpawnDroplet_ObjectPooling()
    {
        //Reset the time until next spawn
        timeToSpawn = spawningTime;

        // Spawn the prefab at a random position inside of the meshs' Bounds
        int amount = 0;
        for (int i = 0; i < Droplets_Pool.Count; i++)
        {
            //Search for respawning of the first available droplet in the Object Pool 
            if (!Droplets_Pool[i].activeInHierarchy)
            {
                amount++;

                if (spawningAtRandom)
                {
                    Droplets_Pool[i].transform.position = GetRandomSpawnPosition();
                }
                else
                {
                    Droplets_Pool[i].transform.position = spawningPosition;
                }

                Droplets_Pool[i].GetComponent<Ball>().ResetDropletLifetime();

                break;
            }
        }

        //If the Object Pool is too small, instantiate a Droplet and add it to the pool for further use after its respawn
        if (amount <= 0)
        {
            if (spawningAtRandom)
            {
                Droplets_Pool.Add(Instantiate(DropletPrefab, GetRandomSpawnPosition(), Quaternion.identity) as GameObject);
                Droplets_Pool[Droplets_Pool.Count - 1].transform.parent = DropletParent.transform;
            }
            else
            {
                Droplets_Pool.Add(Instantiate(DropletPrefab, spawningPosition, Quaternion.identity) as GameObject);
                Droplets_Pool[Droplets_Pool.Count - 1].transform.parent = DropletParent.transform;
            }

            Droplets_Pool[Droplets_Pool.Count - 1].GetComponent<Ball>().ResetDropletLifetime();
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        MeshFilter meshFilter = PointCloudVisualize.instance.GetComponent<MeshFilter>();
        Bounds meshBounds = meshFilter.mesh.bounds;

        //Get a random position above the mesh with an offset inwards from the bounds of the mesh
        Vector3 randomSpawnPosition = new Vector3
        (
            Random.Range(meshBounds.min.x + spawningOffset, meshBounds.max.x - spawningOffset),
            transform.position.y + spawningHeightAboveMesh,
            Random.Range(meshBounds.min.z + spawningOffset, meshBounds.max.z - spawningOffset)
        );

        return randomSpawnPosition;
    }

    void IncreaseWaterLevel()
    {
        //Change height level of the waterObject based on new waterLevel from the Droplets
        waterObject.transform.position = new Vector3(startWaterPosition.x, startWaterPosition.y + waterLevel, startWaterPosition.z);
    }
}
