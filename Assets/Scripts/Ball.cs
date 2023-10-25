using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] TriangleSurface triangleSurface;
    Vector3 g = Physics.gravity;
    float m = 1f;
    float r = 0.02f;
    Vector3 velocity = Vector3.zero;
    Vector3 oldNormal = Vector3.zero;
    [SerializeField][Range(0, 1)] float bounciness = 0;

    Vector3 lastPosition = Vector3.zero;
    Vector3 startPosition;


    //--------------------


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
        var hit = triangleSurface.GetCollision(position2D);

        Vector3 newVelocity = velocity;
        Vector3 N = new Vector3();
        Vector3 G = m * g;
        Vector3 normalVelocity;

        var d = hit.position - transform.position;

        if (hit.isHit && d.sqrMagnitude <= (r * r))
        {
            normalVelocity = Vector3.Dot(velocity, hit.normal) * hit.normal;

            //Reflection
            velocity = velocity - normalVelocity - bounciness * normalVelocity;

            lastPosition = hit.position;

            N = -Vector3.Dot(hit.normal, G) * hit.normal;

            transform.position = hit.position + (r * hit.normal);

            print("Hit");
        }
        else
        {
            lastPosition = Vector3.zero;
        }

        Vector3 acceleration = new Vector3();
        acceleration = (G + N) / m;

        velocity += acceleration * Time.fixedDeltaTime;
        transform.position += velocity * Time.fixedDeltaTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(lastPosition, r * r);
    }
}
