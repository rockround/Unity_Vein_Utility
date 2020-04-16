using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexAnalyzer : MonoBehaviour
{
    public MeshFilter mf;
    public int polyVertices = 15;

    // Start xis called before the first frame update
    void Start()
    {
        Mesh mesh = mf.mesh;
        int vertexCount = mesh.vertexCount;
        print(vertexCount);
        Color[] colors = new Color[vertexCount];
        for(int i = 0; i < 120; i++)
        {
            colors[0] = Color.black;
            colors[mesh.vertexCount-1-i] = Color.black;

        }

        int numLayers = (vertexCount - 242) / polyVertices;

        for (int i = 120; i < 750; i++)
        {
            float con = ((float)(i / 15)) / (numLayers);
            //print(con);
            colors[i] =con * Color.white;
        }
        mesh.colors = colors;

    }

    // Update is called once per frame
    void Update()
    {

    }
}
