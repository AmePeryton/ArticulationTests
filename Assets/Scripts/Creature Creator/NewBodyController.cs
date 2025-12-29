using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewBodyController : MonoBehaviour
{
	[Header("Data")]
	public ConcreteBodyData data;
	// Get the controller from the concrete part
	public Dictionary<ConcreteBodyPartData, NewBodyPartController> bodyPartControllerDict;

	[Header("Prefabs")]
	public GameObject bodyPartPrefab;

	private void Awake()
	{
		bodyPartControllerDict = new();
	}

	// Creates new serialized and concrete bodies
	public void NewBody()
	{
		Reset();
		SerializedBodyData newSBody = new();
		// Makes a root part
		SerializedBodyPartData newSBodyPart = new(0, -1)
		{
			rotation = -90f * Vector3.right,
			scale = 0.5f * Vector3.one,
		};
		newSBody.sBodyParts.Add(newSBodyPart);
		newSBody.sBodyPartIndex++;
		data = new(newSBody);
		// The ConcreteBodyData constructor automatically constructs the concrete body,
		// so ConstructConcreteBody does not need to be called here

		MakeBodyPartControllers();
	}

	public void Save(string filePath)
	{
		data.Save(filePath);
	}

	public void Load(string filePath)
	{
		data.Load(filePath);
		MakeBodyPartControllers();
	}

	// Resets the GameObject representations of the concrete body, but not the data of the serialized or concrete body
	public void Reset()
	{
		foreach (NewBodyPartController controller in bodyPartControllerDict.Values)
		{
			Destroy(controller.gameObject);
		}

		bodyPartControllerDict.Clear();
	}

	// Constructs the concrete body from the serialized body and makes controllers
	public void ConstructConcreteBodyAndControllers()
	{
		data.ConstructConcreteBody();
		MakeBodyPartControllers();
	}

	public void MakeBodyPartControllers()
	{
		float timer = Time.realtimeSinceStartup;

		Reset();

		List<ConcreteBodyPartData> roots = data.GetRoots().ToList();
		foreach (ConcreteBodyPartData root in roots)
		{
			foreach (ConcreteBodyPartData cBodyPart in data.PreOrder(root))
			{
				NewBodyPartController newPartController = Instantiate(bodyPartPrefab).GetComponent<NewBodyPartController>();
				bodyPartControllerDict.Add(cBodyPart, newPartController);
				if (cBodyPart.parent != null)
				{
					newPartController.transform.parent = bodyPartControllerDict[cBodyPart.parent].transform;
				}
				else
				{
					newPartController.transform.parent = transform;
				}
				newPartController.Initialize(cBodyPart, this);
			}
		}
		//Debug.Log("MakeBodyPartControllers: " + (Time.realtimeSinceStartup - timer) + " seconds");
	}
}

// Note that while the body data classes are in this file, they are used by other monobehaviours besides this one
// The body data type that is constructed from the serialized version, and used for things like rigging and in-game representation
[Serializable]	// Ironic
public class ConcreteBodyData
{
	public SerializedBodyData sBody;
	public List<ConcreteBodyPartData> cBodyParts;
	// Get the concrete part from the serialized part and rep index
	//public Dictionary<SerializedBodyPartData, List<ConcreteBodyPartData>> testDict;
	// NOTE: doesn't work quite right because of nested symmetry and repetitions,
	// but maybe a dictionary of a different kind could still be useful later

	public ConcreteBodyData(SerializedBodyData sBodyData)
	{
		sBody = sBodyData;

		ConstructConcreteBody();
	}

	public override string ToString()
	{
		return base.ToString();
	}

	// Returns the root concrete part(s) of the body (there may be more than 1 if it is symmetric and nonaxial)
	public IEnumerable<ConcreteBodyPartData> GetRoots()
	{
		foreach (ConcreteBodyPartData cRoot in GetConcreteBodyParts(sBody.GetRoot()))
		{
			// Yield return every concrete part that represents the serialized root part
			yield return cRoot;
		}
	}

	// Returns the concrete part(s) associated with the given serialized part
	public IEnumerable<ConcreteBodyPartData> GetConcreteBodyParts(SerializedBodyPartData serializedPart)
	{
		foreach (ConcreteBodyPartData part in cBodyParts)
		{
			if (part.sRef == serializedPart)
			{
				yield return part;
			}
		}
	}

	// Returns the concrete part from the specified info, or null if the part does not exist
	public ConcreteBodyPartData GetConcreteBodyPartFromInfo(SerializedBodyPartData serializedPart, List<int> repIndexChain)
	{
		// NOTE: the repChainIndex is the indices of every long parent of this part, in order from root to leaf,
		// then the repIndex of this part itself
		List<SerializedBodyPartData> sHierarchy = sBody.GetLongParents(serializedPart).ToList();
		// Add the part itself to the list as well, since it is analyzed the same as the parents
		sHierarchy.Add(serializedPart);
		// Make sure the lists' lengths are correct
		if (repIndexChain.Count != sHierarchy.Count)
		{
			Debug.Log("Invalid repIndex chain!");
			return null;
		}

		// First pointer is at the root with the first repIndex value

		ConcreteBodyPartData curr = GetRoots().ToList().Find(x => x.repIndex == repIndexChain[0]);
		for (int i = 1; i < repIndexChain.Count; i++)
		{
			curr = curr.children.Find(x => x.sRef == sHierarchy[i] && x.repIndex == repIndexChain[i]);
			if (curr == null)
			{
				Debug.Log("Could not find the specified concrete body part!");
				break;
			}
		}

		return curr;
	}

	public IEnumerable<ConcreteBodyPartData> PreOrder(ConcreteBodyPartData part)
	{
		yield return part;

		foreach (ConcreteBodyPartData child in part.children)
		{
			foreach (ConcreteBodyPartData x in PreOrder(child))
			{
				yield return x;
			}
		}
	}

	public IEnumerable<ConcreteBodyPartData> PostOrder(ConcreteBodyPartData part)
	{
		foreach (ConcreteBodyPartData child in part.children)
		{
			foreach (ConcreteBodyPartData x in PostOrder(child))
			{
				yield return x;
			}
		}

		yield return part;
	}

	public void Save(string filePath)
	{
		sBody.Save(filePath);
	}

	public void Load(string filePath)
	{
		SerializedBodyData newSBody = SerializedBodyData.Load(filePath);

		// If the loaded body data is null, don't change the saved data
		if (newSBody == null)
		{
			Debug.Log("Could not load the new concrete body: the serialized data is null!");
			return;
		}

		sBody = newSBody;
		ConstructConcreteBody();
	}

	// Public method to reload the entire concrete body from the serialized data
	public void ConstructConcreteBody()
	{
		cBodyParts = new();

		if(sBody.ValidateBodyStructure())
		{
			SerializedToConcreteRecursive(sBody.GetRoot(), null, false);
		}
		else
		{
			Debug.Log("Could not construct the concrete body: the serialized body structure is not valid!");
		}
	}

	// Creates concrete part(s) from serialized part and its children, adds to heirarchy
	private void SerializedToConcreteRecursive(SerializedBodyPartData sRef, ConcreteBodyPartData cParent, bool isXFlipped)
	{
		// For each repetition of this part
		for (int i = 0; i < sRef.numReps; i++)
		{
			// New variable copy so that changes to it only affect the part's children, not its siblings
			bool flip = isXFlipped;
			Vector3 flipA = Vector3.one;	// For positions
			Vector3 flipB = Vector3.one;	// For rotation
			if (flip)
			{
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}

			// Make concrete part
			ConcreteBodyPartData newCBodyPart = new(sRef, cParent, i);

			// Add self to parent
			cParent?.children.Add(newCBodyPart);

			// Add self to dict
			cBodyParts.Add(newCBodyPart);

			// Part symmetry
			if (!sRef.isAxial)
			{
				// Bilateral part mirroring
				if (sRef.symmetryType == SymmetryType.Bilateral && i == 1)
				{
					flip = !flip;

					// Get local rotation in Quaternion form
					Quaternion q = Quaternion.Euler(sRef.rotation);

					// Get orginal forward vector in local space
					Vector3 fwd = q * Vector3.forward;
					// Get orginal up vector in local space
					Vector3 up = q * Vector3.up;

					// Reflect fwd
					Vector3 newFwd = 2 * new Plane(sRef.plaxisDirection, Vector3.zero).ClosestPointOnPlane(fwd) - fwd;
					// Reflect up
					Vector3 newUp = 2 * new Plane(sRef.plaxisDirection, Vector3.zero).ClosestPointOnPlane(up) - up;

					// Reflect local position
					newCBodyPart.position = 2 * new Plane(sRef.plaxisDirection, sRef.plaxisPoint).ClosestPointOnPlane(sRef.position) - sRef.position;
					// Reflect local rotation
					newCBodyPart.rotation = Quaternion.LookRotation(newFwd, newUp).eulerAngles;
					// Set reflected bulk offset
					newCBodyPart.bulkOffset = Vector3.Scale(new Vector3(-1, 1, 1), sRef.bulkOffset);
				}

				// Radial part revolving
				if (sRef.symmetryType == SymmetryType.RadialRotate)
				{
					// Get revolution in quaternion form
					Quaternion r = Quaternion.AngleAxis(i * 360f / sRef.numReps, sRef.plaxisDirection);

					// Revolve position about axis
					newCBodyPart.position = r * (sRef.position - sRef.plaxisPoint) + sRef.plaxisPoint;

					// Get local rotation in Quaternion form
					Quaternion q = Quaternion.Euler(sRef.rotation);
					// Get orginal forward vector in local space
					Vector3 fwd = q * Vector3.forward;
					// Get orginal up vector in local space
					Vector3 up = q * Vector3.up;

					// Revolve fwd
					Vector3 newFwd = r * fwd;
					// Revolve up
					Vector3 newUp = r * up;

					// Revolve local rotation
					newCBodyPart.rotation = Quaternion.LookRotation(newFwd, newUp).eulerAngles;
				}
			}

			// Apply space flipping vectors (if space is not flipped, does nothing);
			newCBodyPart.position = Vector3.Scale(flipA, newCBodyPart.position);
			newCBodyPart.rotation = Vector3.Scale(flipB, newCBodyPart.rotation);
			newCBodyPart.bulkOffset = Vector3.Scale(flipA, newCBodyPart.bulkOffset);

			// For each child of the serialized part
			List<SerializedBodyPartData> sChildParts = sBody.GetChildren(sRef).ToList();
			foreach (SerializedBodyPartData sChild in sChildParts)
			{
				SerializedToConcreteRecursive(sChild, newCBodyPart, flip);
			}
		}
	}

	public void UpdateConcreteBody()
	{

	}
}

// The body data type that is saved to files and used as instructions to build concrete versions of the data
[Serializable]
public class SerializedBodyData
{
	public int sBodyPartIndex;	// Not sure if I still need this, orgiginally used to assign unique IDs
	public List<SerializedBodyPartData> sBodyParts;	// List of parts, independent of symmetry or hierarchy

	// Default data
	public SerializedBodyData()
	{
		sBodyPartIndex = 0;
		sBodyParts = new();
	}

	public override string ToString()
	{
		return base.ToString();
	}

	// Returns the root serialized part of the body (there should always be exactly 1)
	public SerializedBodyPartData GetRoot()
	{
		foreach (SerializedBodyPartData part in sBodyParts)
		{
			if (part.parentId == -1)
			{
				return part;
			}
		}

		Debug.LogError("No root part found!");
		return null;
	}

	// Returns all immediate children of a given part
	public SerializedBodyPartData GetBodyPart(int id)
	{
		foreach (SerializedBodyPartData potentialBodyPart in sBodyParts)
		{
			if (potentialBodyPart.id == id)
			{
				return potentialBodyPart;
			}
		}

		Debug.Log("No part found!");
		return null;
	}

	// Returns the parent of the part
	public SerializedBodyPartData GetParent(SerializedBodyPartData part)
	{
		if (part.parentId == -1)
		{
			Debug.Log("This part is the root! [ " + part.id + " ]");
			return null;
		}

		SerializedBodyPartData output = GetBodyPart(part.parentId);
		if (output == null)
		{
			Debug.LogError("No parent part found! [" + part.parentId + " ]");
		}

		return output;
	}

	// Returns list of parent and grandparents, etc. to the root
	public IEnumerable<SerializedBodyPartData> GetLongParents(SerializedBodyPartData part)
	{
		// Check that the structure is valid first
		if (!ValidateBodyStructure())
		{
			Debug.Log("Invalid body structure!");
			yield break;
		}

		// Check that this part still exists in the body
		if (!sBodyParts.Contains(part))
		{
			Debug.Log("The part is no longer a part of the body!");
			yield break;
		}

		List<SerializedBodyPartData> output = new();
		SerializedBodyPartData curr = part;

		while (curr.parentId != -1)
		{
			curr = GetParent(curr);
			output.Add(curr);
		}

		output.Reverse();

		foreach (SerializedBodyPartData x in output)
		{
			yield return x;
		}
	}

	// Returns all immediate children of a given part
	public IEnumerable<SerializedBodyPartData> GetChildren(SerializedBodyPartData part)
	{
		foreach (SerializedBodyPartData potentialChildPart in sBodyParts)
		{
			if (potentialChildPart.parentId == part.id)
			{
				yield return potentialChildPart;
			}
		}
	}

	// Returns unordered list of all children, grandchildren, etc. of a part (likely faster than preorder and postorder)
	public IEnumerable<SerializedBodyPartData> GetLongChildren(SerializedBodyPartData part)
	{
		// Check that the structure is valid first
		if (!ValidateBodyStructure())
		{
			Debug.Log("Invalid body structure!");
			yield break;
		}

		SerializedBodyPartData root = GetRoot();

		// Yield return body parts with the given part in their long parentage
		foreach (SerializedBodyPartData potentialChildPart in sBodyParts)
		{
			// Set the current pointer at the part being analyzed
			SerializedBodyPartData curr = potentialChildPart;

			// Try to trace a path back to the root from each body part
			while (curr != root)
			{
				curr = GetParent(curr);

				// If it runs into the given part, yield return it
				if (curr == part)
				{
					yield return potentialChildPart;
					break;
				}
			}
		}
	}

	// Returns long children of the part in root to leaf order (possibly slow due to iterating through the list many times)
	public IEnumerable<SerializedBodyPartData> PreOrder(SerializedBodyPartData part)
	{
		yield return part;

		foreach (SerializedBodyPartData child in GetChildren(part))
		{
			foreach (SerializedBodyPartData x in PreOrder(child))
			{
				yield return x;
			}
		}
	}

	// Returns long children of the part in leaf to root order (possibly slow due to iterating through the list many times)
	public IEnumerable<SerializedBodyPartData> PostOrder(SerializedBodyPartData part)
	{
		foreach (SerializedBodyPartData child in GetChildren(part))
		{
			foreach (SerializedBodyPartData x in PostOrder(child))
			{
				yield return x;
			}
		}

		yield return part;
	}

	// Check that the body is correclty structured as a tree with a single root and with unique IDs
	public bool ValidateBodyStructure()
	{
		// Unique IDs
		foreach (SerializedBodyPartData part in sBodyParts)
		{
			// Check for duplicate IDs in list
			if (sBodyParts.FindAll(x => x.id == part.id).Count > 1)
			{
				Debug.Log("Duplicate body part IDs! [ " + part.id + " ]");
				return false;
			}
		}

		// Exactly 1 root
		List<SerializedBodyPartData> roots = sBodyParts.FindAll(x => x.parentId == -1);
		if (roots.Count != 1)
		{
			// If there are 0 roots or more than 1 root, return false
			Debug.Log("Invalid number of roots! ( " + roots.Count + " )");
			return false;
		}

		// All parent paths lead back to the root (no circular references)
		foreach (SerializedBodyPartData part in sBodyParts)
		{
			// Set the current pointer at the part being analyzed
			SerializedBodyPartData curr = part;

			// Try to trace a path back to the root from each body part
			while (curr != roots[0])
			{
				curr = GetParent(curr);

				// If it can't find the referenced parent from the parentID, the parentID must be invalid, return false
				if (curr == null)
				{
					Debug.Log("No valid parent found!");
					return false;
				}

				// If it runs into the original part again, the path must be circular, return false
				if (curr == part)
				{
					Debug.Log("Circular parentage detected! [ " + curr.id + " ]");
					return false;
				}
			}
		}

		// If passed the above tests, structure is valid, return true
		return true;
	}

	// Check that all body parts are symmetry compliant
	public bool ValidateBodySymmetry()
	{
		foreach (SerializedBodyPartData part in sBodyParts)
		{
			if (!part.ValidateSymmetryRules())
			{
				return false;
			}
		}
		return true;
	}

	// Write data in this instance to a file
	public void Save(string filePath)
	{
		FileHandler.Save(this, filePath, true);
	}

	// Load data from a file into this instance's fields
	// Since loading from a file creates a new instance, loading should be a static method so it can be called without another instance
	public static SerializedBodyData Load(string filePath)
	{
		SerializedBodyData output = FileHandler.Load<SerializedBodyData>(filePath, true);

		if (output == null)
		{
			Debug.Log("Loaded serialized data is null");
			return null;
		}
		if (!output.ValidateBodyStructure())
		{
			Debug.Log("Invalid serialized body structure!");
			return null;
		}
		if (!output.ValidateBodySymmetry())
		{
			Debug.Log("Invalid symmetry!");
			return null;
		}

		return output;
	}
}
