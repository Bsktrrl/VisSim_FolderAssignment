using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class LasToVertex : MonoBehaviour
{
    //Classes
    #region
    public class MeshVertex
    {
        public Vector3 pos = Vector3.zero;
        public Vector3 normal = Vector3.zero;
        public Vector3 UV = Vector3.zero;
    }
    public class ColorVertex
    {
        public Vector3 pos = Vector3.zero;
        public Vector3 color = Vector3.zero;
    };
    public class ColorNormalVertex
    {
        public Vector3 pos = Vector3.zero;
        public Vector3 color = Vector3.zero;
        public Vector3 normal = Vector3.zero;
    }
    public class Triangle
    {
        public Vector3 A = Vector3.zero;
        public Vector3 B = Vector3.zero;
        public Vector3 C = Vector3.zero;
        public Vector3 N = Vector3.zero;
    }

    public class HeightAndColor
    {
        public int count = 0;
        public float sum = 0f;
        public Vector3 color = Vector3.zero;
    }

    public class lasHeader
    {
        public char fileSignature;

        public int sourceID;
        public int globalEncoding;
        public int GUID1;
        public int GUID2;
        public int GUID3;
        public int GUID4;
        public int versionMajor;
        public int versionMinor;

        public char systemIdentifier;
        public char generatingSoftware;

        public int creationDay;
        public int creationYear;
        public int headerSize;
        public int offsetToPointData;
        public int numberVariableLengthRecords;
        public int pointDataRecordFormat;
        public int pointDataRecordLength;
        public int legacyNumberPointsRecords;
        public int legacyNumberPointReturn;

        public double xScaleFactor, yScaleFactor, zScaleFactor;
        public double xOffset, yOffset, zOffset;
        public double maxX, minX;
        public double maxY, minY;
        public double maxZ, minZ;
    };
    public class lasVariableLengthRecords
    {
        public int lasReserved;
        public char UserID;
        public int recordID;
        public int recordLengthAfterHeader;
        public char lasDescription;
    };
    public class lasPointData1
    {
        public int xPos;
        public int yPos;
        public int zPos;
        public int intensity;
        public int flags;
        public int classificaton;
        public int scanAngle;
        public int userData;
        public int pointSourceID;
        public double GPSTime;
    };
    public class lasPointData2
    {
        int xPos;
        int yPos;
        int zPos;
        int intensity;
        int flags;
        int classificaton;
        int scanAngle;
        int userData;
        int pointSourceID;
        int red;
        int green;
        int blue;
    };

    public class Pair_int
    {
        public int a_i;
        public int b_i;
    }
    public class Pair_triangle
    {
        public Triangle a_t;
        public Triangle b_t;
    }

    #endregion

    List<ColorVertex> pointData;
    List<MeshVertex> vertexData;
    List<ColorNormalVertex> colorNormalVertexData;
    List<int> indexData;
    List<MeshVertex> triangulatedVertexData;
    List<Triangle> triangles;

    Vector3 min = Vector3.zero;
    Vector3 max = Vector3.zero;
    Vector3 middle = Vector3.zero;
    Vector3 offset = Vector3.zero;
    float xSquares = 0f;
    float zSquares = 0f;


    //--------------------


    void FindMinMax()
    {
        //Check if Min/Max exist
        if (max != Vector3.zero)
        {
            return;
        }

        //Set Min & Max values
        min = Vector3.positiveInfinity;
        max = Vector3.positiveInfinity;

        foreach (var vertex in pointData)
        {
            if (min.x > vertex.pos.x)
            {
                min.x = vertex.pos.x; 
            }
            else if (max.x < vertex.pos.x)
            { 
                max.x = vertex.pos.x;
            }
            
            if (min.z > vertex.pos.z)
            {
                min.z = vertex.pos.z;
            }
            else if (max.z < vertex.pos.z)
            {
                max.z = vertex.pos.z;
            }
            
            if (min.y > vertex.pos.y) 
            {
                min.y = vertex.pos.y; 
            }
            else if (max.y < vertex.pos.y) 
            {
                max.y = vertex.pos.y; 
            }
        }
    }
    void CalcCenter()
    {
        FindMinMax();

        //FInd Middle
        middle = min + (max - min) / 2;
        offset = min;

        //Update MIn/Max
        min -= offset;
        max -= offset;
    }
    void UpdatePoints()
    {
        foreach (var vertex in pointData)
        {
            if (middle != Vector3.zero)
            {
                vertex.pos -= offset;
            }
        }
    }


    //--------------------


    void Triangulate()
    {
        //Set Width and Height
        xSquares = (max.x - min.x);
        zSquares = (max.z - min.z);

        //Save all height data for each vertex
        HeightAndColor[,] heightmap = new HeightAndColor[(int)zSquares, (int)xSquares];

        foreach (var vertex in pointData)
        {
            int xPos = (int)vertex.pos.x;
            int zPos = (int)vertex.pos.z;

            if (xPos < 0f || xPos > xSquares - 1
                || zPos < 0f || zPos > zSquares - 1)
            {
                continue;
            }

            heightmap[zPos, xPos].count++;
            heightmap[zPos, xPos].sum += vertex.pos.y;
            heightmap[zPos, xPos].color += vertex.color;
        }

        List<Pair_int> noHeight = new List<Pair_int>();

        // Calculate average height for each vertex and push
        for (int z = 0; z < zSquares; ++z)
        {
            for (int x = 0; x < xSquares; ++x)
            {
                float y;
                Vector3 color = new Vector3();
                Pair_int pairTemp = new Pair_int();

                if (heightmap[z, x].count == 0)
                {
                    pairTemp.a_i = x;
                    pairTemp.b_i = z;

                    y = -max.y;
                    noHeight.Add(pairTemp);
                    color = new Vector3(1, 1, 1);
                }
                else
                {
                    //y = (average / count) - max.y;
                    y = heightmap[z, x].sum / heightmap[z, x].count - max.y;
                    color = heightmap[z, x].color / heightmap[z, x].count;
                }

                MeshVertex temp = new MeshVertex();
                ColorNormalVertex temp2 = new ColorNormalVertex();
                temp.pos = new Vector3(x, y, z);
                temp2.pos = temp.pos;
                temp2.color = color;
                vertexData.Add(temp);
                colorNormalVertexData.Add(temp2);
            }
        }

        // Calculate average height if no height
        foreach (var vertex in noHeight)
        {
            int x = vertex.a_i;
            int z = vertex.b_i;

            if (x <= 0 || x >= xSquares - 1 || z <= 0 || z >= zSquares - 1)
            {
                continue;
            }
            else
            {
                float averageHeight = 0;
                averageHeight += vertexData[(int)((x - 1) + (z * xSquares))].pos.y;
                averageHeight += vertexData[(int)((x - 1) + ((z - 1) * xSquares))].pos.y;
                averageHeight += vertexData[(int)(x + ((z - 1) * xSquares))].pos.y;
                averageHeight += vertexData[(int)((x + 1) + ((z - 1) * xSquares))].pos.y;
                averageHeight += vertexData[(int)((x + 1) + (z * xSquares))].pos.y;
                averageHeight += vertexData[(int)((x + 1) + ((z + 1) * xSquares))].pos.y;
                averageHeight += vertexData[(int)(x + ((z + 1) * xSquares))].pos.y;
                averageHeight += vertexData[(int)((x - 1) + ((z + 1) * xSquares))].pos.y;

                Vector3 averageColor = new Vector3();
                averageColor += colorNormalVertexData[(int)((x - 1) + (z * xSquares))].color;
                averageColor += colorNormalVertexData[(int)((x - 1) + ((z - 1) * xSquares))].color;
                averageColor += colorNormalVertexData[(int)(x + ((z - 1) * xSquares))].color;
                averageColor += colorNormalVertexData[(int)((x + 1) + ((z - 1) * xSquares))].color;
                averageColor += colorNormalVertexData[(int)((x + 1) + (z * xSquares))].color;
                averageColor += colorNormalVertexData[(int)((x + 1) + ((z + 1) * xSquares))].color;
                averageColor += colorNormalVertexData[(int)(x + ((z + 1) * xSquares))].color;
                averageColor += colorNormalVertexData[(int)((x - 1) + ((z + 1) * xSquares))].color;

                vertexData[(int)(x + (z * xSquares))].pos.y = averageHeight / 8f;
                colorNormalVertexData[(int)(x + (z * xSquares))].pos.y = averageHeight / 8f;
                colorNormalVertexData[(int)(x + (z * xSquares))].color = averageColor / 8f;
            }
        }

        // Create Index
        for (int z = 0; z < zSquares - 1; ++z)
        {
            for (int x = 0; x < xSquares - 1; ++x)
            {
                indexData.Add((int)(x + (xSquares * z)));
                indexData.Add((int)(x + 1 + (xSquares * (z + 1))));
                indexData.Add((int)(x + 1 + (xSquares * z)));

                indexData.Add((int)(x + (xSquares * z)));
                indexData.Add((int)(x + (xSquares * (z + 1))));
                indexData.Add((int)(x + 1 + (xSquares * (z + 1))));
            }
        }

        // Calculate smooth normals
        int xmin = 0, xmax = (int)xSquares, zmin = 0, zmax = (int)zSquares; // The size to draw

        // Normals for rest
        for (int z = zmin; z < zmax; z++)
        {
            for (int x = xmin; x < xmax; x++)
            {
                if (z == zmin || z == zmax - 1 || x == xmin || x == xmax - 1)
                {
                    vertexData[x + (xmax * z)].normal = new Vector3(0f, 1f, 0f);
                }
                else
                {
                    Vector3 a = vertexData[x + (xmax * z)].pos;
                    Vector3 b = vertexData[x + 1 + (xmax * z)].pos;
                    Vector3 c = vertexData[x + 1 + (xmax * (z + 1))].pos;
                    Vector3 d = vertexData[x + (xmax * (z + 1))].pos;
                    Vector3 e = vertexData[x - 1 + (xmax * z)].pos;
                    Vector3 f = vertexData[x - 1 + (xmax * (z - 1))].pos;
                    Vector3 g = vertexData[x + (xmax * (z - 1))].pos;

                    var n0 = Vector3.Cross(c - a, b - a);
                    var n1 = Vector3.Cross(d - a, c - a);
                    var n2 = Vector3.Cross(e - a, d - a);
                    var n3 = Vector3.Cross(f - a, e - a);
                    var n4 = Vector3.Cross(g - a, f - a);
                    var n5 = Vector3.Cross(b - a, g - a);

                    Vector3 normal = n0 + n1 + n2 + n3 + n4 + n5;
                    normal = normal.normalized;

                    vertexData[x + (xmax * z)].normal = normal;
                    colorNormalVertexData[x + (xmax * z)].normal = normal;
                }
            }
        }
    }


    //--------------------


    List<MeshVertex> GetVertexData()
    {
        List<MeshVertex> _out = new List<MeshVertex>();
        int i = 0;
        while (i != indexData.Count)
        {
            MeshVertex tempA;
            tempA = vertexData[indexData[i]];
            i++;

            MeshVertex tempB;
            tempB = vertexData[indexData[i]];
            i++;

            MeshVertex tempC;
            tempC = vertexData[indexData[i]];
            i++;

            var ab = tempB.pos - tempA.pos;
            var ac = tempC.pos - tempA.pos;

            Vector3 normal = Vector3.Cross(ab, ac).normalized;
            tempA.normal = normal;
            tempB.normal = normal;
            tempC.normal = normal;

            tempA.UV.x = tempA.pos.x;
            tempA.UV.y = tempA.pos.y;
            tempB.UV.x = tempB.pos.x;
            tempB.UV.y = tempB.pos.y;
            tempC.UV.x = tempC.pos.x;
            tempC.UV.y = tempC.pos.y;

            _out.Add(tempA);
            _out.Add(tempB);
            _out.Add(tempC);
        }

        return _out;
    }

    List<List<Pair_triangle>> GetTarrainData()
    {
        int width = (int)(max.x - min.x);
        int height = (int)(max.z - min.z);

        List<List<Pair_triangle>> _out = new List<List<Pair_triangle>>();

        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                Triangle bottom = new Triangle();
                Triangle top = new Triangle(); ;

                int aIndex = (x + (z * width));
                int bIndex = ((x + 1) + (z * width));
                int cIndex = ((x + 1) + ((z + 1) * width));
                int dIndex = (x + ((z + 1) * width));

                bottom.A = vertexData[aIndex].pos;
                bottom.B = vertexData[cIndex].pos;
                bottom.C = vertexData[bIndex].pos;

                top.A = vertexData[aIndex].pos;
                top.B = vertexData[dIndex].pos;
                top.C = vertexData[cIndex].pos;

                bottom.N = Vector3.Cross(bottom.B - bottom.A, bottom.C - bottom.A).normalized;
                top.N = Vector3.Cross(top.B - top.A, top.C - top.A).normalized;

                _out[z][x].a_t = bottom;
                _out[z][x].b_t = top;
            }
        }

        return _out;
    }
}
