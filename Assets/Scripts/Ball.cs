using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class Ball : MonoBehaviour
{
    [Header("File")]
    //[SerializeField] TriangleSurface triangleSurface;
    [SerializeField] PointCloudVisualize pointCloudVisualize;

    Vector3 gravity = Physics.gravity;
    float mass = 1f;
    float radius = 5f;
    Vector3 velocity = Vector3.zero;
    Vector3 acceleration = new Vector3();

    [Header("Stats")]
    [SerializeField][Range(0, 1)] float bounciness = 0;

    Vector3 lastPosition = Vector3.zero;

    int mapPos = 0;

    [SerializeField] bool cooldown;
    [SerializeField] float cooldownTime;
    float dropletStuck = 0.2f;
    float dropletStuckTime = 0.2f;


    //--------------------


    private void Awake()
    {
        pointCloudVisualize = FindObjectOfType<PointCloudVisualize>();
    }
    private void Start()
    {
        ResetDropletLifetime();

        //startPosition = transform.position;
        //cooldowntTime = timeAlive;
    }
    private void Update()
    {
        //Check if gameObject is stuck
        DropletIsStuck();

        //Perform moving of the gameObject
        Move();

        //Check if gameObject is under the Mesh
        DropletIsUnderTheMesh();

        if (cooldown)
        {
            cooldownTime -= Time.deltaTime;

            if (cooldownTime <= 0)
            {
                RemoveDroplet();

                //print("Time Up");
            }
        }
    }

    void Move()
    {
        Vector3 position = transform.position;
        Vector2 position2D = new Vector2(position.x, position.z);

        mapPos = PointCloudVisualize.instance.FindMapPos(position2D.x, position2D.y);

        Vector3 Force_Vector = new Vector3(0, -9.81f * mass, 0);
        Vector3 Normal_Vector = new Vector3();
        Vector3 Gravity_Vector = mass * gravity;

        if (mapPos <= 0 || mapPos >= PointCloudVisualize.instance.meshToSpawn.vertices.Length)
        {
            if (cooldown)
            {
                //print("1. Cooldown");
            }
            else
            {
                //print(PointCloudVisualize.instance.meshToSpawn.normals.Count() + "," + (mapPos));

                RemoveDroplet();

                //print("mapPos <= -1");
            }
        }
        else
        {
            if (PointCloudVisualize.instance.meshToSpawn.normals[mapPos] == null)
            {
                //print("PointCloudVisualize.instance.meshToSpawn.normals[mapPos] == null");

                return;
            }

            Vector3 normal = PointCloudVisualize.instance.meshToSpawn.normals[mapPos];

            var hit = PointCloudVisualize.instance.CheckCollission(transform.position, mapPos, radius);

            if (hit != Vector3.one * -1f)
            {
                cooldown = true;

                Normal_Vector = -Vector3.Dot(hit, Force_Vector) * hit;
                Vector3 normalvelocity = Vector3.Dot(velocity, hit) * hit;
                velocity = velocity - normalvelocity;

                RainManager.instance.waterLevel += RainManager.instance.waterInDroplet;
            }
        }

        acceleration = (Gravity_Vector + Normal_Vector) / mass;

        velocity += acceleration * Time.fixedDeltaTime * RainManager.instance.dropletSpeed;
        transform.position += velocity * Time.fixedDeltaTime;
    }

    void DropletIsUnderTheMesh()
    {
        if (transform.position.y <= RainManager.instance.maxHeightUnderMesh)
        {
            RemoveDroplet();

            //print("UnderTheMesh");
        }
    }
    void DropletIsStuck()
    {
        if (transform.position == lastPosition)
        {
            //print("3. DropletIsStuck");

            RemoveDroplet();
        }

        lastPosition = transform.position;
    }

    void RemoveDroplet()
    {
        cooldown = false;
        gameObject.SetActive(false);
    }
    public void ResetDropletLifetime()
    {
        cooldown = false;
        cooldownTime = RainManager.instance.dropletLifetime;

        dropletStuck = dropletStuckTime;

        gameObject.SetActive(true);
    }


    //--------------------


    [ExecuteInEditMode]
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(lastPosition, radius);
    }
}
