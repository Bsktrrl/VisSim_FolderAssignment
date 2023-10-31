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
    //Vector3 startPosition;

    int mapPos = 0;

    [SerializeField] bool cooldown;
    [SerializeField] float cooldownTime;


    //--------------------


    private void Awake()
    {
        pointCloudVisualize = FindObjectOfType<PointCloudVisualize>();
    }
    private void Start()
    {
        //startPosition = transform.position;
        //cooldowntTime = timeAlive;
    }
    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    transform.position = startPosition;
        //    velocity = Vector3.zero;
        //    oldNormal = Vector3.zero;
        //    lastPosition = Vector3.zero;
        //}

        if (cooldown)
        {
            cooldownTime -= Time.deltaTime;

            if (cooldownTime <= 0)
            {
                RemoveDroplet();

                print("Time Up");
            }
        }
    }
    private void FixedUpdate()
    {
        //Perform moving of the gameObject
        Move();

        //Check if gameObject is under the Mesh
        UnderTheMesh();
    }

    void Move()
    {
        Vector3 position = transform.position;
        Vector2 position2D = new Vector2(position.x, position.z);

        mapPos = PointCloudVisualize.instance.FindMapPos(position2D.x, position2D.y);

        Vector3 gravity_force = new Vector3(0, -9.81f * mass, 0);

        Vector3 newVelocity = velocity;
        Vector3 N = new Vector3();
        Vector3 G = mass * gravity;
        Vector3 normalVelocity;

        if (mapPos <= 0 || mapPos >= PointCloudVisualize.instance.meshToSpawn.vertices.Length)
        {
            if (cooldown)
            {
                print("1. Cooldown");
            }
            else
            {
                //print(PointCloudVisualize.instance.meshToSpawn.normals.Count() + "," + (mapPos));

                RemoveDroplet();

                print("mapPos <= -1");
            }
        }
        else
        {
            if (PointCloudVisualize.instance.meshToSpawn.normals[mapPos] == null)
            {
                print("PointCloudVisualize.instance.meshToSpawn.normals[mapPos] == null");

                return;
            }

            //Vector3 normal = PointCloudVisualize.instance.meshToSpawn.normals[mapPos];

            var hit = PointCloudVisualize.instance.CheckCollission(transform.position, mapPos, radius);

            if (hit != Vector3.one * -1f)
            {
                cooldown = true;

                N = -Vector3.Dot(hit, gravity_force) * hit;
                Vector3 Vnormal = Vector3.Dot(velocity, hit) * hit;
                velocity = velocity - Vnormal;
            }
        }

        acceleration = (G + N) / mass;

        velocity += acceleration * Time.fixedDeltaTime * RainManager.instance.dropletSpeed;
        transform.position += velocity * Time.fixedDeltaTime;
    }

    void UnderTheMesh()
    {
        if (transform.position.y <= RainManager.instance.maxHeightUnderMesh)
        {
            RemoveDroplet();

            print("UnderTheMesh");
        }
    }

    void RemoveDroplet()
    {
        cooldown = false;
        gameObject.SetActive(false);
    }
    public void ResetLifetime()
    {
        cooldown = false;
        cooldownTime = RainManager.instance.dropletLifetime;

        gameObject.SetActive(true);
    }

    [ExecuteInEditMode]
    private void OnDrawGizmos()
    {
        lastPosition = transform.position;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(lastPosition, radius);
    }
}
