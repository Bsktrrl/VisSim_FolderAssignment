using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class PointCloudVisualize : MonoBehaviour
{
    public static PointCloudVisualize instance { get; set; } //Singleton


    //--------------------


    [Header("File")]
    [SerializeField] TextAsset pointCloudFile;
    [SerializeField] MeshFilter meshFilter;

    [Header("Simulation")]
    [SerializeField] bool runSimulation;
    [SerializeField] bool showGizmo;
    [SerializeField] bool generateMeshToTxt;

    [Header("Gismo")]
    [SerializeField] int resolution = 500;
    [SerializeField] int pointsJumpedOver = 1000;
    [SerializeField] float pointSize = 1;
    [SerializeField] Color gismoColor;

    [Header("Vertices")]
    [SerializeField] int verticesSize = 0;
    List<Vector3> vertices_PointCloud = new List<Vector3>();
    List<Vector3> vertices_After;
    bool posisionIsCorrected;
    bool triangulateIsFinished;

    [Header("Other")]
    float xmin;
    float ymin;
    float zmin;
    float vertex_width;
    float vertex_height;
    public Vector2 min;
    public Vector2 max;
    int[] heightmap;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    List<Vector3> vertex_positions = new List<Vector3>();

    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    int cachedInstanceCount = -1;

    Mesh meshToSpawn;


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

            TriangulatePoints();
        }
        else
        {
            if (vertices_PointCloud.Count > 0)
            {
                vertices_PointCloud.Clear();
                verticesSize = 0;

                min = Vector2.zero;
                max = Vector2.zero;

                heightmap = null;

                xmin = 0;
                ymin = 0;
                zmin = 0;

                vertex_width = 0;
                vertex_height = 0;

                vertices_After.Clear();

                meshFilter.mesh = null;
            }
        }
    }


    //--------------------


    void ReadVertexData()
    {
        if (vertices_PointCloud.Count > 0)
        {
            return;
        }

        posisionIsCorrected = false;
        triangulateIsFinished = false;

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
        for (int i = 1; i <= numVertices; i += (1 + pointsJumpedOver))
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{pointCloudFile.name} is missing data on line {i}");

                continue;
            }

            //Vertex vertex = new Vertex(new Vector3
            //    (
            //        float.Parse(elements[0], CultureInfo.InvariantCulture),
            //        float.Parse(elements[2], CultureInfo.InvariantCulture),
            //        float.Parse(elements[1], CultureInfo.InvariantCulture))
            //    );

            Vector3 vertex = new Vector3(
                    float.Parse(elements[0], CultureInfo.InvariantCulture),
                    float.Parse(elements[2], CultureInfo.InvariantCulture),
                    float.Parse(elements[1], CultureInfo.InvariantCulture)
                );

            vertices_PointCloud.Add(vertex);
            verticesSize = vertices_PointCloud.Count;
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
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            //Find Min value
            if (vertices_PointCloud[i].x < minValue.x)
            {
                minValue.x = (float)vertices_PointCloud[i].x;
            }
            if (vertices_PointCloud[i].y < minValue.y)
            {
                minValue.y = (float)vertices_PointCloud[i].y;
            }
            if (vertices_PointCloud[i].z < minValue.z)
            {
                minValue.z = (float)vertices_PointCloud[i].z;
            }

            //Find Max value
            if (vertices_PointCloud[i].x > maxValue.x)
            {
                maxValue.x = (float)vertices_PointCloud[i].x;
            }
            if (vertices_PointCloud[i].y > maxValue.y)
            {
                maxValue.y = (float)vertices_PointCloud[i].y;
            }
            if (vertices_PointCloud[i].z > maxValue.z)
            {
                maxValue.z = (float)vertices_PointCloud[i].z;
            }
        }

        //print("minValue.x = " + minValue.x + " | minValue.y = " + minValue.y + " | minValue.z = " + minValue.z);
        //print("maxValue.x = " + maxValue.x + " | maxValue.y = " + maxValue.y + " | maxValue.z = " + maxValue.z);
        //print("Difference.x = " + (maxValue.x - minValue.x) + " | Difference.y = " + (maxValue.y - minValue.y) + " | Difference.z = " + (maxValue.z - minValue.z));

        //Change Point Position based on new Min/Max
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            vertices_PointCloud[i] = new Vector3
                (
                    vertices_PointCloud[i].x - minValue.x, 
                    vertices_PointCloud[i].y * 2 - 300,
                    vertices_PointCloud[i].z - minValue.z
                );

            //Length and Width
            //vertices_PointCloud[i].x -= minValue.x;
            //vertices_PointCloud[i].z -= minValue.z;

            ////Height
            //vertices_PointCloud[i].y *= 2;
            //vertices_PointCloud[i].y -= 300;
        }

        posisionIsCorrected = true;
    }


    //--------------------


    void TriangulatePoints()
    {
        if (triangulateIsFinished)
        {
            return;
        }

        Triangulate();
        BuildMesh();

        triangulateIsFinished = true;
    }

    void Triangulate()
    {
        // Create a mesh object and a vertices array
        meshToSpawn = new Mesh();
        vertices_After = new List<Vector3>();

        //Consruct a mesh based on a set resolution
        vertices_After = GetVertices(vertices_PointCloud, resolution);
        meshToSpawn.vertices = vertices_After.ToArray();

        // calculate triangles based on the resolution 
        meshToSpawn.triangles = GetTriangles(resolution).ToArray();
        meshToSpawn.RecalculateNormals();
        meshFilter.mesh = meshToSpawn;

        //Save the new constucted mesh to a text file
        if (generateMeshToTxt)
        {
            TextReaderWriter.WriteText(AssetDatabase.GenerateUniqueAssetPath("Assets/DataFiles/generatedMesh.txt"), vertices_After);
        }

        // save the heightmap in a float array so we can use it 
        heightmap = new int[resolution * resolution];
        for (int i = 0; i < vertices_After.Count; i++)
        {
            if (float.IsNaN(vertices_After[i].y)) Debug.Log("NAN when inserting heights to heightmap");
            heightmap[i] = (int)vertices_After[i].y;
        }
    }
    List<Vector3> GetVertices(List<Vector3> vertex_positions, int resolution)
    {
        // Important to remember that z is the up direction in this array
        // Set the min and max to the first element and compare from there
        xmin = vertex_positions[0].x;
        float xmax = vertex_positions[0].x;
        ymin = vertex_positions[0].y;
        float ymax = vertex_positions[0].y;
        zmin = vertex_positions[0].z;

        // Search for x/y min-max
        for (int i = 0; i < vertex_positions.Count; i++)
        {
            if (vertex_positions[i] == Vector3.zero) continue;

            if (vertex_positions[i].x < xmin) xmin = vertex_positions[i].x;
            if (vertex_positions[i].x > xmax) xmax = vertex_positions[i].x;
            if (vertex_positions[i].y < ymin) ymin = vertex_positions[i].y;
            if (vertex_positions[i].y > ymax) ymax = vertex_positions[i].y;
            if (vertex_positions[i].z < zmin) zmin = vertex_positions[i].z;
        }

        float width = xmax - xmin;
        float height = ymax - ymin;
        vertex_width = width / resolution;
        vertex_height = height / resolution;

        // Create a 2D Array that will "sort" all points to their corresponding Vertex
        List<List<List<Vector3>>> areas = new List<List<List<Vector3>>>();
        for (int x = 0; x < resolution; x++)
        {
            areas.Add(new List<List<Vector3>>());
            for (int y = 0; y < resolution; y++)
            {
                areas[x].Add(new List<Vector3>());
            }
        }

        // The Actual Sorting
        for (int i = 0; i < vertex_positions.Count; i++)
        {
            int x = (int)Map(vertex_positions[i].x, xmin, xmax + 1, 0, resolution);
            int y = (int)Map(vertex_positions[i].y, ymin, ymax + 1, 0, resolution);
            if (x >= areas.Count || x < 0)
            {
                Debug.Log(x + " is out of bounds, max size is " + areas.Count);
                Debug.Log("x: " + vertex_positions[i].x + ", min / max: " + xmin + "/" + xmax);
                Debug.Log("y: " + vertex_positions[i].y + ", min / max: " + ymin + "/" + ymax);
                continue;
            }
            if (y >= areas[x].Count || y < 0)
            {
                Debug.Log(y + " is out of bounds, max size is " + areas[x].Count);
                Debug.Log("x: " + vertex_positions[i].x + ", min / max: " + xmin + "/" + xmax);
                Debug.Log("y: " + vertex_positions[i].y + ", min / max: " + ymin + "/" + ymax);
                continue;
            }
            if (vertex_positions[i] != Vector3.zero)
            {
                areas[x][y].Add(vertex_positions[i]);
            }
        }

        // Create mesh
        List<Vector3> result = new List<Vector3>();
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float z = 0f;
                for (int i = 0; i < areas[x][y].Count; i++)
                {
                    z += areas[x][y][i].z;
                }
                if (areas[x][y].Count > 0)
                {
                    result.Add(new Vector3((vertex_width / 2f) + (vertex_width * x), z / areas[x][y].Count - zmin, (vertex_height / 2f) + (vertex_height * y)));
                }
                else
                {
                    result.Add(new Vector3((vertex_width / 2f) + (vertex_width * x), 0f, (vertex_height / 2f) + (vertex_height * y)));
                }
            }
        }

        min = new Vector2((vertex_width / 2f), (vertex_width / 2f));
        max = new Vector2((vertex_width / 2f) + (vertex_width * resolution), (vertex_width / 2f) + (vertex_width * resolution));
        
        return result;
    }
    public static float Map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    List<int> GetTriangles(int resolution)
    {
        List<int> indexes = new List<int>();

        for (int x = 0; x < resolution - 1; x++)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                indexes.Add(x + (y * resolution));
                indexes.Add(x + (y * resolution) + 1);
                indexes.Add(x + (y * resolution) + resolution);

                indexes.Add(x + (y * resolution) + 1);
                indexes.Add(x + (y * resolution) + resolution + 1);
                indexes.Add(x + (y * resolution) + resolution);
            }
        }

        return indexes;
    }

    private void BuildMesh()
    {
        //Spawn Mesh
        //meshToSpawn = new Mesh
        //{
        //    vertices = verticesAfter.Select(v => v).ToArray(),
        //    triangles = heightmap
        //};

        //GetComponent<MeshFilter>().mesh = meshToSpawn;
    }

    //--------------------


    void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        // For each triangle
        for (var i = 0; i < vertices_PointCloud.Count; i += 3)
        {
            //Color
            Gizmos.color = gismoColor;

            //Cube
            Gizmos.DrawCube(new Vector3(vertices_PointCloud[i].x, vertices_PointCloud[i].y, vertices_PointCloud[i].z), new Vector3(pointSize, pointSize, pointSize));
        }
    }
}
