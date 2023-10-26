using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static DelaunayTriangulation;

[ExecuteInEditMode]
public class PointCloudVisualize : MonoBehaviour
{
    public static PointCloudVisualize instance { get; set; } //Singleton

    [Serializable]
    public class Vertex
    {
        public Vector3 position;
        public Vector3 normal;

        public Vertex(Vector3 _pos, Vector3 _normal = new())
        {
            position = _pos;
            normal = _normal;
        }
    }

    [Header("File")]
    [SerializeField] TextAsset pointCloudFile;
    
    [Header("Simulation")]
    [SerializeField] bool runSimulation;
    [SerializeField] bool showGizmo;

    [Header("Gismo")]
    [SerializeField] int pointsJumpedOver = 1000;
    [SerializeField] float pointSize = 1;
    [SerializeField] Color gismoColor;

    [Header("Vertices")]
    [SerializeField] int verticesSize = 0;
    List<Vertex> vertices = new List<Vertex>();
    bool posisionIsCorrected;


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
    }
    private void Update()
    {
        if (runSimulation)
        {
            ReadVertexData();
            CorrectPointPosition();

            Triangulate();
        }
        else
        {
            if (vertices.Count > 0)
            {
                vertices.Clear();
                verticesSize = 0;
            }
        }
    }


    //--------------------


    void ReadVertexData()
    {
        if (vertices.Count > 0)
        {
            return;
        }

        posisionIsCorrected = false;

        //Where to split lines
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //What seperates the numbers on the line
        var lineDelimiters = new[] { '(', ')', ' ' };

        //Split the lines into a string-array
        var lines = pointCloudFile.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(message: $"{pointCloudFile.name} was empty");
            return;
        }

        //Get first line, to see how many lines there will be
        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(message: $"{pointCloudFile.name} contains no vertex data");
            return;
        }

        //Split all lines based on the first line number
        for (int i = 1; i <= numVertices; i += pointsJumpedOver)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{pointCloudFile.name} is missing data on line {i}");

                continue;
            }

            Vertex vertex = new Vertex(new Vector3
                (
                    float.Parse(elements[0], CultureInfo.InvariantCulture),
                    float.Parse(elements[2], CultureInfo.InvariantCulture),
                    float.Parse(elements[1], CultureInfo.InvariantCulture))
                );

            vertices.Add(vertex);
            verticesSize = vertices.Count;
        }
    }
    void CorrectPointPosition()
    {
        if (posisionIsCorrected)
        {
            return;
        }

        Vector3 minValue = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3 maxValue = new Vector3(int.MinValue, int.MinValue, int.MinValue);

        //Find Min and Max value
        for (int i = 0; i < vertices.Count; i++)
        {
            //Find Min value
            if (vertices[i].position.x < minValue.x)
            {
                minValue.x = vertices[i].position.x;
            }
            if (vertices[i].position.y < minValue.y)
            {
                minValue.y = vertices[i].position.y;
            }
            if (vertices[i].position.z < minValue.z)
            {
                minValue.z = vertices[i].position.z;
            }

            //Find Max value
            if (vertices[i].position.x > maxValue.x)
            {
                maxValue.x = vertices[i].position.x;
            }
            if (vertices[i].position.y > maxValue.y)
            {
                maxValue.y = vertices[i].position.y;
            }
            if (vertices[i].position.z > maxValue.z)
            {
                maxValue.z = vertices[i].position.z;
            }
        }

        //print("minValue.x = " + minValue.x + " | minValue.y = " + minValue.y + " | minValue.z = " + minValue.z);
        //print("maxValue.x = " + maxValue.x + " | maxValue.y = " + maxValue.y + " | maxValue.z = " + maxValue.z);
        //print("Difference.x = " + (maxValue.x - minValue.x) + " | Difference.y = " + (maxValue.y - minValue.y) + " | Difference.z = " + (maxValue.z - minValue.z));

        //Change Point Position based on new Min/Max
        for (int i = 0; i < vertices.Count; i++)
        {
            //Length and Width
            vertices[i].position.x -= minValue.x;
            vertices[i].position.z -= minValue.z;

            //Height
            vertices[i].position.y *= 2;
            vertices[i].position.y -= 300;
        }

        posisionIsCorrected = true;
    }


    //--------------------

    void Triangulate()
    {
        //List<Tetrahedron> tetrahedra = Triangulate(vertices);
    }

    void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        // For each triangle
        for (var i = 0; i < vertices.Count; i += 3)
        {
            //Color
            Gizmos.color = gismoColor;

            //Cube
            Gizmos.DrawCube(vertices[i].position, new Vector3(pointSize, pointSize, pointSize));
        }
    }
}
