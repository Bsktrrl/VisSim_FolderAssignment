using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    #region Variables
    //Physics
    Vector3 gravity = Physics.gravity;
    Vector3 velocity = Vector3.zero;
    Vector3 acceleration = new Vector3();
    float mass = 1f;
    float radius = 5f;

    //Position
    Vector3 lastPosition = Vector3.zero;
    int mapPoint = 0;

    //Stats
    [Header("Stats")]
    [SerializeField] bool cooldown;
    [SerializeField] float cooldownTime;

    [HideInInspector] float dropletStuck = 0.2f;
    [HideInInspector] float dropletStuckTime = 0.2f;
    #endregion


    //--------------------


    private void Start()
    {
        ResetDropletLifetime();
    }
    private void Update()
    {
        //Check if gameObject is stuck, to despawn it
        DropletIsStuck();

        //Perform the movement of the gameObject
        Movement();

        //Check if gameObject is under the Mesh, to despawn it
        DropletIsUnderTheMesh();

        //Check if gameObject have been on the mesh for its lifetime duration, to despawn it
        if (cooldown)
        {
            cooldownTime -= Time.deltaTime;

            if (cooldownTime <= 0)
            {
                RemoveDroplet();
            }
        }
    }


    //--------------------


    void Movement()
    {
        //Get gameObject's 2D position
        Vector3 position = transform.position;
        Vector2 position2D = new Vector2(position.x, position.z);

        //Find which point on the regular mesh the gameObject is on
        mapPoint = PointCloudVisualize.instance.FindDropletPosition(position2D.x, position2D.y);

        //Set physics vectors
        Vector3 force_Vector = new Vector3(0, -9.81f * mass, 0);
        Vector3 normal_Vector = new Vector3();
        Vector3 gravity_Vector = mass * gravity;

        //Check if mapPoint is outside "meshToSpawn.vertices"-bounds
        if (mapPoint <= 0 || mapPoint >= PointCloudVisualize.instance.tempMesh.vertices.Length)
        {
            //If outside, keep the droplet alive as long as it has collided with the mesh
            if (!cooldown)
            {
                RemoveDroplet();
            }
        }

        //If mapPoint is iside "meshToSpawn.vertices"-bounds
        else
        {
            //Check if mapPoint contain a legal index
            if (PointCloudVisualize.instance.tempMesh.normals[mapPoint] == null)
            {
                return;
            }

            //Get the point where collission takes place 
            Vector3 collission = PointCloudVisualize.instance.GetCollissionPoint(transform.position, mapPoint, radius);

            //Check if gameObject collides with the mesh
            if (collission != Vector3.one * -1f)
            {
                //Start gameObject's cooldown process, before repawning it
                cooldown = true;

                //Use physics to setup the change of the gameObject's movement direction
                normal_Vector = -Vector3.Dot(collission, force_Vector) * collission;
                Vector3 normal_Velocity = Vector3.Dot(velocity, collission) * collission;
                velocity = velocity - normal_Velocity;

                //Increase water level with the gameObject's water amount
                RainManager.instance.waterLevel += RainManager.instance.waterInDroplet;
            }
        }

        //Change gameObject position based on acceleration and velocity
        if (cooldown)
        {
            acceleration = (gravity_Vector + normal_Vector) / mass;
            velocity += acceleration * Time.fixedDeltaTime / 100;
            transform.position += velocity * Time.fixedDeltaTime;
        }
        else
        {
            acceleration = (gravity_Vector + normal_Vector) / mass;
            velocity += acceleration * Time.fixedDeltaTime * RainManager.instance.dropletSpeed;
            transform.position += velocity * Time.fixedDeltaTime;
        }
    }


    //--------------------


    void DropletIsUnderTheMesh()
    {
        //Check if gameObject is under the mesh, to despawn it
        if (transform.position.y <= RainManager.instance.despawningHeightUnderMesh)
        {
            RemoveDroplet();
        }
    }
    void DropletIsStuck()
    {
        //Check if gameObject is stuck, to despawn it
        if (transform.position == lastPosition)
        {
            RemoveDroplet();
        }

        lastPosition = transform.position;
    }

    void RemoveDroplet()
    {
        //Despawn the mesh, placing the gameObject back into the pool
        cooldown = false;
        gameObject.SetActive(false);
    }
    public void ResetDropletLifetime()
    {
        //Respawn the gameObject from the pool
        cooldown = false;
        cooldownTime = RainManager.instance.dropletLifetime;
        dropletStuck = dropletStuckTime;

        gameObject.SetActive(true);
    }


    //--------------------


    private void OnDrawGizmos()
    {
        //Draw gizmo around gameObject to keep track
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
