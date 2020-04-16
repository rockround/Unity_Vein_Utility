﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    public LineRenderer lr;
    public float radius = 1;
    public float granularity = .05f;
    public int ringVertices = 6;
    int points = 0;
    Vector3 start, startNorm, end, endNorm;
    bool selection = true;
    //List<List<Vector3>> ringPoss;
    //List<List<Vector3>> ringNorms;
    List<Vector3> positions;
    List<Vector3> normals;
    List<int> triangles;
    List<Vector2> uvs;
    Mesh currentMesh;
    public MeshFilter mf;
    public MeshRenderer mr;
    public bool saveAsset;
    // Start is called before the first frame update
    void Start()
    {
        //ringPoss = new List<List<Vector3>>();
        //ringNorms = new List<List<Vector3>>();
        positions = new List<Vector3>();
        triangles = new List<int>();
        normals = new List<Vector3>();
        uvs = new List<Vector2>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selection)
            {

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    Collider col = hit.collider;

                    points += 1;
                    if (points == 1)
                    {
                        start = hit.point;                        
                        print("Start direction: " + hit.normal);
                        startNorm = hit.normal;


                    }
                    else if (points == 2)
                    {
                        end = hit.point;
                        endNorm = -hit.normal;
                        print("End direction: " + -hit.normal);

                        Tuple<List<Vector3>, List<Vector3>> pointData = generatePoints(start, end, startNorm, endNorm);
                        List<Vector3> points = pointData.Item1;
                        List<Vector3> directions = pointData.Item2;
                        List<Color> colors = new List<Color>();

                        int layers = points.Count;

                        float intensityPerColor = 1f / layers;

                        float deltaUVX = 1f / ringVertices;
                        float deltaUVY = 1f / layers;

                        for (int i = 0; i < points.Count; i++)
                        {
                            lr.positionCount += 1;
                            Tuple<List<Vector3>, List<Vector3>> pointSet = generateRing(ringVertices, radius, directions[i], points[i]);
                            //ringPoss.Add(pointSet.Item1);
                            //ringNorms.Add(pointSet.Item2);
                            positions.AddRange(pointSet.Item1);
                            normals.AddRange(pointSet.Item2);

                            //Adds colors corresponding to the layering
                            for (int j = 0; j < ringVertices; j++)
                            {
                                colors.Add(intensityPerColor * i * Color.white);
                                uvs.Add(new Vector2(deltaUVX * j, deltaUVY * i));
                            }

                            //point is centroid of tube
                            lr.SetPosition(i, points[i]);
                        }
                        for (int j = 0; j < layers - 1; j++)
                        {
                            for (int i = 0; i < ringVertices; i++)
                            {
                                int idx1 = i;
                                int idx2 = (i + 1) % ringVertices;
                                int layer1 = j;
                                int layer2 = j + 1;
                                int a = layer1 * ringVertices + idx1;
                                int b = layer1 * ringVertices + idx2;
                                int d = layer2 * ringVertices + idx2;
                                triangles.Add(a);
                                triangles.Add(d);
                                triangles.Add(b);
                            }
                            for (int i = 0; i < ringVertices; i++)
                            {

                                int idx1 = i;
                                int idx2 = (i + 1) % ringVertices;
                                int layer1 = j;
                                int layer2 = j + 1;
                                int a = layer1 * ringVertices + idx1;
                                int c = layer2 * ringVertices + idx1;
                                int d = layer2 * ringVertices + idx2;
                                triangles.Add(c);
                                triangles.Add(d);
                                triangles.Add(a);
                            }
                        }
                        currentMesh = new Mesh();
                        currentMesh.SetVertices(positions);
                        currentMesh.SetTriangles(triangles, 0);
                        currentMesh.SetNormals(normals);
                        currentMesh.SetUVs(0,uvs);
                        currentMesh.SetColors(colors);
                        currentMesh.UploadMeshData(true);

                        mf.mesh = currentMesh;
                        mr.UpdateGIMaterials();
                        if (saveAsset)
                        {
                            AssetDatabase.CreateAsset(currentMesh, @"Assets\Prefabs\mesh.asset");
                            AssetDatabase.SaveAssets();
                        }
                        selection = false;
                    }
                }
            }
            else
            {
                //ringPoss.Clear();
                //ringNorms.Clear();
                uvs.Clear();
                normals.Clear();
                lr.positionCount = 0;
                positions.Clear();
                triangles.Clear();
                selection = true;
                points = 0;
            }
        }
        else
        {
            //if vertices already generated
            if (!selection)
            {
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    Vector3 v1 = positions[triangles[i]];
                    Vector3 v2 = positions[triangles[i + 1]];
                    Vector3 v3 = positions[triangles[i + 2]];
                    Debug.DrawLine(v1, v2, Color.red);
                    Debug.DrawLine(v2, v3, Color.red);
                    Debug.DrawLine(v3, v1, Color.red);
                }
                //Just in case... may not need this
                /*if (ringPoss.Count > 0 && ringNorms.Count > 0)
                {
                    //per ring
                    for (int j = 0; j < ringPoss.Count; j++)
                    {
                        //per vertex per ring
                        for (int i = 0; i < ringVertices; i++)
                        {
                            int idx1 = i;
                            int idx2 = (i + 1) % ringVertices;

                            Debug.DrawLine(ringPoss[j][idx1], ringPoss[j][idx2], Color.red);
                            Debug.DrawLine(ringPoss[j][idx1], ringPoss[j][idx1] + ringNorms[j][idx1], Color.blue);
                        }
                    }
                }*/
            }
        }
    }
    //Returns points and normals of ring points
    public Tuple<List<Vector3>, List<Vector3>> generateRing(int vertexCount, float radius, Vector3 planeNormal, Vector3 planeCenter)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        float dTheta = 2 * Mathf.PI / vertexCount;
        for (int i = 0; i < vertexCount; i++)
        {
            float x = Mathf.Cos(i * dTheta);
            float z = Mathf.Sin(i * dTheta);
            Vector3 rawPoint = new Vector3(x, 0, z);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, planeNormal);
            Vector3 newPoint = rot * rawPoint;
            normals.Add(newPoint);
            //Scale to point location
            newPoint *= radius;
            points.Add(newPoint + planeCenter);
        }
        return new Tuple<List<Vector3>, List<Vector3>>(points, normals);
    }
    Tuple<List<Vector3>, List<Vector3>> generatePoints(Vector3 start, Vector3 end, Vector3 startNorm, Vector3 endNorm)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> directions = new List<Vector3>();
        Vector3 prevPoint = Vector3.zero;
        int iterations = Mathf.RoundToInt(1f / granularity);
        //directions.Add(startNorm);
        for (int i= 0; i <= iterations; i ++)
        {
            float t = i * granularity; 
            Vector3 point = (2 * t * t * t - 3 * t * t + 1) * start + (t * t * t - 2 * t * t + t) * startNorm + (-2 * t * t * t + 3 * t * t) * end + (t * t * t - t * t) * endNorm;
            Vector3 direction = (6 * t * t - 6 * t) * start + (3 * t * t - 4 * t + 1) * startNorm + (-6 * t * t + 6 * t) * end + (3 * t * t - 2 * t) * endNorm;
            points.Add(point);
            directions.Add(direction);
            /*if (t > 0)
            {
                directions.Add((point - prevPoint).normalized);
            }
            prevPoint = point;*/
        }
        //directions.RemoveAt(points.Count - 1);
        //directions.Add(endNorm);
        return new Tuple<List<Vector3>, List<Vector3>>(points, directions);
    }
}