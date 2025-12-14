using UnityEngine;

public class SkinBuilder : MonoBehaviour
{
	[Header("Settings")]
	public int numSegmentsLongitude;	// Left-right
	public int numSegmentsLatitude;		// Down-up
	public Vector3 scale;
	public Material skinMaterial;

	private Vector3[] vertices;
	private Vector2[] uvs;
	private int[] triangles;
	private Mesh mesh;

	private void Start()
	{
		BuildSkin();
	}

	public void BuildSkin()
	{
		PlaceVerts();
		//SetUVs();
		SetTris();
		BuildMesh();
	}

	// Place vertices of the mesh
	public void PlaceVerts()
	{
		vertices = new Vector3[(numSegmentsLongitude) * (numSegmentsLatitude + 1)];

		for (int i = 0; i <= numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				float angle = (2 * Mathf.PI * j) / numSegmentsLongitude;
				float height = (scale.y * i) / numSegmentsLatitude;
				vertices[numSegmentsLongitude * i + j] = new Vector3(scale.x * Mathf.Cos(angle), height, scale.z * Mathf.Sin(angle));
			}
		}
	}

	public void SetUVs()
	{
		uvs = new Vector2[vertices.Length];

		for (int i = 0; i <= numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				uvs[(numSegmentsLongitude + 1) * i + j] = new Vector2((float)i / numSegmentsLatitude, (float)j / numSegmentsLongitude);
			}
		}
	}

	// Connect vertices into triangles
	public void SetTris()
	{
		triangles = new int[numSegmentsLatitude * numSegmentsLongitude * 6];

		for (int i = 0; i < numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				int a = numSegmentsLongitude * i + j;
				int b = numSegmentsLongitude * (i + 1) + j;
				int c = numSegmentsLongitude * i + (j + 1) % numSegmentsLongitude;
				int d = numSegmentsLongitude * (i + 1) + (j + 1) % numSegmentsLongitude;

				triangles[(numSegmentsLongitude * i + j) * 6 + 0] = a;
				triangles[(numSegmentsLongitude * i + j) * 6 + 1] = b;
				triangles[(numSegmentsLongitude * i + j) * 6 + 2] = c;
				triangles[(numSegmentsLongitude * i + j) * 6 + 3] = c;
				triangles[(numSegmentsLongitude * i + j) * 6 + 4] = b;
				triangles[(numSegmentsLongitude * i + j) * 6 + 5] = d;
			}
		}
	}

	// Assemble verts and tris into a mesh
	public void BuildMesh()
	{
		mesh = new Mesh
		{
 			vertices = vertices,
			uv = uvs,
			triangles = triangles
		};
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		GetComponent<MeshRenderer>().material = skinMaterial;
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
	}
}
