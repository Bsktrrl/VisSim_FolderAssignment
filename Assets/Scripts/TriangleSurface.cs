using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System;

public class TriangleSurface : MonoBehaviour
{
    public struct Hit
    {
        public Vector3 position;
        public Vector3 normal;
        public bool isHit;
    }

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


    //--------------------


    [SerializeField] TextAsset vertexFile;
    [SerializeField] TextAsset indicesFile;

    Mesh meshToSpawn;
    public List<Vertex> vertices = new();
    public List<int> indices = new();


    //--------------------


    private void Awake()
    {
        ReadVertexData();
        ReadIndicesData();
    }
    void Start()
    {
        BuildMesh();

        CalculateNormals();
        var hit = GetCollision(new Vector2(15.19f, 15.19f));

        print($"Hit: {hit.isHit}");
        print($"Pos: {hit.position}");
        print($"Norm: {hit.normal}");
    }


    //--------------------


    void ReadVertexData()
    {
        //Where to split lines
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //What seperates the numbers on the line
        var lineDelimiters = new[] { ' ', ' ', ' ' };

        //Split the lines into a string-array
        var lines = vertexFile.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(message:$"{vertexFile.name} was empty");
            return;
        }

        //Get first line, to see how many lines there will be
        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(message:$"{vertexFile.name} contains no vertex data");
            return;
        }

        //Split all lines based on the first line number
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{vertexFile.name} is missing data on line {i}");

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
    void ReadIndicesData()
    {
        //Where to split lines
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //What seperates the numbers on the line
        var lineDelimiters = new[] { ' ', ' ', ' ' };

        //Split the lines into a string-array
        var lines = indicesFile.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(message: $"{indicesFile.name} was empty");
            return;
        }

        //Get first line, to see how many lines there will be
        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(message: $"{indicesFile.name} contains no indices data");
            return;
        }

        //Split all lines based on the first line number
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{indicesFile.name} is missing data on line {i}");

                continue;
            }

            indices.Add(int.Parse(elements[0], CultureInfo.InvariantCulture));
            indices.Add(int.Parse(elements[1], CultureInfo.InvariantCulture));
            indices.Add(int.Parse(elements[2], CultureInfo.InvariantCulture));
        }
    }
    private void BuildMesh()
    {
        //Spawn Mesh
        meshToSpawn = new Mesh
        {
            vertices = vertices.Select(v => v.position).ToArray(),
            triangles = indices.ToArray()
        };

        GetComponent<MeshFilter>().mesh = meshToSpawn;
    }

    private void CalculateNormals()
    {
        for (var i = 0; i < indices.Count; i += 3)
        {
            int i1 = indices[i];
            int i2 = indices[i + 1];
            int i3 = indices[i + 2];

            var v1 = vertices[i1];
            var v2 = vertices[i2];
            var v3 = vertices[i3];

            var normal = Vector3.Cross(v2.position - v1.position, v3.position - v2.position).normalized;
            v1.normal += normal;
            v2.normal += normal;
            v3.normal += normal;
        }

        vertices.ForEach(v => v.normal = v.normal.normalized);
    }

    public Hit GetCollision(Vector2 position)
    {
        var hit = new Hit();
        hit.position.x = position.x;
        hit.position.z = position.y;

        for (var i = 0; i < indices.Count; i += 3)
        {
            int i1 = indices[i];
            int i2 = indices[i + 1];
            int i3 = indices[i + 2];

            var v1 = vertices[i1];
            var v2 = vertices[i2];
            var v3 = vertices[i3];

            var v1n = new Vector2(v1.position.x, v1.position.z);
            var v2n = new Vector2(v2.position.x, v2.position.z);
            var v3n = new Vector2(v3.position.x, v3.position.z);

            Vector3 temp = Barycentric(v1n, v2n, v3n, position);

            if (temp.x is >= 0f and <= 1f && temp.y is >= 0f and <= 1f && temp.z is >= 0f and <= 1f)
            {
                var y = vertices[i1].position.y * temp.x + vertices[i2].position.y * temp.y + vertices[i3].position.y * temp.z;

                hit.position.y = y;

                hit.normal = Vector3.Cross(v2.position - v1.position, v3.position - v2.position).normalized;
                hit.isHit = true;

                //Corrigate y
                //Vector3 p = hit.position;
                //Vector3 c = position;
                //Vector3 d = p - c;
                //Vector3 n = hit.normal;

                //var k = c + (Vector3.Dot(d, n) * n);

                return hit;
            }
        }

        return hit;
    }

    public static Vector3 Barycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;

        float u, v, w;
        v = (d11 * d20 - d01 * d21) / denom;
        w = (d00 * d21 - d01 * d20) / denom;
        u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }

    void OnDrawGizmos()
    {
        // For each triangle
        //for (var i = 0; i < indices.Count; i += 3)
        //{
        //    int i1 = indices[i];
        //    int i2 = indices[i + 1];
        //    int i3 = indices[i + 2];

        //    var v1 = vertices[i1];
        //    var v2 = vertices[i2];
        //    var v3 = vertices[i3];

        //    var normal = Vector3.Cross(v2.position - v1.position, v3.position - v2.position).normalized * 10;
        //    Gizmos.color = UnityEngine.Color.cyan;
        //    Gizmos.DrawLine(v1.position, v1.position + normal);
        //    Gizmos.DrawLine(v2.position, v2.position + normal);
        //    Gizmos.DrawLine(v3.position, v3.position + normal);
        //}
    }
}
