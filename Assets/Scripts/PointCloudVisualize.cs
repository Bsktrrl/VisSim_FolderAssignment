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


    #region Variables
    [Header("File")]
    [SerializeField] TextAsset pointCloudFile;
    [SerializeField] TextAsset generatedMeshFile_Vertices;
    [SerializeField] TextAsset generatedMeshFile_Indices;
    [SerializeField] MeshFilter meshFilter;

    [Header("Simulation")]
    [SerializeField] bool runSimulation;
    [SerializeField] bool generateFromMesh;
    [SerializeField] bool generateMeshToTxt;
    [SerializeField] bool showGizmo;

    [Header("Gismo")]
    public int resolution = 500;
    [SerializeField] int pointsJumpedOver = 1000;
    [SerializeField] float pointSize = 1;
    [SerializeField] Color gismoColor;

    [Header("Vertices")]
    [SerializeField] int verticesSize = 0;
    [SerializeField] int indicesSize = 0;
    [SerializeField] int verticesSizeAfter = 0;
    List<Vector3> vertices_PointCloud = new List<Vector3>();
    List<int> indices_PointCloud = new List<int>();
    List<Vector3> vertices_After = new List<Vector3>();
    
    bool posisionIsCorrected;
    bool triangulateIsFinished;
    bool generatedFromMesh;
    public bool programIsOff;

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

    public Mesh meshToSpawn;
    List<Vector3> vertex_Position = new List<Vector3>();
    List<int> vertex_Indices = new List<int>();
    List<Vector3> vertex_Normals = new List<Vector3>();
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
    }
    private void Start()
    {
        ReadFileData_Vertices();
        ReadFileData_Indices();

        CalculateWidthHeight();

        BuildMesh();

        //ReadVertexData();
        //CorrectPointPosition();
        //TriangulatePoints();

        generatedFromMesh = true;
    }
    private void Update()
    {
        if (runSimulation)
        {
            if (!programIsOff)
            {
                return;
            }

            if (generateFromMesh)
            {
                ReadFileData_Vertices();
                ReadFileData_Indices();

                CalculateWidthHeight();

                BuildMesh();

                generatedFromMesh = true;
            }
            else
            {
                ReadVertexData();
                CorrectPointPosition();
                TriangulatePoints();
            }

            programIsOff = false;
        }
        else
        {
            if (programIsOff)
            {
                return;
            }

            vertices_PointCloud.Clear();
            indices_PointCloud.Clear();
            verticesSize = 0;
            verticesSizeAfter = 0;

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

            generatedFromMesh = false;

            programIsOff = true;
        }

        //if (indices_PointCloud.Count > 0 && vertices_PointCloud.Count > 0)
        //{
        //    CalculateNormals();
        //}
    }


    //--------------------


    void ReadFileData_Vertices()
    {
        if (vertices_PointCloud.Count > 0)
        {
            return;
        }

        posisionIsCorrected = false;
        triangulateIsFinished = false;
        generatedFromMesh = false;

        //Where to split lines
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //What seperates the numbers on the line
        var lineDelimiters = new[] { ' ', ' ', ' ' };

        //Split the lines into a string-array
        var lines = generatedMeshFile_Vertices.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(message: $"{generatedMeshFile_Vertices.name} was empty");
            return;
        }

        //Get first line, to see how many lines there will be
        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(message: $"{generatedMeshFile_Vertices.name} contains no vertex data");
            return;
        }

        //Split all lines based on the first line number
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{generatedMeshFile_Vertices.name} is missing data on line {i}");

                continue;
            }

            Vector3 vertex = new Vector3
                (
                    float.Parse(elements[0]),
                    float.Parse(elements[1]),
                    float.Parse(elements[2])
                );

            vertices_PointCloud.Add(vertex);
        }

        //print("vertices.size: " + vertices_PointCloud.Count);
    }
    void ReadFileData_Indices()
    {
        if (indices_PointCloud.Count > 0)
        {
            return;
        }

        posisionIsCorrected = false;
        triangulateIsFinished = false;
        generatedFromMesh = false;

        //Where to split lines
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //What seperates the numbers on the line
        var lineDelimiters = new[] { ' ', ' ', ' ' };

        //Split the lines into a string-array
        var lines = generatedMeshFile_Indices.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(message: $"{generatedMeshFile_Indices.name} was empty");
            return;
        }

        //Get first line, to see how many lines there will be
        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(message: $"{generatedMeshFile_Indices.name} contains no vertex data");
            return;
        }

        //Split all lines based on the first line number
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(message: $"{generatedMeshFile_Indices.name} is missing data on line {i}");

                continue;
            }

            Vector3 vertex = new Vector3
                (
                    float.Parse(elements[0]),
                    float.Parse(elements[1]),
                    float.Parse(elements[2])
                );

            indices_PointCloud.Add(int.Parse(elements[0]));
            indices_PointCloud.Add(int.Parse(elements[1]));
            indices_PointCloud.Add(int.Parse(elements[2]));
        }

        //print("indices.size: " + indices_PointCloud.Count);
    }
    
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
        var lineDelimiters = new[] { ' ', ' ', ' ' };

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

            Vector3 vertex = new Vector3
                (
                    float.Parse(elements[0], CultureInfo.InvariantCulture),
                    float.Parse(elements[2], CultureInfo.InvariantCulture),
                    float.Parse(elements[1], CultureInfo.InvariantCulture)
                );

            vertices_PointCloud.Add(vertex);
        }

        verticesSize = vertices_PointCloud.Count;
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
        }

        posisionIsCorrected = true;
    }


    //--------------------


    void BuildMesh()
    {
        meshToSpawn = new Mesh();

        meshToSpawn.vertices = vertices_PointCloud.ToArray();
        meshToSpawn.triangles = indices_PointCloud.ToArray();

        meshToSpawn.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = meshToSpawn;
    }

    void CalculateWidthHeight()
    {
        if (vertices_PointCloud.Count <= 0)
        {
            return;
        }

        xmin = vertices_PointCloud[0].x;
        float xmax = vertices_PointCloud[0].x;
        ymin = vertices_PointCloud[0].y;
        float ymax = vertices_PointCloud[0].y;
        zmin = vertices_PointCloud[0].z;

        // Search for x/y min-max
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            if (vertices_PointCloud[i] == Vector3.zero) continue;

            if (vertices_PointCloud[i].x < xmin) xmin = vertices_PointCloud[i].x;
            if (vertices_PointCloud[i].x > xmax) xmax = vertices_PointCloud[i].x;
            if (vertices_PointCloud[i].z < ymin) ymin = vertices_PointCloud[i].z;
            if (vertices_PointCloud[i].z > ymax) ymax = vertices_PointCloud[i].z;
            if (vertices_PointCloud[i].y < zmin) zmin = vertices_PointCloud[i].y;
        }

        float width = xmax - xmin;
        float height = ymax - ymin;
        vertex_width = width / resolution;
        vertex_height = height / resolution;

        print("Widht: " + vertex_width + " | height: " + vertex_height);
    }


    void TriangulatePoints()
    {
        if (triangulateIsFinished)
        {
            return;
        }

        Triangulate();

        //var hit = GetCollision(new Vector2(15.19f, 15.19f));

        //print($"New Hit: {hit.isHit}");
        //print($"New Pos: {hit.position}");
        //print($"New Norm: {hit.normal}");

        triangulateIsFinished = true;
    }
    void Triangulate()
    {
        //Spawn Mesh
        meshToSpawn = new Mesh();
        //{
        //    vertices = vertex_Position.ToArray(),
        //    triangles = vertex_Indices.ToArray()
        //};

        vertices_After = new List<Vector3>();

        //Swap .y and .z so that the Up (y) is last (z)
        List<Vector3> vertices_Temp = new List<Vector3>();
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            float x = vertices_PointCloud[i].x;
            float y = vertices_PointCloud[i].z;
            float z = vertices_PointCloud[i].y;

            vertices_Temp.Add(new Vector3(x, y, z));
        }

        //Construct a mesh based on a set resolution
        vertex_Position = GetVertices(vertices_Temp, resolution);
        vertices_After = vertex_Position;

        meshToSpawn.vertices = vertex_Position.ToArray();
        verticesSizeAfter = meshToSpawn.vertices.Length;

        // calculate triangles based on the resolution 
        vertex_Indices = GetTriangles(resolution);
        meshToSpawn.triangles = vertex_Indices.ToArray();

        meshToSpawn.RecalculateNormals();
        //meshFilter.mesh = meshToSpawn;

        GetComponent<MeshFilter>().mesh = meshToSpawn;

        //Save the new constucted mesh to a text file
        if (generateMeshToTxt)
        {
            TextReaderWriter.WriteText(AssetDatabase.GenerateUniqueAssetPath("Assets/DataFiles/generatedMesh_vertices.txt"), meshToSpawn.vertices.ToList());

            List<Vector3> temp = new List<Vector3>();
            for (int i = 0; i < meshToSpawn.triangles.Length; i += 3)
            {
                temp.Add(new Vector3(meshToSpawn.triangles[i], meshToSpawn.triangles[i + 1], meshToSpawn.triangles[i + 2]));
            }
            
            TextReaderWriter.WriteText(AssetDatabase.GenerateUniqueAssetPath("Assets/DataFiles/generatedMesh_indices.txt"), temp);
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


    //--------------------


    void CalculateNormals()
    {
        for (var i = 0; i < indices_PointCloud.Count; i += 3)
        {
            int i1 = indices_PointCloud[i];
            int i2 = indices_PointCloud[i + 1];
            int i3 = indices_PointCloud[i + 2];

            var v1 = vertices_PointCloud[i1];
            var v2 = vertices_PointCloud[i2];
            var v3 = vertices_PointCloud[i3];

            var normal = Vector3.Cross(v2 - v1, v3 - v2).normalized;
            v1 += normal;
            v2 += normal;
            v3 += normal;
        }

        vertices_PointCloud.ForEach(v => v = v.normalized);
    }
    public int FindMapPos(float x, float z)
    {
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            if (vertices_PointCloud[i].x <= x && vertices_PointCloud[i].x + vertex_width > x && vertices_PointCloud[i].z <= z && vertices_PointCloud[i].z + vertex_height > z)
            {
                return i;
            }
        }

        //for (int i = 0; i < vertices_PointCloud.Count; i++)
        //{
        //    if (i % resolution != resolution - 1 && (i / resolution) != resolution - 1)
        //    {
        //        if (vertices_PointCloud[i].x <= x && vertices_PointCloud[i + 1].x > x && vertices_PointCloud[i].z <= z && vertices_PointCloud[i + resolution].z > z)
        //        {
        //            return i;
        //        }
        //    }
        //}

        return -1;
    }

    public Hit GetCollision(Vector2 position, int mapPos)
    {
        #region Adrians GetCollision
        ////print("Position: " + position);

        //var hit = new Hit();
        //hit.position.x = position.x;
        //hit.position.z = position.y;

        ////Check out of bounds
        //if ((mapPos) >= vertices_PointCloud.Count || mapPos < 0)
        //{
        //    print("0: " + vertices_PointCloud.Count + " | " + (mapPos));
        //    return new Hit() { isHit = false };
        //}
        //if ((mapPos + 1) >= vertices_PointCloud.Count)
        //{
        //    print("1: " + vertices_PointCloud.Count + " | " + (mapPos + 1));
        //    return new Hit() { isHit = false };
        //}
        //if ((mapPos + resolution) >= vertices_PointCloud.Count)
        //{
        //    print("2");
        //    return new Hit() { isHit = false };
        //}
        //if ((mapPos + resolution + 1) >= vertices_PointCloud.Count)
        //{
        //    print("3");
        //    return new Hit() { isHit = false };
        //}

        //Vector3 p = vertices_PointCloud[mapPos];
        //Vector3 q = vertices_PointCloud[mapPos + 1];
        //Vector3 r = vertices_PointCloud[mapPos + resolution];
        //Vector3 s = vertices_PointCloud[mapPos + resolution + 1];

        //print("P: " + p.y + " | Q: " + q.y + " | r: " + r.y);

        //Vector3 temp = Barycentric(p, q, r, position);
        //Vector3 temp2 = Barycentric(q, s, r, position);

        //print("Temp: " + temp + " | Temp2: " + temp2);

        //if (temp.x >= 0f && temp.x <= 1f && temp.y >= 0f && temp.y <= 1f && temp.z >= 0f && temp.z <= 1f)
        //{
        //    float y = (p.y * temp.x) + (q.y * temp.y) + (r.y * temp.z);

        //    hit.position.y = y;

        //    hit.normal = Vector3.Cross(q - p, r - p).normalized;
        //    hit.isHit = true;

        //    return hit;
        //}
        //if (temp2.x >= 0f && temp2.x <= 1f && temp2.y >= 0f && temp2.y <= 1f && temp2.z >= 0f && temp2.z <= 1f)
        //{
        //    float y = (q.y * temp2.x) + (s.y * temp2.y) + (r.y * temp2.z);

        //    hit.position.y = y;

        //    hit.normal = Vector3.Cross(s - q, r - q).normalized;
        //    hit.isHit = true;

        //    return hit;
        //}
        #endregion

        #region Aleksanders GetCollision


        #endregion


        //for (var i = 0; i < meshToSpawn.triangles.Length; i += 3)
        //{
        //    int i1 = meshToSpawn.triangles[i];
        //    int i2 = meshToSpawn.triangles[i + 1];
        //    int i3 = meshToSpawn.triangles[i + 2];

        //    var v1 = meshToSpawn.vertices[i1];
        //    var v2 = meshToSpawn.vertices[i2];
        //    var v3 = meshToSpawn.vertices[i3];

        //    var v1n = new Vector2(v1.x, v1.z);
        //    var v2n = new Vector2(v2.x, v2.z);
        //    var v3n = new Vector2(v3.x, v3.z);

        //    Vector3 temp = Barycentric(v1n, v2n, v3n, position);

        //    if (temp.x >= 0f && temp.x <= 1f && temp.y >= 0f && temp.y <= 1f && temp.z >= 0f && temp.z <= 1f)
        //    {
        //        //print("Success: " + i);

        //        float y = meshToSpawn.vertices[i1].y * temp.x + meshToSpawn.vertices[i2].y * temp.y + meshToSpawn.vertices[i3].y * temp.z;

        //        hit.position.y = y;

        //        hit.normal = Vector3.Cross(v2 - v1, v3 - v2).normalized;
        //        hit.isHit = true;

        //        return hit;
        //    }
        //}

        return new Hit();
    }


    static Vector3 Barycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        //Vector2 PQ = Q - P;
        //Vector2 PR = R - P;

        //Vector3 PQR = Cross2D(PQ, PR);
        //float normal = PQR.magnitude;


        //Vector2 XP = P - X;
        //Vector2 XQ = Q - X;
        //Vector2 XR = R - X;

        //float x = Cross2D(XP, XQ).z / normal;
        //float y = Cross2D(XQ, XR).z / normal;
        //float z = Cross2D(XR, XP).z / normal;

        //return new Vector3(x, y, z);

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




    //--------------------


    public Vector3 CheckCollission(Vector3 pos, Vector3 vel, int index, float r)
    {
        Vector2 X = new Vector2(pos.x, pos.z);

        List<Vector3> ALL = GetSquare(index);

        Vector2 P = new Vector2(ALL[0].x, ALL[0].z);
        Vector2 Q = new Vector2(ALL[2].x, ALL[2].z);
        Vector2 R = new Vector2(ALL[1].x, ALL[1].z);


        Vector3 bary1 = Bary(X, Q, R, P);

        P = new Vector2(ALL[1].x, ALL[1].z);
        Q = new Vector2(ALL[2].x, ALL[2].z);
        R = new Vector2(ALL[3].x, ALL[3].z);

        Vector3 bary2 = Bary(X, Q, R, P);

        if (bary1.x >= 0f && bary1.x <= 1f && bary1.y >= 0f && bary1.y <= 1f && bary1.z >= 0f && bary1.z <= 1f)
        {
            float y = bary1.x * ALL[0].y + bary1.y * ALL[2].y + bary1.z * ALL[1].y;
            float distance = Mathf.Abs(pos.y - y);

            if (distance < r)
            {
                Vector3 PQ = ALL[2] - ALL[0];
                Vector3 PR = ALL[1] - ALL[0];

                Vector3 normal_unit_vector = Vector3.Cross(PQ, PR);
                normal_unit_vector = normal_unit_vector.normalized;

                return normal_unit_vector;
            }
        }
        else if (bary2.x >= 0f && bary2.x <= 1f && bary2.y >= 0f && bary2.y <= 1f && bary2.z >= 0f && bary2.z <= 1f)
        {
            float y = (bary2.x * ALL[1].y) + (bary2.y * ALL[2].y) + (bary2.z * ALL[3].y);
            float distance = Mathf.Abs(pos.y - y);

            if (distance < r)
            {
                Vector3 PQ = ALL[2] - ALL[1];
                Vector3 PR = ALL[3] - ALL[1];
                Vector3 normal_unit_vector = Vector3.Cross(PQ, PR);
                normal_unit_vector = normal_unit_vector.normalized;
                return normal_unit_vector;
            }
        }

        return Vector3.one * -1f;
    }

    Vector3 Bary(Vector2 playerxz, Vector2 P, Vector2 Q, Vector2 R)
    {
        Vector2 PQ = Q - P;
        Vector2 PR = R - P;
        Vector3 PQR = Cross2D(PQ, PR);
        float normal = PQR.magnitude;
        Vector2 XP = P - playerxz;
        Vector2 XQ = Q - playerxz;
        Vector2 XR = R - playerxz;
        float x = Cross2D(XP, XQ).z / normal;
        float y = Cross2D(XQ, XR).z / normal;
        float z = Cross2D(XR, XP).z / normal;
        return new Vector3(x, y, z);
    }
    Vector3 Cross2D(Vector2 A, Vector2 B)
    {
        Vector3 cross = new Vector3();
        cross.z = (A.x * B.y) - (A.y * B.x);
        return cross;
    }

    public List<Vector3> GetSquare(int index)
    {
        print("index: " + index);

        List<Vector3> result = new List<Vector3>()
            {
                vertices_PointCloud[index],
                vertices_PointCloud[index + 1],
                vertices_PointCloud[index + resolution],
                vertices_PointCloud[index + resolution + 1]
            };

        return result;
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

public struct Int2
{
    public int x;
    public int y;
}
