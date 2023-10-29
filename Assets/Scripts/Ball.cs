using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Ball : MonoBehaviour
{
    //[SerializeField] TriangleSurface triangleSurface;
    [SerializeField] PointCloudVisualize pointCloudVisualize;
    Vector3 g = Physics.gravity;
    float m = 1f;
    float r = 5f;
    Vector3 velocity = Vector3.zero;
    Vector3 oldNormal = Vector3.zero;
    [SerializeField][Range(0, 1)] float bounciness = 0;

    Vector3 lastPosition = Vector3.zero;
    Vector3 startPosition;

    int mapPos = 0;


    //--------------------


    private void Awake()
    {
        pointCloudVisualize = FindObjectOfType<PointCloudVisualize>();
    }
    private void Start()
    {
        startPosition = transform.position;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position = startPosition;
            velocity = Vector3.zero;
            oldNormal = Vector3.zero;
            lastPosition = Vector3.zero;
        }
    }
    private void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        Vector3 position = transform.position;
        Vector2 position2D = new Vector2(position.x, position.z);

        mapPos = PointCloudVisualize.instance.FindMapPos(position2D.x, position2D.y);

        Vector3 gravity_force = new Vector3(0, -9.81f * m, 0);

        Vector3 newVelocity = velocity;
        Vector3 N = new Vector3();
        Vector3 G = m * g;
        Vector3 normalVelocity;

        if (mapPos < 0)
        {
            print(PointCloudVisualize.instance.meshToSpawn.normals.Count() + "," + (mapPos));

            //Destroy Droplet
        }
        else
        {
            //print("1. Enter Else");

            Vector3 normal = PointCloudVisualize.instance.meshToSpawn.normals[mapPos];

            var hit = PointCloudVisualize.instance.CheckCollission(transform.position, velocity, mapPos, r);

            if (hit != Vector3.one * -1f)
            {
                N = -Vector3.Dot(hit, gravity_force) * hit;
                Vector3 Vnormal = Vector3.Dot(velocity, hit) * hit;
                velocity = velocity - Vnormal;
            }

            #region 
            //var d = hit.position - transform.position;

            //print("d: " + d);
            //print("hit.position: " + hit.position);
            //print("transform.position: " + transform.position);
            //print("d.sqrMagnitude: " + d.sqrMagnitude + " | (r * r): " + (r * r));

            //if (hit.isHit && d.sqrMagnitude <= (r * r))
            //{
            //    normalVelocity = Vector3.Dot(velocity, hit.normal) * hit.normal;

            //    //Reflection
            //    velocity = velocity - normalVelocity - bounciness * normalVelocity;

            //    lastPosition = hit.position;

            //    N = -Vector3.Dot(hit.normal, G) * hit.normal;

            //    transform.position = hit.position + (r * hit.normal);

            //    //print("Hit");
            //}
            //else
            //{
            //    lastPosition = transform.position;
            //}
            #endregion
        }

        Vector3 acceleration = new Vector3();
        acceleration = (G + N) / m;

        velocity += acceleration * Time.fixedDeltaTime;
        transform.position += velocity * Time.fixedDeltaTime;

    }

    [ExecuteInEditMode]
    private void OnDrawGizmos()
    {
        lastPosition = transform.position;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(lastPosition, r);
    }
}
