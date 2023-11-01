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


    #region Variables
    [Header("File")]
    [SerializeField] TextAsset pointCloudFile;
    [SerializeField] TextAsset generatedMeshFile_Vertices;
    [SerializeField] TextAsset generatedMeshFile_Indices;

    [Header("Simulation")]
    [SerializeField] bool runSimulation;

    [Space(10)]

    [SerializeField] bool generateFromMesh;
    [SerializeField] bool generateMeshToTxt;

    [Space(10)]

    public bool showGizmo;
    [SerializeField] float gizmoSize = 1;
    [SerializeField] Color gizmoColor = new Color(0, 0, 0, 1);

    [Header("Generating")]
    public int gridSize = 120;
    [SerializeField] int pointsJumpedOver = 100;

    [Space(10)]

    [SerializeField] int verticesListSize = 0;

    List<Vector3> vertices_PointCloud = new List<Vector3>();
    List<int> indices_PointCloud = new List<int>();

    [Header("Other")]
    [SerializeField] Material meshMaterial;
    [HideInInspector] public Mesh tempMesh;

    public Vector2 meshVertices_min;
    public Vector2 meshVertices_max;
    float x_min;
    float y_min;
    float z_min;

    float meshVertex_width;
    float meshVertex_height;
    int[] hightPointList;

    //Checks for letting the code run in the Editor
    bool posisionIsCorrected;
    bool triangulateIsFinished;
    bool generatedFromMesh;
    [HideInInspector] public bool programIsOff;
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

        //Setting up the tempMesh
        tempMesh = new Mesh();
    }
    private void Start()
    {
        //Build a mesh from a generated Mesh
        ReadFileData_Vertices();
        ReadFileData_Indices();

        CalculateWidthHeight();

        BuildMesh();

        generatedFromMesh = true;

        //ReadVertexData();
        //CorrectPointPosition();
        //TriangulatePoints();
    }
    private void Update()
    {
        if (runSimulation)
        {
            if (!programIsOff) { return; }

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
                ReadFileData();
                CorrectPointPosition();
                TriangulatePoints();
            }

            programIsOff = false;
        }
        else
        {
            if (programIsOff) { return; }

            vertices_PointCloud.Clear();
            indices_PointCloud.Clear();

            verticesListSize = 0;

            meshVertices_min = Vector2.zero;
            meshVertices_max = Vector2.zero;
            x_min = 0;
            y_min = 0;
            z_min = 0;

            meshVertex_width = 0;
            meshVertex_height = 0;

            GetComponent<MeshFilter>().mesh = null;
            hightPointList = null;

            generatedFromMesh = false;
            programIsOff = true;
        }
    }


    //--------------------


    void ReadFileData_Vertices()
    {
        if (vertices_PointCloud.Count > 0) { return; }

        posisionIsCorrected = false;
        triangulateIsFinished = false;
        generatedFromMesh = false;

        //How to split the lines
        string[] fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //How to seperate each lines
        char[] lineDelimiters = new[] { ' ', ' ', ' ' };

        //Separate each line
        string[] lines = generatedMeshFile_Vertices.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            print(generatedMeshFile_Vertices.name + " is empty");
            return;
        }

        //Check first line to see the number of lines in the file
        int numVertices = int.Parse(lines[0]);
        if (numVertices < 1)
        {
            print(generatedMeshFile_Vertices.name + " contains no vertex data");
            return;
        }

        //Split each line into 3 lines, covering the "x, y, z"-coordinates
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length < 3)
            {
                print(generatedMeshFile_Vertices.name + " is missing data on line " + i);
                continue;
            }

            //Make a new Vector3 containing "x, y, z"-coordinates
            Vector3 vertex = new Vector3
                (
                    float.Parse(elements[0]),
                    float.Parse(elements[1]),
                    float.Parse(elements[2])
                );

            //Add the Vector3 to the "vertices_PointCloud"-List
            vertices_PointCloud.Add(vertex);
        }

        //Display the size of "vertices_PointCloud"-List
        verticesListSize = vertices_PointCloud.Count;
    }
    void ReadFileData_Indices()
    {
        if (indices_PointCloud.Count > 0) { return; }

        posisionIsCorrected = false;
        triangulateIsFinished = false;
        generatedFromMesh = false;

        //How to split the lines
        string[] fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //How to seperate each lines
        char[] lineDelimiters = new[] { ' ', ' ', ' ' };

        //Separate each line
        string[] lines = generatedMeshFile_Indices.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1)
        {
            print(generatedMeshFile_Indices.name + " is empty");
            return;
        }

        //Check first line to see the number of lines in the file
        int numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(generatedMeshFile_Indices.name + " contains no vertex data");
            return;
        }

        //Split each line into 3 lines
        for (int i = 1; i <= numVertices; i++)
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(generatedMeshFile_Indices.name + " is missing data on line " + i);

                continue;
            }

            //Add the lines to the "indices_PointCloud"-List
            indices_PointCloud.Add(int.Parse(elements[0]));
            indices_PointCloud.Add(int.Parse(elements[1]));
            indices_PointCloud.Add(int.Parse(elements[2]));
        }
    }
    
    void ReadFileData()
    {
        if (vertices_PointCloud.Count > 0) { return; }

        posisionIsCorrected = false;
        triangulateIsFinished = false;

        //How to split the lines
        string[] fileDelimiters = new[] { "\r\n", "\r", "\n" };

        //How to seperate each lines
        char[] lineDelimiters = new[] { ' ', ' ', ' ' };

        //Separate each line
        string[] lines = pointCloudFile.text.Split(fileDelimiters, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1)
        {
            print(pointCloudFile.name + " is empty");
            return;
        }

        //Check first line to see the number of lines in the file
        int numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            print(pointCloudFile.name + " contains no vertex data");
            return;
        }

        //Split each line into 3 lines, covering the "x, y, z"-coordinates
        for (int i = 1; i <= numVertices; i += (1 + pointsJumpedOver))
        {
            var elements = lines[i].Split(lineDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

            if (elements.Length < 3)
            {
                print(pointCloudFile.name + " is missing data on line " + i);
                continue;
            }

            //Make a new Vector3 containing "x, y, z"-coordinates
            Vector3 vertex = new Vector3
                (
                    float.Parse(elements[0], CultureInfo.InvariantCulture),
                    float.Parse(elements[2], CultureInfo.InvariantCulture),
                    float.Parse(elements[1], CultureInfo.InvariantCulture)
                );

            //Add the Vector3 to the "vertices_PointCloud"-List
            vertices_PointCloud.Add(vertex);
        }

        //Display the size of "vertices_PointCloud"-List
        verticesListSize = vertices_PointCloud.Count;
    }


    //--------------------


    void CorrectPointPosition()
    {
        if (posisionIsCorrected) { return; }

        Vector3 minValue = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3 maxValue = new Vector3(int.MinValue, int.MinValue, int.MinValue);

        //Find Min and Max value of the "vertices_PointCloud"-List
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

        //Correct Point Position based on new Min/Max, and set new height
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
        //make a temporary MeshObject
        tempMesh = new Mesh();

        //Insert vertices and indices into the temporary MeshObject
        tempMesh.vertices = vertices_PointCloud.ToArray();
        tempMesh.triangles = indices_PointCloud.ToArray();

        //Recalculate the temporary MeshObject's normals
        tempMesh.RecalculateNormals();

        //Input data into the real meshObject
        GetComponent<MeshFilter>().mesh = tempMesh;
    }

    void CalculateWidthHeight()
    {
        if (vertices_PointCloud.Count <= 0) { return; }

        //Set the min and max values to the first in the "vertices_PointCloud"_List
        x_min = vertices_PointCloud[0].x;
        float x_max = x_min;

        y_min = vertices_PointCloud[0].y;
        float y_max = y_min;

        z_min = vertices_PointCloud[0].z;

        //Compare and find the true min and max values in the "vertices_PointCloud"_List
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            if (vertices_PointCloud[i] == Vector3.zero)
                continue;

            if (vertices_PointCloud[i].x < x_min)
            {
                x_min = vertices_PointCloud[i].x;
            }
            if (vertices_PointCloud[i].x > x_max)
            {
                x_max = vertices_PointCloud[i].x;
            }
            if (vertices_PointCloud[i].z < y_min)
            {
                y_min = vertices_PointCloud[i].z;
            }
            if (vertices_PointCloud[i].z > y_max)
            {
                y_max = vertices_PointCloud[i].z;
            }
            if (vertices_PointCloud[i].y < z_min)
            {
                z_min = vertices_PointCloud[i].y;
            }
        }

        //Set width and height of the MeshObject, based on the resolution
        float widthTemp = x_max - x_min;
        float heightTemp = y_max - y_min;

        meshVertex_width = widthTemp / gridSize;
        meshVertex_height = heightTemp / gridSize;
    }


    //--------------------


    void TriangulatePoints()
    {
        if (triangulateIsFinished) { return; }

        Triangulate();

        triangulateIsFinished = true;
    }
    void Triangulate()
    {
        //Reset tempMesh
        tempMesh = new Mesh();

        //Set "x, y, z"-positions according to Unity engine's logic
        List<Vector3> vertices_Temp = new List<Vector3>();
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            float x = vertices_PointCloud[i].x;
            float y = vertices_PointCloud[i].z;
            float z = vertices_PointCloud[i].y;

            vertices_Temp.Add(new Vector3(x, y, z));
        }

        //Build the tempMesh, calculating and inserting the vertices and indices into it
        tempMesh.vertices = GetVertices(vertices_Temp, gridSize).ToArray();
        tempMesh.triangles = GetIndices(gridSize).ToArray();

        //Recalculate the tempMesh's normals
        tempMesh.RecalculateNormals();

        //Input data into the real meshObject
        GetComponent<MeshFilter>().mesh = tempMesh;

        //Save the real meshObject into a .txt-file for vertices and indices
        if (generateMeshToTxt)
        {
            //Insert directly from the "tempMesh.vertices"
            TextReaderWriter.WriteText(AssetDatabase.GenerateUniqueAssetPath("Assets/DataFiles/generatedMesh_vertices.txt"), tempMesh.vertices.ToList());

            //Convert "tempMesh.triangles" into a Vector3 for inserting into a .txt-file
            List<Vector3> temp = new List<Vector3>();
            for (int i = 0; i < tempMesh.triangles.Length; i += 3)
            {
                temp.Add(new Vector3(tempMesh.triangles[i], tempMesh.triangles[i + 1], tempMesh.triangles[i + 2]));
            }
            TextReaderWriter.WriteText(AssetDatabase.GenerateUniqueAssetPath("Assets/DataFiles/generatedMesh_indices.txt"), temp);
        }

        //Make a pointList from the heights of the regulated mesh
        hightPointList = new int[gridSize * gridSize];

        for (int i = 0; i < tempMesh.vertices.Length; i++)
        {
            if (float.IsNaN(tempMesh.vertices[i].y))
            {
                print("y is NAN");
            }

            hightPointList[i] = (int)tempMesh.vertices[i].y;
        }
    }
  
    List<Vector3> GetVertices(List<Vector3> pos, int size)
    {
        //Set the min and max values to the first in the "vertex_positions"_List
        x_min = pos[0].x;
        float xmax = x_min;

        y_min = pos[0].y;
        float ymax = y_min;

        z_min = pos[0].z;

        //Compare and find the true min and max values in the "vertices_PointCloud"_List
        for (int i = 0; i < pos.Count; i++)
        {
            if (pos[i] == Vector3.zero)
                continue;

            if (pos[i].x < x_min)
            {
                x_min = pos[i].x;
            }
            if (pos[i].x > xmax)
            {
                xmax = pos[i].x;
            }
            if (pos[i].y < y_min)
            {
                y_min = pos[i].y;
            }
            if (pos[i].y > ymax)
            {
                ymax = pos[i].y;
            }
            if (pos[i].z < z_min)
            {
                z_min = pos[i].z;
            }
        }

        //Set width and height of the MeshObject, based on the resolution
        float width = xmax - x_min;
        float height = ymax - y_min;

        meshVertex_width = width / size;
        meshVertex_height = height / size;

        //Make a 2D Array for sorting points
        List<List<List<Vector3>>> gridPositions = new List<List<List<Vector3>>>();
        for (int x = 0; x < size; x++)
        {
            gridPositions.Add(new List<List<Vector3>>());
            for (int y = 0; y < size; y++)
            {
                gridPositions[x].Add(new List<Vector3>());
            }
        }

        //Sort all points into the correct positions in the 2D List
        for (int i = 0; i < pos.Count; i++)
        {
            int x = (int)FindPoint(pos[i].x, x_min, xmax + 1, 0, size);
            int y = (int)FindPoint(pos[i].y, y_min, ymax + 1, 0, size);

            //Add the point to the 2D List
            if (pos[i] != Vector3.zero)
            {
                gridPositions[x][y].Add(pos[i]);
            }
        }

        //Set the Mesh's Vertices
        List<Vector3> mesh_vertices = new List<Vector3>();
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float z = 0f;

                for (int i = 0; i < gridPositions[x][y].Count; i++)
                {
                    z += gridPositions[x][y][i].z;
                }

                if (gridPositions[x][y].Count > 0)
                {
                    mesh_vertices.Add(new Vector3((meshVertex_width / 2) + (meshVertex_width * x), z / gridPositions[x][y].Count - z_min, (meshVertex_height / 2) + (meshVertex_height * y)));
                }
                else
                {
                    mesh_vertices.Add(new Vector3((meshVertex_width / 2) + (meshVertex_width * x), 0f, (meshVertex_height / 2) + (meshVertex_height * y)));
                }
            }
        }

        //Set min and max for the regulated mesh's vertices
        meshVertices_min = new Vector2(meshVertex_width / 2, meshVertex_width / 2);
        meshVertices_max = new Vector2((meshVertex_width / 2) + (meshVertex_width * size), (meshVertex_width / 2) + (meshVertex_width * size));
        
        return mesh_vertices;
    }
    public static float FindPoint(float pos, float min, float max, float zero, float size)
    {
        return (pos - min) / (max - min) * (size - zero) + zero;
    }
    List<int> GetIndices(int size)
    {
        List<int> indexList = new List<int>();

        //Add the indexes of the indices for both triangles in a square
        for (int x = 0; x < size - 1; x++)
        {
            for (int y = 0; y < size - 1; y++)
            {
                indexList.Add(x + (y * size));
                indexList.Add(x + (y * size) + 1);
                indexList.Add(x + (y * size) + size);

                indexList.Add(x + (y * size) + 1);
                indexList.Add(x + (y * size) + size + 1);
                indexList.Add(x + (y * size) + size);
            }
        }

        return indexList;
    }


    //--------------------


    public int FindDropletPosition(float x, float z)
    {
        for (int i = 0; i < vertices_PointCloud.Count; i++)
        {
            //Find the droplet position in the 2D grid / Mesh
            if (vertices_PointCloud[i].x <= x && (vertices_PointCloud[i].x + meshVertex_width) > x && vertices_PointCloud[i].z <= z && (vertices_PointCloud[i].z + meshVertex_height) > z)
            {
                return i;
            }
        }

        //If not on the grid, return negative to despawn it
        return -1;
    }

    public Vector3 GetCollissionPoint(Vector3 pos, int index, float radius)
    {
        Vector2 a = new Vector2();
        Vector2 b = new Vector2();
        Vector2 c = new Vector2();

        //Make a 2D position of the Droplet's world position
        Vector2 position = new Vector2(pos.x, pos.z);

        //Find the points making up the square the Droplet is on, on the grid
        List<Vector3> square = FindSquare(index);

        //Get the vectors of each corner-length of the first triangle in the square and its Barycentric coordinates
        a = new Vector2(square[1].x, square[1].z);
        b = new Vector2(square[2].x, square[2].z);
        c = new Vector2(square[3].x, square[3].z);
        Vector3 barycentric_first = Barycentric(position, b, c, a);

        //Get the vectors of each corner-length of the second triangle in the square and its Barycentric coordinates
        a = new Vector2(square[0].x, square[0].z);
        b = new Vector2(square[2].x, square[2].z);
        c = new Vector2(square[1].x, square[1].z);
        Vector3 barycentric_second = Barycentric(position, b, c, a);

        //Check if the Barysentric coordinates gives a position in the first triangle in the square
        if (barycentric_first.x >= 0f && barycentric_first.x <= 1f && barycentric_first.y >= 0f && barycentric_first.y <= 1f && barycentric_first.z >= 0f && barycentric_first.z <= 1f)
        {
            float y = (barycentric_first.x * square[1].y) + (barycentric_first.y * square[2].y) + (barycentric_first.z * square[3].y);
            float length = Mathf.Abs(pos.y - y);

            if (length < radius)
            {
                Vector3 ab = square[3] - square[1];
                Vector3 ac = square[2] - square[1];

                Vector3 hit = Vector3.Cross(ac, ab);
                hit = hit.normalized;

                return hit;
            }
        }

        //Check if the Barysentric coordinates gives a position in the second triangle in the square
        else if (barycentric_second.x >= 0f && barycentric_second.x <= 1f && barycentric_second.y >= 0f && barycentric_second.y <= 1f && barycentric_second.z >= 0f && barycentric_second.z <= 1f)
        {
            float y = (barycentric_second.x * square[0].y) + (barycentric_second.y * square[2].y) + (barycentric_second.z * square[1].y);
            float length = Mathf.Abs(pos.y - y);

            if (length < radius)
            {
                Vector3 ab = square[1] - square[0];
                Vector3 ac = square[2] - square[0];

                Vector3 hit = Vector3.Cross(ac, ab);
                hit = hit.normalized;

                return hit;
            }
        }

        //If the Droplet position isn't inside any triangle, return negative to despawn it 
        return Vector3.one * -1f;
    }

    Vector3 Barycentric(Vector2 pos, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        Vector3 abc = CrossProduct(ab, ac);

        float normal = abc.magnitude;

        Vector2 pos_a = a - pos;
        Vector2 pos_b = b - pos;
        Vector2 pos_c = c - pos;

        float x = CrossProduct(pos_a, pos_b).z / normal;
        float y = CrossProduct(pos_b, pos_c).z / normal;
        float z = CrossProduct(pos_c, pos_a).z / normal;

        return new Vector3(x, y, z);
    }
    Vector3 CrossProduct(Vector2 a, Vector2 b)
    {
        Vector3 crossProduct = new Vector3();

        crossProduct.z = (a.x * b.y) - (a.y * b.x);

        return crossProduct;
    }

    public List<Vector3> FindSquare(int index)
    {
        //Find Droplet square, checking the points adjacent to the Droplet index position on the grid
        List<Vector3> square = new List<Vector3>()
        {
            vertices_PointCloud[index],
            vertices_PointCloud[index + 1],
            vertices_PointCloud[index + gridSize],
            vertices_PointCloud[index + gridSize + 1]
        };

        return square;
    }


    //--------------------


    void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        //Draw squares on each "vertices_PointCloud[i]"-point
        for (var i = 0; i < vertices_PointCloud.Count; i += 3)
        {
            //Color
            Gizmos.color = gizmoColor;

            //Cube
            Gizmos.DrawCube
                (
                    new Vector3
                    (
                        vertices_PointCloud[i].x, 
                        vertices_PointCloud[i].y, 
                        vertices_PointCloud[i].z
                    ),

                    new Vector3(gizmoSize, gizmoSize, gizmoSize)
                );
        }
    }
}