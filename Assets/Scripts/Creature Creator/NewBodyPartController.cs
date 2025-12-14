using System;
using System.Collections.Generic;
using UnityEngine;

public class NewBodyPartController : MonoBehaviour
{
	[Header("Data")]
	public ConcreteBodyPartData data;

	[Header("Body Controller")]
	public NewBodyController bodyController;

	[Header("GameObject Components")]
	public GameObject bulkHolder;
	public GameObject highlightCube;
	public LineRenderer boneLineRenderer;
	public Collider surfaceCollider;    // Mainly used for raycasts

	private void Update()
	{
		UpdateVisuals();
	}

	public void Initialize(ConcreteBodyPartData newData, NewBodyController newBodyController)
	{
		data = newData;
		bodyController = newBodyController;
		gameObject.name = "Body Part [s" + data.sRef.id + "]";

		highlightCube.SetActive(false);
	}

	public void UpdateVisuals()
	{
		transform.localPosition = data.position;
		transform.localEulerAngles = data.rotation;

		bulkHolder.transform.localPosition = data.bulkOffset;
		bulkHolder.transform.localScale = data.scale;

		// Update bone line renderer
		boneLineRenderer.positionCount = 2;
		boneLineRenderer.SetPositions(new Vector3[] { Vector3.zero, (0.5f * data.scale.z + data.bulkOffset.z) * Vector3.forward });
	}

	public void SetSelected(bool isSelected)
	{
		if (isSelected)
		{
			highlightCube.SetActive(true);
		}
		else
		{
			highlightCube.SetActive(false);
		}
	}	
}

[Serializable]	// Ironic
public class ConcreteBodyPartData
{
	[Header("Physics")]
	public Vector3 position;    // meters, from parent proximal point
	public Vector3 rotation;    // degrees, from parent's forward vector
	public Vector3 scale;       // meters, bulk size
	public Vector3 bulkOffset;  // meters, from center between proximal and distal points

	[Header("Refs")]
	public SerializedBodyPartData sRef;
	public ConcreteBodyPartData parent;
	[NonSerialized]
	public List<ConcreteBodyPartData> children;
	public int repIndex;	// The exact repetition that this concrete part represents
	// repIndex == 0 when it is the first rep, and all physics data is the same without being transformed

	public ConcreteBodyPartData(SerializedBodyPartData sRef, ConcreteBodyPartData parent, int repIndex)
	{
		this.repIndex = repIndex;
		position = sRef.position;
		rotation = sRef.rotation;
		scale = sRef.scale;
		bulkOffset = sRef.bulkOffset;

		this.sRef = sRef;
		this.parent = parent;
		children = new();
	}

	public override string ToString()
	{
		return base.ToString();
	}

	// Returns the repIndexChain for the given concrete part
	// Essentially, gives turn by turn directions to find this specific concrete part when starting at the root
	public List<int> GetRepIndexChain()
	{
		List<int> output = new();

		ConcreteBodyPartData curr = this;

		while (curr.parent != null)
		{
			output.Add(curr.repIndex);
			curr = curr.parent;
		}
		// Add repIndex for the root
		output.Add(curr.repIndex);

		output.Reverse();

		return output;
	}

	// Checks if there are an odd number of bilaterally reflected parts in the part's parentage including the target part
	// Usefull for radially symmetric parts nested under bilaterally symmetric parts
	public bool IsSpaceFlipped()
	{
		bool output = false;
		ConcreteBodyPartData curr = this;
		do
		{
			if (curr.sRef.symmetryType == SymmetryType.Bilateral && curr.repIndex != 0)
			{
				output = !output;
			}
			curr = curr.parent;
		}
		while (curr != null);

		return output;
	}
}

[Serializable]
public class SerializedBodyPartData
{
	[Header("Info")]
	public int id;

	[Header("Hierarchy")]
	public int parentId;
	public SymmetryType symmetryType;
	public bool isAxial;
	public int numReps;
	public Vector3 plaxisDirection; // normal direction of the plane of symmetry, or the direction of the axis of symmetry
	public Vector3 plaxisPoint;     // a point that the plane / axis of symmetry passes through
	// Symmetry breaking mask? Maybe later after extensive testing

	[Header("Physics")]
	public Vector3 position;    // meters, from parent proximal point
	public Vector3 rotation;    // degrees, from parent's forward vector
	public Vector3 scale;       // meters, bulk size
	public Vector3 bulkOffset;  // meters, from center between proximal and distal points

	// Maximum allowed distance to be considered on the plane / axis
	private const float ad = 0.000001f;
	// Maximum allowed angle to be considered on the plane / axis
	private const float aa = 0.00005f;

	// Default data
	public SerializedBodyPartData(int id, int parentId)
	{
		this.id = id;

		this.parentId = parentId;
		symmetryType = SymmetryType.Asymmetrical;
		isAxial = false;
		numReps = 1;
		plaxisDirection = Vector3.forward;
		plaxisPoint = Vector3.zero;

		position = Vector3.zero;
		rotation = Vector3.zero;
		scale = Vector3.one;
		bulkOffset = Vector3.zero;
	}

	public override string ToString()
	{
		return base.ToString();
	}

	// Instead of changing data, just return if the data is already compliant or not
	public bool ValidateSymmetryRules()
	{
		// Ensure plaxis is valid
		// NOTE: maybe use the zero vector plaxis direction to indicate that the part uses it's parent part's plaxis?????
		if (plaxisDirection.magnitude == 0)
		{
			Debug.Log("Plaxis is the zero vector! [ " + id + " ]");
			return false;
		}

		/* Symmetry rules:
		 * Asymmetrical: 1 rep, isAxial = false
		 * Bilateral Axial: 1 rep, position + rotation + bulk offset aligned to plane
		 * Bilateral Nonaxial: 2 reps
		 * Radial Axial: 1 rep, position + rotation + bulk offset + scale aligned to axis
		 * Radial Nonaxial: >=2 reps */
		switch (symmetryType)
		{
			case SymmetryType.Asymmetrical:
				if (!isAxial && numReps == 1)
				{
					return true;
				}
				else
				{
					Debug.Log("Invalid numReps! [ " + id + " ]");
					return false;
				}
			case SymmetryType.Bilateral:
				if (isAxial)
				{
					// Check that numReps is exactly 1
					if (numReps != 1)
					{
						Debug.Log("Invalid numReps! [ " + id + " ]");
						return false;
					}

					// Check that the position is on the plane
					if(!MathExt.IsPointOnPlane(plaxisDirection, plaxisPoint, position, ad))
					{
						Debug.Log("Invalid position! [ " + id + " ]");
						return false;
					}

					// Check that the rotation is on the plane
					if (!MathExt.IsRotationOnPlane(plaxisDirection, rotation, aa))
					{
						Debug.Log("Invalid rotation! [ " + id + " ]");
						return false;
					}
					
					// Check that the bulk offset is on the plane
					if (bulkOffset.x != 0)
					{
						Debug.Log("Invalid bulkOffset.x! [ " + id + " ]");
						return false;
					}

					return true;
				}
				else
				{
					if (numReps == 2)
					{
						return true;
					}
					else
					{
						Debug.Log("Invalid numReps! [ " + id + " ]");
						return false;
					}
				}
			case SymmetryType.RadialRotate:
				if (isAxial)
				{
					// Check that numReps is exactly 1
					if (numReps != 1)
					{
						Debug.Log("Invalid numReps! [ " + id + " ]");
						return false;
					}

					// Check that the position is on the axis
					if (!MathExt.IsPointOnAxis(plaxisDirection, plaxisPoint, position, ad))
					{
						Debug.Log("Invalid position! [ " + id + " ]");
						return false;
					}

					// Check that the rotation is on the axis
					if (!MathExt.IsRotationOnAxis(plaxisDirection, rotation, aa))
					{
						Debug.Log("Invalid rotation! [ " + id + " ]");
						return false;
					}

					// Check that the X and Y scales are equal
					if (scale.x != scale.y)
					{
						Debug.Log("Invalid scale! [ " + id + " ]");
						return false;
					}

					// Check that the bulk offset is on the axis
					if (bulkOffset.x != 0 || bulkOffset.y != 0)
					{
						Debug.Log("Invalid bulkOffset.x! [ " + id + " ]");
						return false;
					}

					return true;
				}
				else
				{
					if (numReps < 2)
					{
						Debug.Log("Too few reps! [ " + id + " ]");
						return false;
					}
					if (numReps > 12)	// Restrict reps to a maximum of 12 for now
					{
						Debug.Log("Too many reps! [ " + id + " ]");
						return false;
					}
					return true;
				}
			default:
				Debug.Log("Unknown symmetry type! [ " + id + " ]");
				return false;
		}
	}
}

/* Notes:
 * NEVER reference a monobehaviour in a data class, even concrete data, 
	because the gameobject and associated monobehaviour are easy to accidentally delete while changing data
 */