using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartController : MonoBehaviour
{
	[Header("Data")]
	public BodyPartData data;

	[Header("Physical Attributes")]
	public float mass;	// kg

	[Header("UI")]
	public bool isSelected;

	[Header("Parent and Children")]
	public BodyPartController parent;
	public List<BodyPartController> children;

	[Header("Body Controller")]
	public BodyController bodyController;

	[Header("Clones")]
	public List<BodyPartController> clones;	// [0] is main part, other indices filled only in main part controller
	public int cloneIndex;

	[Header("Parts")]
	// Colliders
	public Collider raycastCollider;
	public Collider physicsCollider;
	// Visuals
	public GameObject highlightCube;
	public LineRenderer boneLineRenderer;
	// Holders
	public GameObject bulkHolder;

	// Private Fields
	private bool isSimulating;
	private ArticulationBody articulationBody;

	public Vector3 reflectVectorA;
	public Vector3 reflectVectorB;
	public float rotationOffsetAngle;
	public Quaternion rotationQuaternion;

	private void Awake()
	{
		articulationBody = GetComponent<ArticulationBody>();
		SetSelected(false);
		SetSimulating(false);
	}

	private void Update()
	{
		if (!isSimulating)
		{
			UpdateCalculations();
			UpdateVisuals();
		}
	}

	// Give this gameobject data and set properties related to it
	public void Initialize(BodyController bodyController, BodyPartData data, BodyPartController parentController, 
		BodyPartController mainPart = null, int cloneIndex = 0)
	{
		this.bodyController = bodyController;
		this.data = data;
		name = data.name;
		clones = new List<BodyPartController> { (mainPart == null) ? this : mainPart};

		// Truncated version of UpdateParent
		parent = parentController;
		if (parent == null)
		{
			transform.parent = bodyController.transform;
		}
		else
		{
			// Set new parent and add to its list of children
			parent.children.Add(this);

			transform.parent = parent.transform;
		}

		SetCloneIndex(cloneIndex);
	}

	public void SetCloneIndex(int newCloneIndex)
	{
		cloneIndex = newCloneIndex;

		switch (bodyController.data.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				reflectVectorA = Vector3.one;
				reflectVectorB = Vector3.one;
				rotationOffsetAngle = 0;
				rotationQuaternion = Quaternion.identity;
				break;
			case SymmetryType.Bilateral:
				reflectVectorA = (cloneIndex == 0) ? Vector3.one : new Vector3(-1, 1, 1);
				reflectVectorB = (cloneIndex == 0) ? Vector3.one : new Vector3(1, -1, -1);
				rotationOffsetAngle = 0;
				rotationQuaternion = Quaternion.identity;
				break;
			case SymmetryType.RadialRotate:
				reflectVectorA = Vector3.one;
				reflectVectorB = Vector3.one;
				if (parent != null)
				{
					if (parent.data.isCentered)
					{
						rotationOffsetAngle = 360f / bodyController.data.numSegments;
						rotationQuaternion = Quaternion.AngleAxis(cloneIndex * (rotationOffsetAngle), Vector3.forward);
						break;
					}
				}
				rotationOffsetAngle = 0;
				rotationQuaternion = Quaternion.identity;
				break;
			case SymmetryType.RadialFlip:
				break;
			default:
				break;
		}
	}

	public void UpdateCalculations()
	{
		mass = data.scale.x * data.scale.y * data.scale.z * data.density;
	}

	// Update visual Gameobjects
	public void UpdateVisuals()
	{
		Vector3 trueCenter = data.length / 2 * Vector3.forward;

		transform.localPosition = Vector3.Scale(data.position, reflectVectorA);
		transform.localEulerAngles = Vector3.Scale(data.rotation, reflectVectorB);
		transform.RotateAround(Vector3.zero, Vector3.up, cloneIndex * rotationOffsetAngle);

		if (parent == null)
		{
			boneLineRenderer.positionCount = 2;
			boneLineRenderer.SetPositions(new Vector3[] { Vector3.zero, data.length * Vector3.forward });
		}
		else
		{
			Vector3 parentDistalPosition = transform.InverseTransformPoint(parent.transform.position + parent.data.length * parent.transform.forward);
			boneLineRenderer.positionCount = 3;
			boneLineRenderer.SetPositions(new Vector3[] { parentDistalPosition, Vector3.zero, data.length * Vector3.forward });
		}

		bulkHolder.transform.localPosition = trueCenter + Vector3.Scale(data.bulkOffset, reflectVectorA);
		bulkHolder.transform.localScale = data.scale;
	}

	public void UpdateArticulationBody()
	{
		articulationBody.mass = mass;
		articulationBody.centerOfMass = data.scale.z / 2 * Vector3.forward;

		articulationBody.jointType = ArticulationJointType.SphericalJoint;
		articulationBody.twistLock = ArticulationDofLock.LimitedMotion;
		articulationBody.swingYLock = ArticulationDofLock.LimitedMotion;
		articulationBody.swingZLock = ArticulationDofLock.LimitedMotion;
		articulationBody.SetDriveLimits(ArticulationDriveAxis.X, data.jointLimits[0].x, data.jointLimits[1].x);
		articulationBody.SetDriveLimits(ArticulationDriveAxis.Y, data.jointLimits[0].y, data.jointLimits[1].y);
		articulationBody.SetDriveLimits(ArticulationDriveAxis.Z, data.jointLimits[0].z, data.jointLimits[1].z);
		articulationBody.SetDriveStiffness(ArticulationDriveAxis.X, data.stiffness.x);
		articulationBody.SetDriveStiffness(ArticulationDriveAxis.Y, data.stiffness.y);
		articulationBody.SetDriveStiffness(ArticulationDriveAxis.Z, data.stiffness.z);
	}

	// Select or deselect this part
	public void SetSelected(bool selected)
	{
		isSelected = selected;
		if (selected)
		{
			highlightCube.SetActive(true);
		}
		else
		{
			highlightCube.SetActive(false);
		}

		//if (parent != null)
		//{
		//	parent.HighlightAsParent(selected);
		//}

		//foreach (BodyPartController child in children)
		//{
		//	child.HighlightAsChild(selected);
		//}
	}

	public void HighlightAsParent(bool highlighted)
	{
		if (highlighted)
		{
			highlightCube.SetActive(true);
		}
		else
		{
			highlightCube.SetActive(false);
		}
	}

	public void HighlightAsChild(bool highlighted)
	{
		if (highlighted)
		{
			highlightCube.SetActive(true);
		}
		else
		{
			highlightCube.SetActive(false);
		}
	}

	// Enable or disable physics simulation mode
	public void SetSimulating(bool simulating)
	{
		foreach (BodyPartController clone in clones)
		{
			clone.isSimulating = simulating;

			clone.articulationBody.enabled = isSimulating;
			clone.physicsCollider.enabled = isSimulating;

			if (clone.isSimulating)
			{
				clone.SetSelected(false);
				clone.UpdateArticulationBody();
			}
		}
	}

	// Changes parentId data and updates parentage
	public void ChangeParent(BodyPartController newParentController)
	{
		if (newParentController == null)
		{
			// Set data
			data.parentId = -1;
		}
		else
		{
			// Set data
			data.parentId = newParentController.data.id;
		}

		UpdateParent(newParentController);
	}

	// Updates child/parent relationships and gameobject parentage
	public void UpdateParent(BodyPartController parentController)
	{
		if (parent != null)
		{
			// Remove from old parent's list of children
			parent.children.Remove(this);
		}

		if (parentController == null)
		{
			// Set new parent to null
			parent = null;

			transform.parent = bodyController.transform;
		}
		else
		{
			// Set new parent and add to its list of children
			parent = parentController;
			parentController.children.Add(this);

			transform.parent = parentController.transform;
		}

		// NOTE: the passed controller should always be the main part
		if (cloneIndex == 0)
		{
			bodyController.UpdateClones(this);
		}
	}

	// Merges clones together
	public void MergeClones()
	{
		foreach (BodyPartController clone in clones)
		{
			clone.parent = parent;
		}
	}
}

[Serializable]
public class BodyPartData
{
	[Header("Info")]
	public int id;
	public int parentId;
	public string name;

	[Header("Symmetry")]
	public bool isCentered;

	[Header("Physics")]
	public Vector3 position;	// meters, from parent proximal point
	public Vector3 rotation;	// degrees, from parent's forward vector
	public float length;		// meters, proximal to distal points
	public Vector3 bulkOffset;	// meters, from center between proximal and distal points
	public Vector3 scale;		// meters, bulk size
	public float density;		// kg/m^3

	[Header("Joint")]
	public Vector3[] jointLimits;	// Low, High
	public Vector3 stiffness;

	// Default data
	public BodyPartData(int id, int parentId)
	{
		this.id = id;
		this.parentId = parentId;
		name = "Body Part " + id;

		isCentered = false;

		position = Vector3.zero;
		rotation = Vector3.zero;
		length = 1;
		bulkOffset = Vector3.zero;
		scale = Vector3.one;
		density = 1000;

		jointLimits = new Vector3[2];
	}

	public BodyPartData(int id, int parentId, bool isCentered, Vector3 position, Vector3 rotation, Vector3 scale)
	{
		this.id = id;
		this.parentId = parentId;
		name = "Body Part " + id;

		this.isCentered = isCentered;
		
		this.position = position;
		this.rotation = rotation;
		length = scale.z;
		bulkOffset = Vector3.zero;
		this.scale = scale;
		density = 1000;

		jointLimits = new Vector3[2];
	}

	public BodyPartData(int id, int parentId, bool isCentered, Vector3 position, Vector3 rotation, float length, Vector3 bulkOffset, Vector3 scale, float density, Vector3[] jointLimits)
	{
		this.id = id;
		this.parentId = parentId;
		name = "Body Part " + id;

		this.isCentered = isCentered;

		this.position = position;
		this.rotation = rotation;
		this.length = length;
		this.bulkOffset = bulkOffset;
		this.scale = scale;
		this.density = density;

		this.jointLimits = jointLimits;
	}

	public BodyPartData Clone()
	{
		return new(id, parentId, isCentered, position, rotation, length, bulkOffset, scale, density, 
			new Vector3[] { jointLimits[0], jointLimits[1] });
	}
}