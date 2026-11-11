using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading;
using System.Threading.Tasks;
//using System.Diagnostics;

public class MarchingCubes : MonoBehaviour
{
	[Header("Chunk Settings")]
	public Material terrainMaterial;
	public int chunkSize;
	public int chunkRenderDist = 10;

	[Header("Frequencies")]
	public float noiseFreq = 0.1f;
	[Range(-1f, 1f)]
	public float isoLevel = 0.5f;
	public float noiseFreqGround;
	public float noiseFreqCeiling;
	public float noiseFreqMask;
	public float noiseScale;

	[Header("FBM Settings")]
	public int octaves;//= 4;
	public float lacunarity;//= 1.8715;
	public float persistence;//= 0.65;
	public float amplitude;//= 1.0;

	[Header("Height Settings")]
	public float groundLevel;
	public float groundBlend;
	public float groundVariance;
	public float ceiling;
	public float ceilingBlend;
	public float ceilingVariance;
	public Texture2D heightShapingTexture;
	public int heightEvaluationResolution;

	public bool isCavernous;

	[Header("Height Shaping Settings")]
	public AnimationCurve continentalnessCurve;
	public float continentalnessFrequency;
	public int continentalnessOctaves;
	public float continentalnessLacunarity;
	public float continentalnessPersistence;
	public float continentalnessAmplitude;

	[Header("")]
	public AnimationCurve erosionCurve;
	public float erosionFrequency;
	public int erosionOctaves;
	public float erosionLacunarity;
	public float erosionPersistence;
	public float erosionAmplitude;

	[Header("")]
	public AnimationCurve peaksValleysCurve;
	public float peaksValleysFrequency;
	public int peaksValleysOctaves;
	public float peaksValleysLacunarity;
	public float peaksValleysPersistence;
	public float peaksValleysAmplitude;

	[Header("")]
	public ComputeShader cShader;
	public ComputeShader cShaderNoise;
	public ComputeShader cShaderHNoise;

	ComputeBuffer triangleBuffer;
	ComputeBuffer pointsBuffer;
	ComputeBuffer triCountBuffer;
	ComputeBuffer sidesBuffer;
	ComputeBuffer heightEvaluationBuffer;
	ComputeBuffer deformationBuffer;


	//ComputeBuffer hasHolesBuffer;
	ComputeBuffer floorBuffer;
	ComputeBuffer ceilingBuffer;
	//ComputeBuffer minMaxHeightBuffer;

	[Header("")]
	public GameObject chunkContainer;
	public GameObject emptyChunkContainer;

	List<Chunk> chunkPool = new List<Chunk>();

	Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
	HashSet<Chunk> borderChunks = new HashSet<Chunk>();
	Dictionary<Vector3Int, Dictionary<float, Chunk>> preComputedChunks = new Dictionary<Vector3Int, Dictionary<float, Chunk>>();
	float[] chunkPoints;
	List<List<int>> chunkSideIndices = new List<List<int>>();

	Dictionary<Vector3Int, List<Deformation>> chunkCoordDeformationDict = new Dictionary<Vector3Int, List<Deformation>>();

	VertexData[] vertexDataArray;

	Vector3 seedOffset;
	Vector2 seedOffsetFloor;
	Vector2 seedOffsetCeiling;
	Vector2 seedOffsetCont;
	Vector2 seedOffsetErosion;
	Vector2 seedOffsetPnV;

	public Transform player;
	Vector3Int playerChunkPos;



	
	struct Triangle
	{
#pragma warning disable 649 // disable unassigned variable warning
		public Vector3 a;
		public Vector3 b;
		public Vector3 c;

		public Vector3 this[int i]
		{
			get
			{
				switch (i)
				{
					case 0:
						return a;
					case 1:
						return b;
					default:
						return c;
				}
			}
		}
	}


	public struct Deformation
    {
		public Vector3 position;
		public float radius;
		public float blend;
		public float noiseFreq;
		public float noiseOct;
		public float noiseAmp;
		public float noiseLac;
		public float noisePers;


        public Deformation(Vector3 _position, float _radius, float _blend, float _noiseFreq, float _noiseAmp, float _noiseOct, float _noiseLac, float _noisePers)
        {
			position = _position;
			radius = _radius;
			blend = _blend;
			noiseFreq = _noiseFreq;
			noiseAmp = _noiseAmp;
			noiseOct = _noiseOct;
			noiseLac = _noiseLac;
			noisePers = _noisePers;
		}
    }

	// Start is called before the first frame update
	void Start()
	{
		//RenderTexture renderTexture = new RenderTexture(heightShapingTexture.width, heightShapingTexture.height, 24, RenderTextureFormat.ARGBFloat);
		//renderTexture.enableRandomWrite = true;
		Vector3[] evalList = new Vector3[heightEvaluationResolution];
		
		for (int i = 0; i < heightEvaluationResolution; i++)
        {
			evalList[i] = new Vector3(
				continentalnessCurve.Evaluate((float)i / heightEvaluationResolution),
				erosionCurve.Evaluate((float)i / heightEvaluationResolution),
				peaksValleysCurve.Evaluate((float)i / heightEvaluationResolution)
				);
        }

		heightEvaluationBuffer = new ComputeBuffer(heightEvaluationResolution, sizeof(float) * 3);
		heightEvaluationBuffer.SetData(evalList);

		//Graphics.Blit(heightShapingTexture, renderTexture);


		//cShaderNoise.SetTexture(0, "heightTexture", renderTexture);

		//CalculateChunkSides();


		//mainThread = Thread.CurrentThread;
		playerChunkPos = new Vector3Int(((int)(player.position.x + chunkSize / 2) / chunkSize), ((int)(player.position.y + chunkSize / 2) / chunkSize), ((int)(player.position.z + chunkSize / 2) / chunkSize));
		InitializeChunks();
		//ChunkTest();
	}

    void Update()
    {
		Vector3 playerChunkPosition = new Vector3(((player.position.x + chunkSize / 2) / chunkSize), ((player.position.y + chunkSize / 2) / chunkSize), ((player.position.z + chunkSize / 2) / chunkSize));

		//Debug.Log(Mathf.Abs(playerChunkPos.x - playerChunkPosition.x));
		//Debug.Log(playerChunkPos);

		if (Mathf.Abs(playerChunkPos.x - playerChunkPosition.x) > 1.25f)
		{
			playerChunkPos = new Vector3Int(((int)(player.position.x + chunkSize / 2) / chunkSize), playerChunkPos.y, playerChunkPos.z);

			//Debug.Log(playerChunkPos);

			//Debug.Log(playerChunkPos);
			//Debug.Log(playerChunkPosition);

			UpdateChunks();
        }
		if (Mathf.Abs(playerChunkPos.z - playerChunkPosition.z) > 1.25f)
        {
			playerChunkPos = new Vector3Int(playerChunkPos.x, playerChunkPos.y, ((int)(player.position.z + chunkSize / 2) / chunkSize));

			//Debug.Log(playerChunkPos);

			//Debug.Log(playerChunkPos);
			//Debug.Log(playerChunkPosition);

			UpdateChunks();
		}


		//foreach ( Chunk chunk in chunks.Values)
        //{
		//	if ((Vector3.ProjectOnPlane(chunk.coord - playerChunkPos, Vector3.up)).magnitude < chunkRenderDist * 0.5f)
        //    {
		//		chunk.meshCollider.enabled = true;
        //    }
        //    else
        //    {
		//		chunk.meshCollider.enabled = false;
		//	}
        //}
	}

	int IndexFromCoord(int x, int y, int z)
    {
		return x + y * chunkSize + z * chunkSize * chunkSize;
	}

	void CalculateChunkSides()
    {
		for (int i = 0; i < 6; i++) chunkSideIndices.Add(new List<int>());

		for (int y = 0; y < chunkSize; y++)
        {
			for (int x = 0; x < chunkSize; x++)
            {
				for (int z = 0; z < chunkSize; z++)
				{
					if (y == 0)
					{
						chunkSideIndices[2].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x, y - 1, z), Color.red, 100000);
					}
					else if (y == chunkSize - 1)
					{
						chunkSideIndices[3].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x, y + 1, z), Color.red, 100000);
					}
					if (x == 0)
					{
						chunkSideIndices[0].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x - 1, y, z), Color.red, 100000);
					}
					else if (x == chunkSize - 1)
					{
						chunkSideIndices[2].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x + 1, y, z), Color.red, 100000);
					}
					if (z == 0)
					{
						chunkSideIndices[4].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x, y, z - 1), Color.red, 100000);
					}
					else if (z == chunkSize - 1)
					{
						chunkSideIndices[5].Add(IndexFromCoord(x, y, z));
						//Debug.DrawLine(new Vector3(x, y, z), new Vector3(x, y, z + 1), Color.red, 100000);
					}
				}
            }
        }
	}

	void InitializeChunks()
	{
		chunkPoints = new float[chunkSize * chunkSize * chunkSize];


		// Make chunk pool
		for (int z = -chunkRenderDist + playerChunkPos.z; z < chunkRenderDist + playerChunkPos.z; z++)
		{
			for (int x = -chunkRenderDist + playerChunkPos.x; x < chunkRenderDist + playerChunkPos.x; x++)
			{
				for (int y = -chunkRenderDist + playerChunkPos.y; y < chunkRenderDist + playerChunkPos.y; y++)
				{
					GameObject chunkObj = new GameObject("");
					chunkObj.transform.SetParent(chunkContainer.transform);
					Chunk chunk = chunkObj.AddComponent<Chunk>();
					chunkObj.SetActive(false);
					chunk.SetUp(terrainMaterial);

					chunkPool.Add(chunk);
				}
			}
		}

		seedOffset = new Vector3(Random.Range(0, 99999), Random.Range(0, 99999), Random.Range(0, 99999));
		seedOffsetFloor = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));
		seedOffsetCeiling = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));
		seedOffsetCont = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));
		seedOffsetErosion = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));
		seedOffsetPnV = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));


		// Make first chunk

		Vector3Int coord = new Vector3Int(0, 0, 0);
		chunks.Add(coord, CreateChunk(coord));

		Queue<Chunk> chunkQueue = new Queue<Chunk>();

		chunkQueue.Enqueue(chunks[coord]);

		bool hasStartedChain = false;

		while (chunkQueue.Count > 0)
		{
			if (chunkCoordDeformationDict.ContainsKey(chunkQueue.Peek().coord))
			{
				chunkQueue.Peek().deformations = chunkCoordDeformationDict[chunkQueue.Peek().coord];
			}

			uint[] shouldGenerateToSidesUint = new uint[12];
			sidesBuffer = new ComputeBuffer(12, sizeof(uint));
			sidesBuffer.SetData(shouldGenerateToSidesUint);

			GenerateChunkDensity(chunkQueue.Peek());
			GenerateChunk(chunkQueue.Peek());

			bool[] shouldGenerateToSides = new bool[12];
			sidesBuffer.GetData(shouldGenerateToSidesUint);

			sidesBuffer.Release();
			for (int i = 0; i < 12; i++)
			{
				if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
				if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
			}

			chunkQueue.Peek().shouldGenerateToSides = shouldGenerateToSides;

			Vector3Int qCoord = chunkQueue.Peek().coord;
			Vector3Int[] newCoords = { 
				qCoord + Vector3Int.right,
				qCoord + Vector3Int.left,
				qCoord + Vector3Int.up,
				qCoord + Vector3Int.down,
				qCoord + Vector3Int.forward,
				qCoord + Vector3Int.back, 
			};

			for (int i = 0; i < 6; i++)
            {
				if (shouldGenerateToSides[i] && shouldGenerateToSides[i + 6] &&
				!chunks.ContainsKey(newCoords[i]))
				{
					if ((newCoords[i] - playerChunkPos).magnitude < chunkRenderDist)
					{
						Chunk newChunk = CreateChunk(newCoords[i]);
						chunkQueue.Enqueue(newChunk);
						chunks.Add(newCoords[i], newChunk);
					}
					else if (!borderChunks.Contains(chunkQueue.Peek()))
					{
						borderChunks.Add(chunkQueue.Peek());
					}

					hasStartedChain = true;
				}
			}

			//if (!borderChunks.Contains(chunkQueue.Peek()) && (chunkQueue.Peek().coord - playerChunkPos) )
			//{
			//	borderChunks.Add(chunkQueue.Peek());
			//}

			if (!hasStartedChain)
			{
				Vector3Int newCoord = qCoord + Vector3Int.up;
				Chunk newChunk = CreateChunk(newCoord);
				chunkQueue.Enqueue(newChunk);
				chunks.Add(newCoord, newChunk);

				player.position = Vector3.up * chunkSize * newCoord.y + Vector3.up * chunkSize * 2;
			}

			chunkQueue.Dequeue();
		}
	}

	Chunk CreateChunk(Vector3Int coord)
    {
		Chunk chunk = chunkPool[0];

		chunk.gameObject.SetActive(true);

		chunkPool.RemoveAt(0);
		chunk.gameObject.name = coord.x.ToString() + ", " + coord.y.ToString() + ", " + coord.z.ToString();
		//chunkObj.transform.parent = chunkContainer.transform;

		//chunk.gameObject.GetComponent<MeshFilter>().mesh = new Mesh();

		chunk.gameObject.GetComponent<MeshFilter>().mesh.Clear();
		//chunk.gameObject.GetComponent<MeshFilter>().mesh.vertices = new Vector3[0];
		//chunk.gameObject.GetComponent<MeshFilter>().mesh.triangles = new int[0];
		chunk.mesh = chunk.gameObject.GetComponent<MeshFilter>().mesh;

		chunk.coord = coord;
		chunk.deformations = new List<Deformation>();
		chunk.deformations.Add(new Deformation(Vector3.zero, 0, 0, 0, 0, 0, 0, 0));
		return chunk;
	}

	void EmptyChunk(Vector3Int coord)
    {
		if (chunks[coord] != null)
        {
			Chunk chunk = chunks[coord];

			chunk.coord = new Vector3Int(0, 0, 0);

			//chunkObj.transform.parent = emptyChunkContainer.transform;
			chunkPool.Add(chunk);
			chunk.gameObject.SetActive(false);
		}
	}

	async void UpdateChunks()
    {
		HashSet<Chunk> newBorderChunks = new HashSet<Chunk>();

		HashSet<Vector3Int> chunkCoordsToRemove = new HashSet<Vector3Int>();
		
		foreach (Vector3Int coord in chunks.Keys)
		{
			if (Vector3.ProjectOnPlane(coord - playerChunkPos, Vector3.up).magnitude > chunkRenderDist)
			{
				chunkCoordsToRemove.Add(coord);
			}
		}
		foreach (Vector3Int coord in chunkCoordsToRemove)
		{
			Vector3Int[] borderingCoords = {
				coord + Vector3Int.up,
				coord + Vector3Int.down,
				coord + Vector3Int.right,
				coord + Vector3Int.left,
				coord + Vector3Int.forward,
				coord + Vector3Int.back,
				};

			foreach (Vector3Int borderingCoord in borderingCoords)
            {
				if (chunks.ContainsKey(borderingCoord) && !chunkCoordsToRemove.Contains(borderingCoord))
                {
					newBorderChunks.Add(chunks[borderingCoord]);
                }
            }

			EmptyChunk(coord);
			chunks.Remove(coord);
		}
		await Task.Yield();


		Vector3Int tempPlayerChunkPos = playerChunkPos;
		// needs work
		foreach ( Chunk borderChunk in borderChunks)
        {
			if (!Application.isPlaying) return;

			Queue<Chunk> chunkQueue = new Queue<Chunk>();

			Vector3Int bCoord = borderChunk.coord;
			Vector3Int[] bNewCoords = {
				bCoord + Vector3Int.right,
				bCoord + Vector3Int.left,
				bCoord + Vector3Int.up,
				bCoord + Vector3Int.down,
				bCoord + Vector3Int.forward,
				bCoord + Vector3Int.back,
				};

			for (int i = 0; i < 6; i++)
			{
				if (borderChunk.shouldGenerateToSides[i] && borderChunk.shouldGenerateToSides[i + 6] &&
				!chunks.ContainsKey(bNewCoords[i]))
				{
					if (Vector3.ProjectOnPlane(bNewCoords[i] - tempPlayerChunkPos, Vector3.up).magnitude < chunkRenderDist)
					{
						Chunk newChunk = CreateChunk(bNewCoords[i]);
						chunkQueue.Enqueue(newChunk);
						chunks.Add(bNewCoords[i], newChunk);
						if ((newChunk.coord - tempPlayerChunkPos).magnitude > chunkRenderDist - 1.6f && !borderChunks.Contains(borderChunk))
                        {
							newBorderChunks.Add(newChunk);
                        }
					}
					//else if (!borderChunks.Contains(borderChunk))
					//{
					//	newBorderChunks.Add(borderChunk);
					//}
				}
			}

			while (chunkQueue.Count > 0)
			{

				if (!Application.isPlaying) return;

				if (chunkCoordDeformationDict.ContainsKey(chunkQueue.Peek().coord))
                {
					chunkQueue.Peek().deformations = chunkCoordDeformationDict[chunkQueue.Peek().coord];
				}


				uint[] shouldGenerateToSidesUint = new uint[12];
				sidesBuffer = new ComputeBuffer(12, sizeof(uint));
				sidesBuffer.SetData(shouldGenerateToSidesUint);

				GenerateChunkDensity(chunkQueue.Peek());
				GenerateChunk(chunkQueue.Peek());

				bool[] shouldGenerateToSides = new bool[12];
				sidesBuffer.GetData(shouldGenerateToSidesUint);

				sidesBuffer.Release();
				for (int i = 0; i < 12; i++)
				{
					if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
					if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
				}

				chunkQueue.Peek().shouldGenerateToSides = shouldGenerateToSides;

				Vector3Int qCoord = chunkQueue.Peek().coord;
				Vector3Int[] newCoords = {
				qCoord + Vector3Int.right,
				qCoord + Vector3Int.left,
				qCoord + Vector3Int.up,
				qCoord + Vector3Int.down,
				qCoord + Vector3Int.forward,
				qCoord + Vector3Int.back,
				};

				for (int i = 0; i < 6; i++)
				{
					if (shouldGenerateToSides[i] && shouldGenerateToSides[i + 6] &&
					!chunks.ContainsKey(newCoords[i]))
					{
						if (Vector3.ProjectOnPlane((newCoords[i] - tempPlayerChunkPos), Vector3.up).magnitude < chunkRenderDist)
						{
							Chunk newChunk = CreateChunk(newCoords[i]);
							chunkQueue.Enqueue(newChunk);
							chunks.Add(newCoords[i], newChunk);
							if ((newChunk.coord - tempPlayerChunkPos).magnitude > chunkRenderDist - 1.6f && !newBorderChunks.Contains(newChunk))
                            {
								newBorderChunks.Add(newChunk);
							}
						}
						//else if (!newBorderChunks.Contains(chunkQueue.Peek()))
						//{
						//	newBorderChunks.Add(chunkQueue.Peek());
						//}
					}
				}

				chunkQueue.Dequeue();

				await Task.Yield();
			}
		}

		borderChunks = newBorderChunks;
	}

	void GenerateChunkDensity(Chunk chunk)
    {
		Vector3 chunkOffset = chunk.coord * (chunkSize - 1);

		chunk.transform.position = new Vector3(chunkOffset.x, chunkOffset.y, chunkOffset.z);

		// Populates point list with positions xyz and values w

		//for (int i = 0; i < chunkSize * chunkSize * chunkSize; i++)
		//{
		//	Vector3 voxelPoint = new Vector3(i % chunkSize, i / (chunkSize * chunkSize) % chunkSize, i / chunkSize % chunkSize);
		//
		//	chunkPoints[i] = new Vector4(voxelPoint.x, voxelPoint.y, voxelPoint.z, 0);
		//}

		chunkPoints = new float[chunkSize * chunkSize * chunkSize];


		//await Task.Yield();

		//chunk.SetUp(terrainMaterial);

		pointsBuffer = new ComputeBuffer(chunkSize * chunkSize * chunkSize, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointData)));

		if (chunk.deformations.Count > 0)
		{
			deformationBuffer = new ComputeBuffer(chunk.deformations.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Deformation)));
			deformationBuffer.SetData(chunk.deformations);
		}
		//hasHolesBuffer = new ComputeBuffer(2, sizeof(int));

		//Vector4[] array = new Vector4[chunkSize * chunkSize * chunkSize];

		//pointsBuffer.SetData(chunkPoints);
		pointsBuffer = GenerateNoiseValues(pointsBuffer, chunkOffset);
		//pointsBuffer.GetData(chunkPoints);

		//foreach (Vector4 point in chunkPoints)
        //{
		//	Debug.Log(point.w);
        //}
	}

	void GenerateChunk(Chunk chunk)
    {
		//hasHolesBuffer.SetData(chunk.hasHoles);

		int numThreadsPerAxis = Mathf.CeilToInt((chunkSize - 1) / (float)8);
		int numVoxels = (chunkSize - 1) * (chunkSize - 1) * (chunkSize - 1);
		int maxTriangleCount = numVoxels * 5;
		int maxVertexCount = maxTriangleCount * 3;

		//triangleBuffer = new ComputeBuffer(maxTriangleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData)), ComputeBufferType.Append);
		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(float) * 3, ComputeBufferType.Append);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		

		triangleBuffer.SetCounterValue(0);

		cShader.SetBuffer(0, "points", pointsBuffer);
		cShader.SetBuffer(0, "triangles", triangleBuffer);
		//cShader.SetBuffer(0, "hasHoles", hasHolesBuffer);
		cShader.SetInt("chunkSize", chunkSize);
		cShader.SetFloat("isoLevel", isoLevel);

		cShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];

		// Get triangle data from shader
		vertexDataArray = new VertexData[numTris * 3];
		triangleBuffer.GetData(vertexDataArray, 0, 0, numTris * 3);

		//Dictionary<Vector3, int> mergedVertsDict = new Dictionary<Vector3, int>();
		//mergedVertsDict.

		//var vertices = new Vector3[numTris * 3];
		//var meshTriangles = new int[numTris * 3];

		ReleaseBuffers();

		chunk.CreateMesh(vertexDataArray, numTris * 3, false);

		//for (int i = 0; i < numTris; i++)
		//{
		//	for (int j = 0; j < 3; j++)
		//	{
		//		meshTriangles[i * 3 + j] = i * 3 + j;
		//		vertices[i * 3 + j] = tris[i][j];
		//	}
		//}
		//chunk.mesh.SetVertices(vertices);
		//chunk.mesh.SetTriangles(meshTriangles, 0, true);
		//
		//chunk.mesh.RecalculateNormals();

		//hasHolesBuffer.GetData(chunk.hasHoles, 0, 0, 2);

	}

	public ComputeBuffer GenerateNoiseValues(ComputeBuffer cPointBuffer, Vector3 chunkOffset)
    {

		int numThreadsPerAxis = Mathf.CeilToInt((chunkSize) / (float)8);

		cShaderNoise.SetInt("chunkSize", chunkSize);
		cShaderNoise.SetVector("seedOffset", seedOffset);
		cShaderNoise.SetVector("seedOffsetGround", seedOffsetFloor);
		cShaderNoise.SetVector("seedOffsetCeiling", seedOffsetCeiling);
		cShaderNoise.SetVector("seedOffsetCont", seedOffsetCont);
		cShaderNoise.SetVector("seedOffsetErosion", seedOffsetErosion);
		cShaderNoise.SetVector("seedOffsetPnV", seedOffsetPnV);


		cShaderNoise.SetVector("chunkOffset", chunkOffset);
		cShaderNoise.SetFloat("noiseFreq", noiseFreq * noiseScale);
		cShaderNoise.SetFloat("noiseFreqGround", noiseFreqGround * noiseScale);
		cShaderNoise.SetFloat("noiseFreqCeiling", noiseFreqCeiling * noiseScale);
		cShaderNoise.SetFloat("noiseFreqMask", noiseFreqMask * noiseScale);


		cShaderNoise.SetFloat("contFreq", continentalnessFrequency * noiseScale);
		cShaderNoise.SetInt("contOct", continentalnessOctaves);
		cShaderNoise.SetFloat("contLac", continentalnessLacunarity);
		cShaderNoise.SetFloat("contPers", continentalnessPersistence);
		cShaderNoise.SetFloat("contAmp", continentalnessAmplitude);

		cShaderNoise.SetFloat("erosionFreq", erosionFrequency * noiseScale);
		cShaderNoise.SetInt("erosionOct", erosionOctaves);
		cShaderNoise.SetFloat("erosionLac", erosionLacunarity);
		cShaderNoise.SetFloat("erosionPers", erosionPersistence);
		cShaderNoise.SetFloat("erosionAmp", erosionAmplitude);

		cShaderNoise.SetFloat("pnvFreq", peaksValleysFrequency * noiseScale);
		cShaderNoise.SetInt("pnvOct", peaksValleysOctaves);
		cShaderNoise.SetFloat("pnvLac", peaksValleysLacunarity);
		cShaderNoise.SetFloat("pnvPers", peaksValleysPersistence);
		cShaderNoise.SetFloat("pnvAmp", peaksValleysAmplitude);


		cShaderNoise.SetFloat("isoLevel", isoLevel);

		cShaderNoise.SetInt("octaves", octaves);
		cShaderNoise.SetFloat("lacunarity", lacunarity);
		cShaderNoise.SetFloat("persistence", persistence);
		cShaderNoise.SetFloat("amplitude", amplitude);

		cShaderNoise.SetFloat("groundLevel", groundLevel / noiseScale);
		cShaderNoise.SetFloat("groundBlend", groundBlend / noiseScale);
		cShaderNoise.SetFloat("groundVariance", groundVariance / noiseScale);
		cShaderNoise.SetFloat("ceiling", ceiling / noiseScale);
		cShaderNoise.SetFloat("ceilingBlend", ceilingBlend / noiseScale);
		cShaderNoise.SetFloat("ceilingVariance", ceilingVariance / noiseScale);
		cShaderNoise.SetBool("isCavernous", isCavernous);

		cShaderNoise.SetFloat("evaluationLength", heightEvaluationResolution);

		cShaderNoise.SetBuffer(0, "points", cPointBuffer);
		cShaderNoise.SetBuffer(0, "sides", sidesBuffer);
		cShaderNoise.SetBuffer(0, "heightEvaluation", heightEvaluationBuffer);
		cShaderNoise.SetBuffer(0, "deformations", deformationBuffer);
		cShaderNoise.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

		return cPointBuffer;
	}

	public void GenerateHeightValues(ComputeBuffer cfloorBuffer, ComputeBuffer cCeilingBuffer, Vector2 chunkOffset)
    {

		int numThreadsPerAxis = Mathf.CeilToInt((chunkSize) / (float)8);

		cShaderHNoise.SetInt("chunkSize", chunkSize);
		cShaderHNoise.SetVector("chunkOffset", chunkOffset);
		cShaderHNoise.SetVector("seedOffsetFloor", seedOffsetFloor);
		cShaderHNoise.SetVector("seedOffsetCeiling", seedOffsetCeiling);
		cShaderHNoise.SetFloat("freqFloor", noiseFreqGround);
		cShaderHNoise.SetFloat("freqCeiling", noiseFreqCeiling);
		//cShaderHNoise.SetFloat("floorVariance", floorVariance);
		cShaderHNoise.SetFloat("ceilingVariance", ceilingVariance);
		cShaderHNoise.SetBuffer(0, "floorValues", cfloorBuffer);
		cShaderHNoise.SetBuffer(0, "ceilingValues", cCeilingBuffer);
		//cShaderHNoise.SetBuffer(0, "minMaxHeight", cMinMaxBuffer);

		cShaderHNoise.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, 1);

		floorBuffer = cfloorBuffer;
		ceilingBuffer = cCeilingBuffer;

		//minMaxHeightBuffer = cMinMaxBuffer;

    }

	public async void DeformChunks(Deformation deformation, float timeOffsetToApply = 0)
    {

		float time = Time.realtimeSinceStartup;

		Vector3Int chunkCoord = new Vector3Int( (int)Mathf.Floor(deformation.position.x / (chunkSize - 1)), (int)Mathf.Floor(deformation.position.y / (chunkSize - 1)), (int)Mathf.Floor(deformation.position.z / (chunkSize - 1)));


		List<Vector3Int> intersectingCoords = new List<Vector3Int>();
		//intersectingCoords.Add(chunkCoord);

		Vector3 chunkCenterPos = (Vector3)chunkCoord * (chunkSize - 1) + Vector3.one * (chunkSize - 1) * 0.5f;
		List<Vector3> chunkCenters = new List<Vector3>();


		Vector3 sphereCenter = new Vector3(deformation.position.x, deformation.position.y, deformation.position.z);
		float sphereRadius = deformation.radius;
		Vector3 cubeHalfExtents = Vector3.one * (chunkSize - 1) * 0.5f;

		int maxChunkReach = Mathf.CeilToInt(sphereRadius / (chunkSize - 1));

		for (int x = - maxChunkReach; x <= maxChunkReach; x++)
        {
			for (int y = -maxChunkReach; y <= maxChunkReach; y++)
			{
				for (int z = -maxChunkReach; z <= maxChunkReach; z++)
				{
					chunkCenters.Add(new Vector3(chunkCenterPos.x + (chunkSize - 1) * x, chunkCenterPos.y + (chunkSize - 1) * y, chunkCenterPos.z + (chunkSize - 1) * z));
				}
			}
		}
		
		foreach ( Vector3 chunkCenter in chunkCenters)
        {
		
			Vector3 closestPointOnCube = new Vector3(
					Mathf.Clamp(sphereCenter.x, chunkCenter.x - cubeHalfExtents.x, chunkCenter.x + cubeHalfExtents.x),
					Mathf.Clamp(sphereCenter.y, chunkCenter.y - cubeHalfExtents.y, chunkCenter.y + cubeHalfExtents.y),
					Mathf.Clamp(sphereCenter.z, chunkCenter.z - cubeHalfExtents.z, chunkCenter.z + cubeHalfExtents.z)
				);
		
			float distToChunk = Vector3.Distance(sphereCenter, closestPointOnCube);

		
			if (distToChunk <= sphereRadius)
            {
				intersectingCoords.Add(new Vector3Int(Mathf.FloorToInt(chunkCenter.x / (chunkSize - 1)), Mathf.FloorToInt(chunkCenter.y / (chunkSize - 1)), Mathf.FloorToInt(chunkCenter.z / (chunkSize - 1))));
				//Debug.DrawLine(sphereCenter, closestPointOnCube, Color.green, 10000);
			}
        }

		//List<Chunk> intersectingChunks = new List<Chunk>();




		float timeToApply = Time.realtimeSinceStartup + timeOffsetToApply;

		foreach (Vector3Int coord in intersectingCoords)
        {

			if (chunks.ContainsKey(coord) && !preComputedChunks.ContainsKey(coord))
            {
				Chunk intersectingChunk = CreateChunk(coord);
				//intersectingChunks.Add(intersectingChunk);

				intersectingChunk.deformations = new List<Deformation>(chunks[coord].deformations);
				intersectingChunk.deformations.Add(deformation);



				uint[] shouldGenerateToSidesUint = new uint[12];
				sidesBuffer = new ComputeBuffer(12, sizeof(uint));
				sidesBuffer.SetData(shouldGenerateToSidesUint);

				GenerateChunkDensity(intersectingChunk);
				GenerateChunk(intersectingChunk);

				sidesBuffer.GetData(shouldGenerateToSidesUint);

				sidesBuffer.Release();

				bool[] shouldGenerateToSides = new bool[12];

				for (int i = 0; i < 12; i++)
				{
					if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
					if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
				}
				intersectingChunk.shouldGenerateToSides = shouldGenerateToSides;

				intersectingChunk.gameObject.SetActive(false);


				preComputedChunks.Add(coord, new Dictionary<float, Chunk>());
				preComputedChunks[coord].Add(timeToApply, intersectingChunk);
			}
			else if (chunks.ContainsKey(coord) && preComputedChunks.ContainsKey(coord))
            {
				Chunk intersectingChunk = CreateChunk(coord);
				//intersectingChunks.Add(intersectingChunk);

				List<float> timesOfApplyToRegen = new List<float>();

				float closestTimeOfApply = 0;
                foreach (float timeOfApply in preComputedChunks[coord].Keys)
                {
                    if (timeOfApply < timeToApply && timeOfApply > closestTimeOfApply)
                    {
                        closestTimeOfApply = timeOfApply;
                    }
					else if (timeOfApply > timeToApply)
                    {
						preComputedChunks[coord][timeOfApply].deformations.Add(deformation);
						timesOfApplyToRegen.Add(timeOfApply);
                    }
                }

				foreach(float timeOfApply in timesOfApplyToRegen)
                {
					if (preComputedChunks[coord].ContainsKey(timeOfApply))
					{
						RegeneratePreComputedChunk(coord, timeOfApply);
					}
					await Task.Yield();
				}

                if (closestTimeOfApply != 0 && preComputedChunks[coord].ContainsKey(closestTimeOfApply))
                {
					intersectingChunk.deformations = new List<Deformation>(preComputedChunks[coord][closestTimeOfApply].deformations);
                }
                else
                {
					intersectingChunk.deformations = new List<Deformation>(chunks[coord].deformations);
                }

				intersectingChunk.deformations.Add(deformation);

				uint[] shouldGenerateToSidesUint = new uint[12];
				sidesBuffer = new ComputeBuffer(12, sizeof(uint));
				sidesBuffer.SetData(shouldGenerateToSidesUint);


				GenerateChunkDensity(intersectingChunk);
				GenerateChunk(intersectingChunk);

				sidesBuffer.GetData(shouldGenerateToSidesUint);

				sidesBuffer.Release();

				bool[] shouldGenerateToSides = new bool[12];

				for (int i = 0; i < 12; i++)
				{
					if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
					if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
				}
				intersectingChunk.shouldGenerateToSides = shouldGenerateToSides;

				intersectingChunk.gameObject.SetActive(false);

				if (preComputedChunks.ContainsKey(coord))
				{
					preComputedChunks[coord].Add(timeToApply, intersectingChunk);
				}
			}
            else if (!chunks.ContainsKey(coord) && !preComputedChunks.ContainsKey(coord))
            {
				//chunks.Add(coord, CreateChunk(coord));
				Chunk intersectingChunk = CreateChunk(coord);

				intersectingChunk.deformations.Add(deformation);

				uint[] shouldGenerateToSidesUint = new uint[12];
				sidesBuffer = new ComputeBuffer(12, sizeof(uint));
				sidesBuffer.SetData(shouldGenerateToSidesUint);

				GenerateChunkDensity(intersectingChunk);
				GenerateChunk(intersectingChunk);

				sidesBuffer.GetData(shouldGenerateToSidesUint); 

				sidesBuffer.Release();

				bool[] shouldGenerateToSides = new bool[12];

				for (int i = 0; i < 12; i++)
				{
					if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
					if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
				}
				intersectingChunk.shouldGenerateToSides = shouldGenerateToSides;

				intersectingChunk.gameObject.SetActive(false);

				preComputedChunks.Add(coord, new Dictionary<float, Chunk>());
				preComputedChunks[coord].Add(timeToApply, intersectingChunk);
			}
			else if (!chunks.ContainsKey(coord) && preComputedChunks.ContainsKey(coord))
            {
				Chunk intersectingChunk = CreateChunk(coord);
				List<float> timesOfApplyToRegen = new List<float>();

				float closestTimeOfApply = 0;
				foreach (float timeOfApply in preComputedChunks[coord].Keys)
				{
					if (timeOfApply < timeToApply && timeOfApply > closestTimeOfApply)
					{
						closestTimeOfApply = timeOfApply;
					}
					else if (timeOfApply > timeToApply)
					{
						preComputedChunks[coord][timeOfApply].deformations.Add(deformation);
						timesOfApplyToRegen.Add(timeOfApply);
					}
				}

				foreach (float timeOfApply in timesOfApplyToRegen)
				{
					if (preComputedChunks[coord].ContainsKey(timeOfApply))
					{
						RegeneratePreComputedChunk(coord, timeOfApply);
					}
					await Task.Yield();
				}

				if (closestTimeOfApply != 0 && preComputedChunks[coord].ContainsKey(closestTimeOfApply))
				{
					intersectingChunk.deformations = new List<Deformation>(preComputedChunks[coord][closestTimeOfApply].deformations);
				}

				intersectingChunk.deformations.Add(deformation);

				uint[] shouldGenerateToSidesUint = new uint[12];
				sidesBuffer = new ComputeBuffer(12, sizeof(uint));
				sidesBuffer.SetData(shouldGenerateToSidesUint);

				GenerateChunkDensity(intersectingChunk);
				GenerateChunk(intersectingChunk);

				sidesBuffer.GetData(shouldGenerateToSidesUint);

				sidesBuffer.Release();

				bool[] shouldGenerateToSides = new bool[12];

				for (int i = 0; i < 12; i++)
				{
					if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
					if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
				}
				intersectingChunk.shouldGenerateToSides = shouldGenerateToSides;

				intersectingChunk.gameObject.SetActive(false);

				if (preComputedChunks[coord] != null)
				{
					preComputedChunks[coord].Add(timeToApply, intersectingChunk);
				}
			}
			await Task.Yield();
        }

		while (Time.realtimeSinceStartup < timeToApply)
        {
			await Task.Yield();
        }
		

		foreach(Vector3Int coord in intersectingCoords)
        {
			if (preComputedChunks.ContainsKey(coord) && preComputedChunks[coord].ContainsKey(timeToApply))
            {
				if (chunks.ContainsKey(coord))
                {
					EmptyChunk(coord);
					chunks.Remove(coord);
                }
				chunks.Add(coord, preComputedChunks[coord][timeToApply]);
				chunks[coord].gameObject.SetActive(true);
				//Debug.Log(chunks[coord].deformations.Count);

				preComputedChunks[coord].Remove(timeToApply);
				if (preComputedChunks[coord].Count == 0)
                {
					preComputedChunks.Remove(coord);
                }


				if (!chunkCoordDeformationDict.ContainsKey(coord))
				{
					chunkCoordDeformationDict.Add(coord, chunks[coord].deformations);
				}
				else
				{
					chunkCoordDeformationDict[coord] = chunks[coord].deformations;
				}
			}

			//Vector3Int coord = intersectingChunk.coord;
			//if (chunks.ContainsKey(coord))
			//{
			//	EmptyChunk(coord);
			//	chunks.Remove(coord);
			//}
			//
			//chunks.Add(coord, intersectingChunk);
			//chunks[coord].gameObject.SetActive(true);
			//
			//if (!chunkCoordDeformationDict.ContainsKey(coord))
			//{
			//	chunkCoordDeformationDict.Add(coord, intersectingChunk.deformations);
			//}
			//else
			//{
			//	chunkCoordDeformationDict[coord] = intersectingChunk.deformations;
			//}
		}
    }

	void RegeneratePreComputedChunk(Vector3Int coord, float timeToApply)
    {
		Chunk updatedChunk = CreateChunk(coord);
		updatedChunk.deformations = preComputedChunks[coord][timeToApply].deformations;


		uint[] shouldGenerateToSidesUint = new uint[12];
		sidesBuffer = new ComputeBuffer(12, sizeof(uint));
		sidesBuffer.SetData(shouldGenerateToSidesUint);

		GenerateChunkDensity(updatedChunk);
		GenerateChunk(updatedChunk);

		sidesBuffer.GetData(shouldGenerateToSidesUint);

		sidesBuffer.Release();

		bool[] shouldGenerateToSides = new bool[12];

		for (int i = 0; i < 12; i++)
		{
			if (shouldGenerateToSidesUint[i] == 0) shouldGenerateToSides[i] = false;
			if (shouldGenerateToSidesUint[i] == 1) shouldGenerateToSides[i] = true;
		}
		updatedChunk.shouldGenerateToSides = shouldGenerateToSides;

		updatedChunk.gameObject.SetActive(false);

		GameObject oldChunk = preComputedChunks[coord][timeToApply].gameObject;
		preComputedChunks[coord][timeToApply] = updatedChunk;
		Destroy(oldChunk);
	}

	void CreateBuffers()
    {
		int numThreadsPerAxis = Mathf.CeilToInt((chunkSize - 1) / (float)8);
		int numVoxels = (chunkSize - 1) * (chunkSize - 1) * (chunkSize - 1);
		int maxTriangleCount = numVoxels * 5;
		int maxVertexCount = maxTriangleCount * 3;

		triangleBuffer = new ComputeBuffer(maxTriangleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData)), ComputeBufferType.Append);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		vertexDataArray = new VertexData[maxVertexCount];
	}

	void ReleaseBuffers()
	{
		if (triangleBuffer != null)
		{
			triangleBuffer.Release();
			pointsBuffer.Release();
			triCountBuffer.Release();
			deformationBuffer.Release();
			//hasHolesBuffer.Release();
		}
	}


    private void OnApplicationQuit()
    {
		ReleaseBuffers();
		heightEvaluationBuffer.Release();
	}
}
