using System;
using System.Collections;
using System.Collections.Generic;
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
    Mesh currentMesh;

    // Start is called before the first frame update
    void Start()
    {
        //ringPoss = new List<List<Vector3>>();
        //ringNorms = new List<List<Vector3>>();
        positions = new List<Vector3>();
        triangles = new List<int>();
        normals = new List<Vector3>();
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
                        startNorm = hit.normal;

                    }
                    else if (points == 2)
                    {
                        end = hit.point;
                        endNorm = -hit.normal;
                        Tuple<List<Vector3>, List<Vector3>> pointData = generatePoints(start, end, startNorm, endNorm);
                        List<Vector3> points = pointData.Item1;
                        List<Vector3> directions = pointData.Item2;

                        for (int i = 0; i < points.Count; i++)
                        {
                            lr.positionCount += 1;
                            Tuple<List<Vector3>, List<Vector3>> pointSet = generateRing(ringVertices, radius, directions[i], points[i]);
                            //ringPoss.Add(pointSet.Item1);
                            //ringNorms.Add(pointSet.Item2);
                            positions.AddRange(pointSet.Item1);
                            normals.AddRange(pointSet.Item2);
                            
                            //point is centroid of tube
                            lr.SetPosition(i, points[i]);
                        }
                        int layers = points.Count;
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
                                triangles.Add(b);
                                triangles.Add(d);
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
                                triangles.Add(a);
                                triangles.Add(d);
                            }
                        }
                        currentMesh = new Mesh();
                        currentMesh.SetVertices(positions);
                        currentMesh.SetTriangles(triangles, 0);
                        currentMesh.SetNormals(normals);
                        selection = false;
                    }
                }
            }
            else
            {
                //ringPoss.Clear();
                //ringNorms.Clear();
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
                for(int i = 0; i< triangles.Count; i+=3)
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
        for (float t = 0; t <= 1 + granularity; t += granularity)
        {
            Vector3 point = (2 * t * t * t - 3 * t * t + 1) * start + (t * t * t - 2 * t * t + t) * startNorm + (-2 * t * t * t + 3 * t * t) * end + (t * t * t - t * t) * endNorm;
            points.Add(point);
            if (t > 0)
            {
                directions.Add((point - prevPoint).normalized);
            }
            prevPoint = point;
        }
        directions.Add(endNorm);
        return new Tuple<List<Vector3>, List<Vector3>>(points, directions);
    }
}
