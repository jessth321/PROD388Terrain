using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesCPU : MonoBehaviour
{
	public Material terrainMaterial;
	public int size = 20;
	public float noiseFreq = 0.1f;
	[Range(-1f, 1f)]
	public float noiseThickness = 0f;
	public float groundLevel;
	public float groundBlendDistance;
	public float maxTerrainHeight;
	public float maxTerrainHeightBlend;

	List<float> voxelValues = new List<float>();
	List<Vector3> points = new List<Vector3>();
	List<Cube> voxelCubes = new List<Cube>();
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();

	class Cube
	{
		public Vector3[] points = new Vector3[8];
		public float[] values = new float[8];
	}

	int[][] trianglulationTable = new int[][] {
		new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
		new int[] { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
		new int[] { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
		new int[] { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
		new int[] { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
		new int[] { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
		new int[] { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
		new int[] { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
		new int[] { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
		new int[] { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
		new int[] { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
		new int[] { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
		new int[] { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
		new int[] { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
		new int[] { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
		new int[] { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
		new int[] { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
		new int[] { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
		new int[] { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
		new int[] { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
		new int[] { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
		new int[] { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
		new int[] { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
		new int[] { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
		new int[] { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
		new int[] { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
		new int[] { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
		new int[] { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
		new int[] { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
		new int[] { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
		new int[] { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
		new int[] { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
		new int[] { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
		new int[] { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
		new int[] { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
		new int[] { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
		new int[] { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
		new int[] { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
		new int[] { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
		new int[] { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
		new int[] { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
		new int[] { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
		new int[] { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
		new int[] { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
		new int[] { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
		new int[] { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
		new int[] { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
		new int[] { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
		new int[] { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
		new int[] { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
		new int[] { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
		new int[] { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
		new int[] { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
		new int[] { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
		new int[] { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
		new int[] { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
		new int[] { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
		new int[] { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
		new int[] { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
		new int[] { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
		new int[] { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
		new int[] { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
		new int[] { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
		new int[] { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
		new int[] { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
		new int[] { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
		new int[] { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
		new int[] { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
		new int[] { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
		new int[] { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
		new int[] { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
		new int[] { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
		new int[] { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
		new int[] { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
		new int[] { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
		new int[] { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
		new int[] { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
		new int[] { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
		new int[] { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
		new int[] { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
		new int[] { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
		new int[] { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
		new int[] { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
		new int[] { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
		new int[] { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
		new int[] { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
		new int[] { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
		new int[] { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
		new int[] { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
		new int[] { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
		new int[] { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
		new int[] { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
		new int[] { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
		new int[] { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
		new int[] { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
		new int[] { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
		new int[] { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
		new int[] { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
		new int[] { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
		new int[] { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
		new int[] { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
		new int[] { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
		new int[] { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
		new int[] { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
		new int[] { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
		new int[] { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
		new int[] { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
		new int[] { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
	};
	int[][] edgeVertexIndices = new int[][] {
		new int[] {0, 1},
		new int[] {1, 2},
		new int[] {2, 3},
		new int[] {3, 0},
		new int[] {4, 5},
		new int[] {5, 6},
		new int[] {6, 7},
		new int[] {7, 4},
		new int[] {0, 4},
		new int[] {1, 5},
		new int[] {2, 6},
		new int[] {3, 7}
};

	// Start is called before the first frame update
	void Start()
	{

		Vector3 seedOffset = new Vector3(Random.Range(0, 999999), Random.Range(0, 999999), Random.Range(0, 999999));

		for (int i = 0; i < size * size * size; i++)
		{
			voxelValues.Add(PerlinNoise3D.get3DPerlinNoise(new Vector3(i % size, i / (size * size), i / size % size) + seedOffset, noiseFreq) + noiseThickness);
			/*voxelValues.Add(Mathf.Lerp(
				PerlinNoise3D.get3DPerlinNoise(new Vector3(i % size, i / (size * size), i / size % size) + seedOffset, noiseFreq) + noiseThickness,
				-1f,
				(float)i / (size * size * size)));*/
			points.Add(new Vector3(i % size, i / (size * size), i / size % size));
			Vector3 voxelPoint = points[points.Count - 1];
			float voxelValue = voxelValues[voxelValues.Count - 1];
			if (voxelPoint.y < groundLevel)
			{
				voxelValues[voxelValues.Count - 1] = 1;
			}
			else if (voxelPoint.y < groundLevel + groundBlendDistance)
			{
				float difference = groundLevel + groundBlendDistance - voxelPoint.y;
				voxelValues[voxelValues.Count - 1] = Mathf.Clamp(voxelValue + (difference / groundBlendDistance) * (difference / groundBlendDistance), -1, 1);
			}
			else if (voxelPoint.y > maxTerrainHeight - maxTerrainHeightBlend)
			{
				float difference = voxelPoint.y - maxTerrainHeight + maxTerrainHeightBlend;
				voxelValues[voxelValues.Count - 1] = Mathf.Clamp(voxelValue - (difference / maxTerrainHeightBlend) * (difference / maxTerrainHeightBlend), -1, 1);
			}
		}

		for (int i = 0; i < (size) * (size) * (size); i++)
		{
			if (i % size != size - 1 && i / size % size != size - 1 && i / (size * size) % size != size - 1)
			{
				Cube cube = new Cube();
				cube.points = new Vector3[] {
					points[i],
					points[i + 1],
					points[i + size + 1],
					points[i + size],
					points[i + (size * size)],
					points[i + (size * size) + 1],
					points[i + (size * size) + size + 1],
					points[i + (size * size) + size]
				};
				cube.values = new float[] {
					voxelValues[i],
					voxelValues[i + 1],
					voxelValues[i + size + 1],
					voxelValues[i + size],
					voxelValues[i + (size * size)],
					voxelValues[i + (size * size) + 1],
					voxelValues[i + (size * size) + size + 1],
					voxelValues[i + (size * size) + size]
				};
				voxelCubes.Add(cube);
			}
		}

		//for (int i = 0; i < size * size * (size - 1) - size - 1; i++)
		foreach (Cube cube in voxelCubes)
		{
			/*int[] cubeIndices = new int[] {
				i,
				i + 1,
				i + size,
				i + size + 1,
				i + (size * size),
				i + (size * size) + 1,
				i + (size * size) + size,
				i + (size * size) + size + 1
			};

			Vector3[] cubePositions = new Vector3[] {
				points[cubeIndices[0]],
				points[cubeIndices[1]],
				points[cubeIndices[2]],
				points[cubeIndices[3]],
				points[cubeIndices[4]],
				points[cubeIndices[5]],
				points[cubeIndices[6]],
				points[cubeIndices[7]]
			};

			float[] cubeValues = new float[] {
				voxelValues[cubeIndices[0]],
				voxelValues[cubeIndices[1]],
				voxelValues[cubeIndices[2]],
				voxelValues[cubeIndices[3]],
				voxelValues[cubeIndices[4]],
				voxelValues[cubeIndices[5]],
				voxelValues[cubeIndices[6]],
				voxelValues[cubeIndices[7]], 
			};*/

			int cubeIndex = 0;
			for (int j = 0; j < 8; j++)
			{
				if (cube.values[j] < 0f)
				{
					cubeIndex |= 1 << j;
				}
			}

			int[] triangulation = trianglulationTable[cubeIndex];

			foreach (int edgeIndex in triangulation)
			{
				if (edgeIndex == -1) break;

				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				//vertexPos = (cube.points[indexA] + (cube.points[indexB])) / 2;
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);

			}
		}


		//DebugHoledCubeMeshes();



		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = vertices.ToArray();

		for (int i = 0; i < triangles.Count - 2; i += 3)
		{
			int swap = triangles[i];
			triangles[i] = triangles[i + 2];
			triangles[i + 2] = swap;
		}

		mesh.triangles = triangles.ToArray();



		mesh.RecalculateNormals();


		//mesh.RecalculateNormals();

		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

		// Create a new mesh filter, and assign the mesh from before to it
		meshFilter.mesh = mesh;

		// Create a new renderer for our mesh, and use the standard shader
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = terrainMaterial;
	}

	/*private void OnDrawGizmos()
    {
		for (int i = 0; i < vertices.Count; i++)
		{
			Gizmos.DrawSphere(vertices[i], 0.05f);
		}
		for (int i = 0; i < points.Count; i++)
		{
			Gizmos.DrawSphere(points[i], 0.1f);
		}
	}*/

	// Update is called once per frame
	void Update()
	{

	}

	/*int[] GetCubeCornerIndices(int pointIndex)
	{
		int[] indexList = new int[] {
			pointIndex,
			pointIndex + 1,
			pointIndex + size,
			pointIndex + size + 1,
			pointIndex + (size * size),
			pointIndex + (size * size) + 1,
			pointIndex + (size * size) + size,
			pointIndex + (size * size) + size + 1
		};
		return indexList;
	}*/

	/*Vector3[] GetCubeCornerPositions(int i)
    {
		return new Vector3[] {
			points[i],
			points[i + 1],
			points[i + size],
			points[i + size+ 1],
			points[i + (size * size)],
			points[i + (size * size) + 1],
			points[i + (size * size) + size],
			points[i + (size * size) + size+ 1]
	};
    }*/

	/*void DebugHoledCubeMeshes()
    {
		Cube cube = new Cube();

		cube.values = new float[]
		{
			1, 1, -1, 1, -1, 1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[0],
			points[0 + 1],
			points[0 + size],
			points[0 + size + 1],
			points[0 + (size * size)],
			points[0 + (size * size) + 1],
			points[0 + (size * size) + size],
			points[0 + (size * size) + size + 1]
		};

		int cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		int[] triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, 1, 1, 1, 1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[1],
			points[1 + 1],
			points[1 + size],
			points[1 + size + 1],
			points[1 + (size * size)],
			points[1 + (size * size) + 1],
			points[1 + (size * size) + size],
			points[1 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, 1, -1, 1, 1, 1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[2],
			points[2 + 1],
			points[2 + size],
			points[2 + size + 1],
			points[2 + (size * size)],
			points[2 + (size * size) + 1],
			points[2 + (size * size) + size],
			points[2 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, 1, 1, -1, 1, 1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[3],
			points[3 + 1],
			points[3 + size],
			points[3 + size + 1],
			points[3 + (size * size)],
			points[3 + (size * size) + 1],
			points[3 + (size * size) + size],
			points[3 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, 1, 1, -1, 1, -1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[4],
			points[4 + 1],
			points[4 + size],
			points[4 + size + 1],
			points[4 + (size * size)],
			points[4 + (size * size) + 1],
			points[4 + (size * size) + size],
			points[4 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, 1, 1, 1, 1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[5],
			points[5 + 1],
			points[5 + size],
			points[5 + size + 1],
			points[5 + (size * size)],
			points[5 + (size * size) + 1],
			points[5 + (size * size) + size],
			points[5 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, 1, 1, 1, -1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[6],
			points[6 + 1],
			points[6 + size],
			points[6 + size + 1],
			points[6 + (size * size)],
			points[6 + (size * size) + 1],
			points[6 + (size * size) + size],
			points[6 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, 1, 1, -1, 1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[7],
			points[7 + 1],
			points[7 + size],
			points[7 + size + 1],
			points[7 + (size * size)],
			points[7 + (size * size) + 1],
			points[7 + (size * size) + size],
			points[7 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, -1, 1, 1, 1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[8],
			points[8 + 1],
			points[8 + size],
			points[8 + size + 1],
			points[8 + (size * size)],
			points[8 + (size * size) + 1],
			points[8 + (size * size) + size],
			points[8 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, 1, -1, 1, 1, 1, 1
		};
		cube.points = new Vector3[]
		{
			points[9],
			points[9 + 1],
			points[9 + size],
			points[9 + size + 1],
			points[9 + (size * size)],
			points[9 + (size * size) + 1],
			points[9 + (size * size) + size],
			points[9 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, 1, 1, 1, 1, -1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[10],
			points[10 + 1],
			points[10 + size],
			points[10 + size + 1],
			points[10 + (size * size)],
			points[10 + (size * size) + 1],
			points[10 + (size * size) + size],
			points[10 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, 1, 1, 1, -1, 1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[11],
			points[11 + 1],
			points[11 + size],
			points[11 + size + 1],
			points[11 + (size * size)],
			points[11 + (size * size) + 1],
			points[11 + (size * size) + size],
			points[11 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}












		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, 1, -1, 1, -1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[12],
			points[12 + 1],
			points[12 + size],
			points[12 + size + 1],
			points[12 + (size * size)],
			points[12 + (size * size) + 1],
			points[12 + (size * size) + size],
			points[12 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, -1, -1, -1, -1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[13],
			points[13 + 1],
			points[13 + size],
			points[13 + size + 1],
			points[13 + (size * size)],
			points[13 + (size * size) + 1],
			points[13 + (size * size) + size],
			points[13 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, 1, -1, -1, -1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[14],
			points[14 + 1],
			points[14 + size],
			points[14 + size + 1],
			points[14 + (size * size)],
			points[14 + (size * size) + 1],
			points[14 + (size * size) + size],
			points[14 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, -1, 1, -1, -1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[15],
			points[15 + 1],
			points[15 + size],
			points[15 + size + 1],
			points[15 + (size * size)],
			points[15 + (size * size) + 1],
			points[15 + (size * size) + size],
			points[15 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, -1, 1, -1, 1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[16],
			points[16 + 1],
			points[16 + size],
			points[16 + size + 1],
			points[16 + (size * size)],
			points[16 + (size * size) + 1],
			points[16 + (size * size) + size],
			points[16 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, -1, -1, -1, -1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[17],
			points[17 + 1],
			points[17 + size],
			points[17 + size + 1],
			points[17 + (size * size)],
			points[17 + (size * size) + 1],
			points[17 + (size * size) + size],
			points[17 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, -1, -1, -1, 1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[18],
			points[18 + 1],
			points[18 + size],
			points[18 + size + 1],
			points[18 + (size * size)],
			points[18 + (size * size) + 1],
			points[18 + (size * size) + size],
			points[18 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, -1, -1, 1, -1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[20],
			points[20 + 1],
			points[20 + size],
			points[20 + size + 1],
			points[20 + (size * size)],
			points[20 + (size * size) + 1],
			points[20 + (size * size) + size],
			points[20 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, 1, 1, -1, -1, -1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[21],
			points[21 + 1],
			points[21 + size],
			points[21 + size + 1],
			points[21 + (size * size)],
			points[21 + (size * size) + 1],
			points[21 + (size * size) + size],
			points[21 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			1, -1, -1, 1, -1, -1, -1, -1
		};
		cube.points = new Vector3[]
		{
			points[22],
			points[22 + 1],
			points[22 + size],
			points[22 + size + 1],
			points[22 + (size * size)],
			points[22 + (size * size) + 1],
			points[22 + (size * size) + size],
			points[22 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, -1, -1, -1, 1, 1, -1
		};
		cube.points = new Vector3[]
		{
			points[23],
			points[23 + 1],
			points[23 + size],
			points[23 + size + 1],
			points[23 + (size * size)],
			points[23 + (size * size) + 1],
			points[23 + (size * size) + size],
			points[23 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}

		cube = new Cube();

		cube.values = new float[]
		{
			-1, -1, 1, -1, 1, -1, -1, 1
		};
		cube.points = new Vector3[]
		{
			points[24],
			points[24 + 1],
			points[24 + size],
			points[24 + size + 1],
			points[24 + (size * size)],
			points[24 + (size * size) + 1],
			points[24 + (size * size) + size],
			points[24 + (size * size) + size + 1]
		};

		cubeIndex = 0;
		for (int j = 0; j < 8; j++)
		{
			if (cube.values[j] < 0f)
			{
				cubeIndex |= 1 << j;
			}
		}

		triangulation = trianglulationTable[cubeIndex];
		Debug.Log(cubeIndex);

		foreach (int edgeIndex in triangulation)
		{
			if (edgeIndex != -1)
			{
				int indexA = edgeVertexIndices[edgeIndex][0];
				int indexB = edgeVertexIndices[edgeIndex][1];

				float t = (0 - cube.values[indexA]) / (cube.values[indexB] - cube.values[indexA]);
				Vector3 vertexPos = cube.points[indexA] + t * (cube.points[indexB] - cube.points[indexA]);
				vertices.Add(vertexPos);
				triangles.Add(vertices.Count - 1);
			}
		}
	}
*/
}
