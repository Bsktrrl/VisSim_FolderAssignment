using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[ExecuteInEditMode]
public class PontCloudVisualize : MonoBehaviour
{
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

    [SerializeField] GameObject point;
    [SerializeField] GameObject pointParent;
    [SerializeField] TextAsset pointCloudFile;
    [SerializeField] float pointSize = 1;

    public List<Vertex> vertices = new List<Vertex>();
    [SerializeField] int verticesSize = 0;

    [SerializeField] bool posisionIsCorrected;
    List<GameObject> pointList = new List<GameObject>();

    private void Start()
    {
        //DeleteAllPoints();

        ReadVertexData();
        ChangePosition();
        SpawnPoints();

        ChangePointSize();
        GetVerticesSize();
    }


    void DeleteAllPoints()
    {
        if (pointList.Count < 0)
        {
            return;
        }

        for (int i = 0; i < pointList.Count; i++)
        {
            DestroyImmediate(pointList[i].gameObject);
        }

        pointList.Clear();
    }

    void ReadVertexData()
    {
        if (pointList.Count >= vertices.Count && vertices.Count != 0)
        {
            return;
        }

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
        for (int i = 1; i <= numVertices; i++)
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
                    float.Parse(elements[1], CultureInfo.InvariantCulture),
                    float.Parse(elements[2], CultureInfo.InvariantCulture))
                );

            vertices.Add(vertex);
        }
    }
    void ChangePosition()
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

        print("minValue.x = " + minValue.x + " | minValue.y = " + minValue.y + " | minValue.z = " + minValue.z);
        print("maxValue.x = " + maxValue.x + " | maxValue.y = " + maxValue.y + " | maxValue.z = " + maxValue.z);

        //Change Point Position based on new Min/Max
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i].position.x -= minValue.x;
            vertices[i].position.y -= minValue.y;
            vertices[i].position.z -= minValue.z;
        }

        for (int i = 0; i < pointList.Count; i++)
        {
            pointList[i].transform.position = vertices[i].position;
        }

        posisionIsCorrected = true;
    }
    void SpawnPoints()
    {
        if (pointList.Count >= vertices.Count && vertices.Count != 0)
        {
            return;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            pointList.Add(Instantiate(point, new Vector3(vertices[i].position.x, vertices[i].position.y, vertices[i].position.z), Quaternion.identity) as GameObject);
            pointList[i].transform.parent = pointParent.transform;
        }
    }


    void ChangePointSize()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            pointList[i].transform.localScale = new Vector3(pointSize, pointSize, pointSize);
        }
    }
    void GetVerticesSize()
    {
        verticesSize = vertices.Count;
    }
}
