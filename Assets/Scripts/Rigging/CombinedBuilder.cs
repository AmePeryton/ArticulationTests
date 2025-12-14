using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CombinedBuilder : MonoBehaviour
{
	[Header("Data")]
	public string fileName;
	public BodyData bodyData;

	[Header("Bones")]
	public GameObject rigObject;
	public BoneRenderer boneRenderer;
	public List<Transform> bones;
	public List<Matrix4x4> bindPoses;
	public Dictionary<int, Transform> idBoneDict;

	[Header("Skin")]
	public int numSegmentsLongitude;    // Left-right
	public int numSegmentsLatitude;     // Down-up
	public GameObject skinObject;
	public Material skinMaterial;
	public List<Vector3> verts;
	public List<Vector2> uvs;
	public List<int> tris;
	public List<BoneWeight> boneWeights;
	public Mesh mesh;

	private void Awake()
	{
		Setup();
	}

	private void Start()
	{
		BuildCombined();
	}

	[ContextMenu("Setup")]
	public void Setup()
	{
		bones = new List<Transform>();
		bindPoses = new List<Matrix4x4>();
		idBoneDict = new Dictionary<int, Transform>();
	}

	[ContextMenu("BuildCombined")]
	public void BuildCombined()
	{
		LoadBodyData();
		BuildSkeleton();
		BuildSkin();
	}

	public void LoadBodyData()
	{
		bodyData = FileHandler.Load<BodyData>(Application.dataPath + "/" + fileName + ".save", true);
	}

	public void ClearSkeleton()
	{
		foreach (Transform t in bones)
		{
			Destroy(t.gameObject);
		}
		bones.Clear();
		bindPoses.Clear();
		idBoneDict.Clear();

		boneRenderer.Reset();
	}

	public void BuildSkeleton()
	{
		if (bodyData == null)
		{
			Debug.Log("No data");
			return;
		}

		ClearSkeleton();

		// Make bones for the root and children of the root
		MakeChildBones(bodyData.bodyParts.Find(x => x.parentId == -1), transform);
	}

	public void MakeChildBones(BodyPartData part, Transform parentBone)
	{
		// Make this bone
		Transform partBone = MakeNewBone(parentBone, part.name, part.position, Quaternion.Euler(part.rotation));
		idBoneDict.Add(part.id, partBone);

		List<BodyPartData> childParts = bodyData.bodyParts.FindAll(x => x.parentId == part.id);
		if (childParts.Count > 0)
		{
			foreach (BodyPartData child in childParts)
			{
				// Make child bones
				MakeChildBones(child, partBone);
			}
		}
		else
		{
			// Make tip bone
			MakeNewBone(partBone, part.name + " tip", part.length * Vector3.forward, Quaternion.identity);
		}
	}

	public Transform MakeNewBone(Transform parentBone, string name, Vector3 localPosition, Quaternion localRotation)
	{
		GameObject newBoneObject = new(name);
		Transform newBone = newBoneObject.transform;
		newBone.parent = parentBone;
		newBone.SetLocalPositionAndRotation(localPosition, localRotation);
		bones.Add(newBone);
		bindPoses.Add(newBone.worldToLocalMatrix * transform.localToWorldMatrix);
		return newBone;
	}

	public void ClearSkin()
	{
		verts = new();
		uvs = new();
		tris = new();
		boneWeights = new List<BoneWeight>();
	}

	public void BuildSkin()
	{
		// NOTE: requires bones to be built first
		if (bodyData == null)
		{
			Debug.Log("No data");
			return;
		}

		ClearSkin();

		// Make skin sections for the root and children of the root
		MakeChildSkins(bodyData.bodyParts.Find(x => x.parentId == -1));

		BuildMesh();
	}

	public void MakeChildSkins(BodyPartData part)
	{
		// Make this skin section
		MakeNewSkin(part);

		List<BodyPartData> childParts = bodyData.bodyParts.FindAll(x => x.parentId == part.id);
		foreach (BodyPartData child in childParts)
		{
			// Make child skin
			MakeChildSkins(child);
		}
	}

	public void MakeNewSkin(BodyPartData part)
	{
		int startIndex = SetVerts(part);
		SetTris(startIndex);
	}

	// Place vertices of the mesh and set bone weights
	public int SetVerts(BodyPartData part)
	{
		int startIndex = verts.Count;
		Vector3[] newVerts = new Vector3[numSegmentsLongitude * (numSegmentsLatitude + 1)];
		Transform partBone = idBoneDict[part.id];

		// Place vertices for this body part
		for (int i = 0; i <= numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				float angle = -(2 * Mathf.PI * j) / numSegmentsLongitude;
				float height = (part.scale.z * i) / numSegmentsLatitude;
				newVerts[numSegmentsLongitude * i + j] = new Vector3(part.scale.x / 2 * Mathf.Cos(angle), part.scale.y / 2 * Mathf.Sin(angle), height) + part.bulkOffset;
			}
		}

		partBone.TransformPoints(newVerts);

		verts.AddRange(newVerts);

		// Add bone weights
		BoneWeight[] newBoneWeights = new BoneWeight[newVerts.Count()];
		for (int i = 0; i < newBoneWeights.Length; i++)
		{
			newBoneWeights[i].boneIndex0 = bones.IndexOf(partBone);
			newBoneWeights[i].weight0 = 1;
		}
		boneWeights.AddRange(newBoneWeights);

		return startIndex;
	}

	/*public void SetUVs()
	{
		uvs = new Vector2[vertices.Length];

		for (int i = 0; i <= numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				uvs[(numSegmentsLongitude + 1) * i + j] = new Vector2((float)i / numSegmentsLatitude, (float)j / numSegmentsLongitude);
			}
		}
	}*/

	// Connect vertices into triangles
	public void SetTris(int startIndex)
	{
		int[] newTris = new int[numSegmentsLatitude * numSegmentsLongitude * 6];

		for (int i = 0; i < numSegmentsLatitude; i++)
		{
			for (int j = 0; j < numSegmentsLongitude; j++)
			{
				int a = startIndex + numSegmentsLongitude * i + j;
				int b = startIndex + numSegmentsLongitude * (i + 1) + j;
				int c = startIndex + numSegmentsLongitude * i + (j + 1) % numSegmentsLongitude;
				int d = startIndex + numSegmentsLongitude * (i + 1) + (j + 1) % numSegmentsLongitude;

				newTris[(numSegmentsLongitude * i + j) * 6 + 0] = a;
				newTris[(numSegmentsLongitude * i + j) * 6 + 1] = b;
				newTris[(numSegmentsLongitude * i + j) * 6 + 2] = c;
				newTris[(numSegmentsLongitude * i + j) * 6 + 3] = c;
				newTris[(numSegmentsLongitude * i + j) * 6 + 4] = b;
				newTris[(numSegmentsLongitude * i + j) * 6 + 5] = d;
			}
		}

		tris.AddRange(newTris);
	}

	// Assemble verts and tris into a mesh
	public void BuildMesh()
	{
		mesh = new Mesh
		{
			vertices = verts.ToArray(),
			uv = uvs.ToArray(),
			triangles = tris.ToArray(),
			bindposes = bindPoses.ToArray(),
			boneWeights = boneWeights.ToArray()
		};
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		SkinnedMeshRenderer smr = skinObject.GetComponent<SkinnedMeshRenderer>();
		smr.material = skinMaterial;
		smr.bones = bones.ToArray();
		smr.sharedMesh = mesh;
		smr.rootBone = idBoneDict[bodyData.bodyParts.Find(x => x.parentId == -1).id];
		smr.localBounds = mesh.bounds;
		boneRenderer.transforms = bones.ToArray();
	}
}

// TODO: make symmetrical