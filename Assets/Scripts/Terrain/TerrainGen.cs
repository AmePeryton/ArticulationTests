using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainGen : MonoBehaviour
{
	[Header("Terrain Settings")]
	public TerrainRemapMode remapMode;

	[Header("Noise Settings")]
	public int noiseSeed;
	public float noiseFrequency;
	public int noiseOctaves;
	public float noiseLacunarity;
	public float noiseGain;
	public float noiseScale;
	public float noiseOffset;

	[Header("Mesh Settings")]
	public bool centerMesh;		// If the mesh should generate centered on the transform
	public float gridCellSize;
	public int gridWidth;	// X
	public int gridDepth;	// Z

	[Header("Visuals")]
	public Material terrainMat;

	[Header("Triangulation Data")]
	private Vector3[] vertices;
	private Vector2[] uvs;
	private int[] triangles;

	[Header("Coroutine Settings")]
	public float maxJobTime;
	public int maxJobCount;

	// Private fields
	private Mesh mesh;
	//private MeshFilter meshFilter;
	//private MeshRenderer meshRenderer;
	//private MeshCollider meshCollider;

	private void Awake()
	{
		//meshFilter = GetComponent<MeshFilter>();
		//meshRenderer = GetComponent<MeshRenderer>();
		//meshCollider = GetComponent<MeshCollider>();
	}

	private void Start()
	{
		BuildTerrainCall();
	}

	// Place vertices of the mesh
	public IEnumerator PlaceVerts()
	{
		float startTime = Time.realtimeSinceStartup;
		vertices = new Vector3[(gridWidth + 1) * (gridDepth + 1)];

		float jobTimer = Time.realtimeSinceStartup;
		for (int i = 0; i <= gridWidth; i++)
		{
			for (int j = 0; j <= gridDepth; j++)
			{
				Vector3 meshOffset = Vector3.zero;
				if(centerMesh)
				{
					meshOffset = new Vector3((gridWidth / 2f) * gridCellSize, 0, (gridDepth / 2f) * gridCellSize);
				}

				vertices[(gridDepth + 1) * i + j] = new Vector3(i * gridCellSize, 0, j * gridCellSize) - meshOffset;

				// Conditional yield (job timer)
				if (Time.realtimeSinceStartup - jobTimer >= maxJobTime)
				{
					jobTimer = Time.realtimeSinceStartup;
					Debug.Log("PlaceVerts: " + MathExt.ToPercentage((i / (gridWidth + 1f)) + (j / (gridDepth + 1f)) * (1f / (gridWidth + 1f))));
					yield return null;
				}
			}
		}
		Debug.Log("[PlaceVerts] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	public IEnumerator SetUVs()
	{
		float startTime = Time.realtimeSinceStartup;
		uvs = new Vector2[vertices.Length];

		float jobTimer = Time.realtimeSinceStartup;
		for (int i = 0; i <= gridWidth; i++)
		{
			for (int j = 0; j <= gridDepth; j++)
			{
				uvs[(gridDepth + 1) * i + j] = new Vector2((float)i / gridWidth, (float)j / gridDepth);

				// Conditional yield (job timer)
				if (Time.realtimeSinceStartup - jobTimer >= maxJobTime)
				{
					jobTimer = Time.realtimeSinceStartup;
					Debug.Log("SetUVs: " + MathExt.ToPercentage((i / (gridWidth + 1f)) + (j / (gridDepth + 1f)) * (1f / (gridWidth + 1f))));
					yield return null;
				}
			}
		}
		Debug.Log("[SetUVs] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	// Offset vertices with perlin noise
	public IEnumerator OffsetVerts()
	{
		float startTime = Time.realtimeSinceStartup;

		FastNoiseLite noise = new();
		noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
		noise.SetFractalType(FastNoiseLite.FractalType.FBm);
		noise.SetSeed(noiseSeed);
		noise.SetFrequency(noiseFrequency);
		noise.SetFractalOctaves(noiseOctaves);
		noise.SetFractalLacunarity(noiseLacunarity);
		noise.SetFractalGain(noiseGain);

		float jobTimer = Time.realtimeSinceStartup;
		for (int i = 0; i < vertices.Length; i++)
		{
			// Simplex Noise
			Vector3 offsetVert = vertices[i] + transform.position;
			//vertices[i].y = noiseScale * noise.GetNoise(offsetVert.x, offsetVert.z) + noiseOffset;
			vertices[i].y = noiseScale * SimplexRemap(noise.GetNoise(offsetVert.x, offsetVert.z)) + noiseOffset;

			// Conditional yield (job timer)
			if (Time.realtimeSinceStartup - jobTimer >= maxJobTime)
			{
				jobTimer = Time.realtimeSinceStartup;
				Debug.Log("OffsetVerts: " + MathExt.ToPercentage((float)i / vertices.Length));
				yield return null;
			}
		}
		Debug.Log("[OffsetVerts] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	// Connect vertices into triangles
	public IEnumerator SetTris()
	{
		float startTime = Time.realtimeSinceStartup;
		triangles = new int[gridWidth * gridDepth * 6];

		float jobTimer = Time.realtimeSinceStartup;
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridDepth; j++)
			{
				triangles[(gridDepth * i + j) * 6 + 0] = (gridDepth + 1) * i + j;
				triangles[(gridDepth * i + j) * 6 + 1] = (gridDepth + 1) * i + j + 1;
				triangles[(gridDepth * i + j) * 6 + 2] = (gridDepth + 1) * (i + 1) + j;
				triangles[(gridDepth * i + j) * 6 + 3] = (gridDepth + 1) * i + j + 1;
				triangles[(gridDepth * i + j) * 6 + 4] = (gridDepth + 1) * (i + 1) + j + 1;
				triangles[(gridDepth * i + j) * 6 + 5] = (gridDepth + 1) * (i + 1) + j;

				// Conditional yield (job timer)
				if (Time.realtimeSinceStartup - jobTimer >= maxJobTime)
				{
					jobTimer = Time.realtimeSinceStartup;
					Debug.Log("SetTris: " + MathExt.ToPercentage(((float)i / gridWidth) + ((float)j / gridDepth) * (1f / gridWidth)));
					yield return null;
				}

			}
		}

		Debug.Log("[SetTris] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	// Assemble verts and tris into a mesh
	public void BuildMesh()
	{
		float startTime = Time.realtimeSinceStartup;
		mesh = new Mesh
		{
			indexFormat = IndexFormat.UInt32,
			vertices = vertices,
			uv = uvs,
			triangles = triangles
		};
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		GetComponent<MeshRenderer>().material = terrainMat;
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
		Debug.Log("[BuildMesh] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	public IEnumerator BuildTerrain()
	{
		float startTime = Time.realtimeSinceStartup;
		yield return StartCoroutine(PlaceVerts());
		yield return StartCoroutine(SetUVs());
		yield return StartCoroutine(OffsetVerts());
		yield return StartCoroutine(SetTris());
		BuildMesh();
		Debug.Log("[BuildTerrain] finished in " + (Time.realtimeSinceStartup - startTime) + " seconds");
	}

	[ContextMenu("Build Terrain")]
	public void BuildTerrainCall()
	{
		StartCoroutine(BuildTerrain());
	}

	[ContextMenu("Abort Build Terrain")]
	public void AbortBuildTerrain()
	{
		StopAllCoroutines();
	}

	// Placeholder function that remaps simplex heigh output to be distributed more like Earth's terrain (more low values)
	private float SimplexRemap(float x)
	{
		switch (remapMode)
		{
			case TerrainRemapMode.None:
				return x;	// Hills
			case TerrainRemapMode.Mountains:
				return Mathf.Pow(x + 1, 4) / 8 - 1;
			case TerrainRemapMode.Mesas:
				return 2.4f * (float)Math.Cbrt(0.1f * x - 0.08f) + 0.35f;
			case TerrainRemapMode.Plateaus:
				return 2.8f * (float)Math.Cbrt(0.05f * x - 0.02f) + 0.15f;
			case TerrainRemapMode.Canyons:
				return 2.8f * (float)Math.Cbrt(0.05f * x + 0.01f) - 0.05f;
			case TerrainRemapMode.Realistic:
				// Earth elevation distribution approximation
				if (x >= 0)
				{
					return 1.5f * MathF.Pow(x, 4);
				}
				else
				{
					return (MathF.Pow(2 * x + 0.9f, 3) / (2 * MathF.Pow(0.9f, 3))) - 0.5f;
				}
			default:
				return 0;
		}
    }

	[ContextMenu("SaveVerts")]
	public void SaveVertStats()
	{
		float[] heights = new float[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			heights[i] = vertices[i].y;
		}
		heights = heights.OrderBy(x => x).ToArray();

		string verts = "";
		foreach (float y in heights)
		{
			verts += y + "\n";
		}

		FileHandler.SaveString(verts, Application.dataPath + "/verts.csv", true);
	}

	public enum TerrainRemapMode
	{
		None = 0,
		Mountains,
		Mesas,
		Plateaus,
		Canyons,
		Realistic
	}
}
