using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SkeletonBuilder : MonoBehaviour
{
	public List<Transform> bones;
	public List<Matrix4x4> bindPoses;
	public string fileName;
	public BodyData bodyData;
	public Dictionary<int, Transform> idBoneDict;
	public BoneRenderer boneRenderer;

	private void Awake()
	{
		Setup();
	}

	private void Start()
	{
		//LoadBodyData();
		//MakeSkeleton();
	}

	[ContextMenu("Setup")]
	public void Setup()
	{
		bones = new List<Transform>();
		bindPoses = new List<Matrix4x4>();
		idBoneDict = new Dictionary<int, Transform>();
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

	[ContextMenu("MakeSkeleton")]
	public void MakeSkeleton()
	{
		LoadBodyData();

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
		Transform partBone = MakeNewBone(parentBone, part.name, part.position, Quaternion.Euler(part.rotation));
		idBoneDict.Add(part.id, partBone);

		List<BodyPartData> childParts = bodyData.bodyParts.FindAll(x => x.parentId == part.id);

		if (childParts.Count > 0)
		{
			foreach (BodyPartData child in childParts)
			{
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
}
