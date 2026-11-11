using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Chunk : MonoBehaviour
{
    public Vector3Int coord;
    public bool[] shouldGenerateToSides;

	public List<MarchingCubes.Deformation> deformations;

    [HideInInspector]
    public Mesh mesh;
	[HideInInspector]
	public MeshCollider meshCollider;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    Dictionary<int2, int> vertexIndexMap;
    List<Vector3> processedVertices;
    //List<Vector3> processedNormals;
    List<int> processedTriangles;


	public Chunk(Vector3Int inCoord)
    {
        coord = inCoord;
		deformations = new List<MarchingCubes.Deformation>();
	}

    public void SetUp(Material terrainMaterial)
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;
		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh = mesh;
		meshCollider = meshRenderer.gameObject.AddComponent<MeshCollider>();
	}

	public void CreateMesh(VertexData[] vertexData, int numVertices, bool useFlatShading)
	{
		Vector3[] vertices = new Vector3[numVertices];
		int[] triangles = new int[numVertices];
		float[] vertTerrainTypes = new float[numVertices];

		int texWidth = Mathf.FloorToInt(Mathf.Sqrt(numVertices));

		//Texture2D terrainTypeTex = new Texture2D(texWidth, texWidth, TextureFormat.RFloat, false);
		//Color[] terrainTypeColors = new Color[numVertices];


		Vector2[] uvs = new Vector2[numVertices];


		for (int i = 0; i < numVertices; i++)
		{ 
			vertices[i] = vertexData[i].position;
			triangles[i] = i;
			vertTerrainTypes[i] = vertexData[i].terrainType;

			//if (i == 0 && vertexData[i].terrainType > 1) Debug.Log(vertexData[i].terrainType);

			uvs[i] = new Vector2(0f, vertexData[i].terrainType);

			//if (vertexData[i].terrainType < 0.4f)
            //{
			//	uvs[i] = new Vector2(0f, 0f);
			//}
            //else if (vertexData[i].terrainType < 0.98f)
            //{
			//	uvs[i] = new Vector2(0f, vertexData[i].terrainType * vertexData[i].terrainType);
			//}
            //else
            //{
			//
			//}

			//terrainTypeColors[i] = new Color(vertTerrainTypes[i], 0, 0);


			//if (vertTerrainTypes[i] >= 1.401298E-39f)
			//{
			//}
		}

		//terrainTypeTex.SetPixels(terrainTypeColors);
		//terrainTypeTex.Apply();

		//meshRenderer.material.SetTexture("_TerrainTypeTex", terrainTypeTex);

		//vertexIndexMap = new Dictionary<int2, int>();
		//processedVertices = new List<Vector3>();
		////processedNormals = new List<Vector3>();
		//processedTriangles = new List<int>();
		//
		//vertexIndexMap.Clear();
		//processedVertices.Clear();
		////processedNormals.Clear();
		//processedTriangles.Clear();

		//int triangleIndex = 0;
		//
		//for (int i = 0; i < numVertices; i++)
		//{
		//	VertexData data = vertexData[i];
		//
		//	int sharedVertexIndex;
		//	if (!useFlatShading && vertexIndexMap.TryGetValue(data.id, out sharedVertexIndex))
		//	{
		//		processedTriangles.Add(sharedVertexIndex);
		//	}
		//	else
		//	{
		//		if (!useFlatShading)
		//		{
		//			vertexIndexMap.Add(data.id, triangleIndex);
		//		}
		//		processedVertices.Add(data.position);
		//		//processedNormals.Add(data.normal);
		//		processedTriangles.Add(triangleIndex);
		//		triangleIndex++;
		//	}
		//}

		


		meshCollider.sharedMesh = null;

		mesh.Clear();
		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0, true);
		mesh.SetUVs(0, uvs);


		mesh.RecalculateNormals();

		//if (useFlatShading)
		//{
		//	mesh.RecalculateNormals();
		//}
		//else
		//{
		//	//mesh.SetNormals(processedNormals);
		//
		//	mesh.RecalculateNormals();
		//}

		meshCollider.sharedMesh = mesh;
		meshCollider.enabled = true;

		//ComputeBuffer terrainTypeBuffer = new ComputeBuffer(numVertices, sizeof(float));
		//
		//terrainTypeBuffer.SetData(vertTerrainTypes, 0, 0, numVertices);

		//meshRenderer.material.SetBuffer("terrainTypes", terrainTypeBuffer);
	}

	public override bool Equals(object obj)
    {
        if (obj is Chunk other)
        {
            return coord == other.coord;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return coord.GetHashCode();
    }
}
