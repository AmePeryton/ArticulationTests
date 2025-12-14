using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BodyController : MonoBehaviour
{
	[Header("Data")]
	public BodyData data;

	[Header("Prefabs")]
	public GameObject bodyPartPrefab;

	public Dictionary<int, BodyPartController> bodyPartsDict;   // Dict of ids and controllers

	public void NewBody()
	{
		data = new();
		bodyPartsDict = new Dictionary<int, BodyPartController>();
		NewRootPart();
	}

	public BodyPartController NewRootPart()
	{
		return AddBodyPartData(new(data.bodyPartIndex++, -1, data.symmetryType != SymmetryType.Asymmetrical, 0.5f * Vector3.up, Vector3.zero, new Vector3(0.15f, 0.15f, 0.3f)));
	}

	public BodyPartController AddBodyPartData(BodyPartData newBodyPartData)
	{
		data.bodyParts.Add(newBodyPartData);
		return AddBodyPartController(newBodyPartData);
	}

	public BodyPartController AddBodyPartController(BodyPartData newBodyPartData)
	{
		// Set up variables and get parent main part
		BodyPartController newMainBodyPartController;
		bodyPartsDict.TryGetValue(newBodyPartData.parentId, out BodyPartController parentController);

		// Create main controller
		newMainBodyPartController = Instantiate(bodyPartPrefab).GetComponent<BodyPartController>();
		newMainBodyPartController.Initialize(this, newBodyPartData, parentController);
		bodyPartsDict.Add(newBodyPartData.id, newMainBodyPartController);

		UpdateClones(newMainBodyPartController);
		return newMainBodyPartController;
	}

	public void DeleteBodyPart(BodyPartData bodyPartData)
	{
		if (bodyPartsDict.TryGetValue(bodyPartData.id, out BodyPartController bodyPartController))
		{
			while (bodyPartController.children.Count > 0)
			{
				DeleteBodyPart(bodyPartController.children[0].data);
			}

			if (bodyPartController.parent != null)
			{
				bodyPartController.parent.children.Remove(bodyPartController);
			}

			data.bodyParts.Remove(bodyPartData);
			bodyPartsDict.Remove(bodyPartData.id);

			for (int i = 1; i < bodyPartController.clones.Count; i++)
			{
				Destroy(bodyPartController.clones[i].gameObject);
			}
			bodyPartController.clones.RemoveRange(1, bodyPartController.clones.Count - 1);
			Destroy(bodyPartController.gameObject);
		}
		else
		{
			Debug.Log("DeleteBodyPart: Controller not found!");
		}
	}

	public void ChangeBodyPartId(BodyPartData bodyPartData, int newId)
	{
		if (!bodyPartsDict.TryGetValue(newId, out BodyPartController bodyPartController))
		{
			bodyPartsDict.Remove(bodyPartData.id);
			bodyPartsDict.Add(newId, bodyPartController);
			bodyPartData.id = newId;

			foreach (BodyPartController child in bodyPartController.children)
			{
				child.data.parentId = newId;
			}
		}
		else
		{
			Debug.Log("ChangeBodyPartId: Another body part already has that ID!");
		}
	}

	public void StartSimulatingPhysics()
	{
		foreach (KeyValuePair<int, BodyPartController> pair in bodyPartsDict)
		{
			pair.Value.SetSimulating(true);
		}
	}

	public void StopSimulatingPhysics()
	{
		foreach (KeyValuePair<int, BodyPartController> pair in bodyPartsDict)
		{
			pair.Value.SetSimulating(false);
		}
	}

	public BodyPartController GetRootPart()
	{
		foreach (BodyPartController part in bodyPartsDict.Values)
		{
			if (part.data.parentId == -1)
			{
				return part;
			}
		}
		return null;
	}

	// Depth first search, parent returned before children, only includes main parts if includeClones = false
	public IEnumerable<BodyPartController> GetChildren(BodyPartController controller, bool includeClones = true)
	{
		if (controller.cloneIndex == 0 || includeClones)
		{
			yield return controller;

			foreach (BodyPartController child in controller.children)
			{
				foreach (BodyPartController x in GetChildren(child, includeClones))
				{
					yield return x;
				}
			}
		}
	}

	public void SetSymmetryType(SymmetryType newSymmetryType)
	{
		data.symmetryType = newSymmetryType;

		//UpdateSymmetry();
	}

	public void SetNumSegments(int newNumSegments)
	{
		data.numSegments = newNumSegments;

		//UpdateSymmetry();
	}

	public void UpdateSymmetry()
	{
		// Spawn clones of noncentered parts
		List<BodyPartController> partsTree = GetChildren(GetRootPart(), false).ToList();
		foreach (BodyPartController controller in partsTree)
		{
			UpdateClones(controller);
		}

		CreatureCreatorUI.instance.UpdateVisuals();
	}

	// Resets clones and remakes them based on current symmetry and centering
	public void UpdateClones(BodyPartController mainPartController)
	{
		BodyPartController parentController = mainPartController.parent;

		if (parentController != null)
		{
			for (int i = 1; i < mainPartController.clones.Count; i++)
			{
				mainPartController.clones[i].parent.children.Remove(mainPartController.clones[i]);
				Destroy(mainPartController.clones[i].gameObject);
			}
			mainPartController.clones.RemoveRange(1, mainPartController.clones.Count - 1);

			switch (data.symmetryType)
			{
				case SymmetryType.Asymmetrical:
					break;
				case SymmetryType.Bilateral:
					if (!mainPartController.data.isCentered)
					{
						if (parentController.data.isCentered)
						{
							// Create clone controller with same parent
							MakeClone(mainPartController, parentController, 1);
						}
						else
						{
							// Create clone controller with clone parent
							MakeClone(mainPartController, parentController.clones[1], 1);
						}
					}
					break;
				case SymmetryType.RadialRotate:
					if (!mainPartController.data.isCentered)
					{
						if (parentController.data.isCentered)
						{
							// Create clone controllers with same parent
							for (int i = 1; i < data.numSegments; i++)
							{
								MakeClone(mainPartController, parentController, i);
							}
						}
						else
						{
							// Create clone controllers with clone parents
							for (int i = 1; i < data.numSegments; i++)
							{
								MakeClone(mainPartController, parentController.clones[i], i);
							}
						}
					}
					break;
				case SymmetryType.RadialFlip:
					break;
				default:
					break;
			}
		}
	}

	// Makes a single clone part controller of a main part controller
	public BodyPartController MakeClone(BodyPartController mainController, BodyPartController parentController, int index)
	{
		BodyPartController cloneController = Instantiate(bodyPartPrefab).GetComponent<BodyPartController>();
		cloneController.Initialize(this, mainController.data, parentController, mainController, index);
		mainController.clones.Add(cloneController);

		return null;
	}

	public void Load(string fileName)
	{
		// Destroy old GameObjects
		foreach (BodyPartController controller in bodyPartsDict.Values)
		{
			foreach (BodyPartController clone in controller.clones)
			{
				Destroy(clone.gameObject);
			}
		}
		bodyPartsDict = new Dictionary<int, BodyPartController>();

		// Load Data
		data = FileHandler.Load<BodyData>(Application.dataPath + "/" + fileName + ".save", true);

		// Initiate parts root first, children of it, and so on)
		BodyPartData rootData = data.bodyParts.Find(x => x.parentId == -1);
		BodyPartController newRootController = Instantiate(bodyPartPrefab).GetComponent<BodyPartController>();
		bodyPartsDict.Add(rootData.id, newRootController);
		newRootController.Initialize(this, rootData, null);

		LoadChildParts(newRootController);
	}

	public void LoadChildParts(BodyPartController parentController)
	{
		List<BodyPartData> children = data.bodyParts.FindAll(x => x.parentId == parentController.data.id);
		foreach (BodyPartData child in children)
		{
			BodyPartController newChildController = AddBodyPartController(child);

			LoadChildParts(newChildController);
		}
	}

	public void Save(string fileName)
	{
		FileHandler.Save(data, Application.dataPath + "/" + fileName + ".save", true);
	}
}

[Serializable]
public class BodyData
{
	public int bodyPartIndex;
	public SymmetryType symmetryType;
	public int numSegments;
	public List<BodyPartData> bodyParts;

	public BodyData()
	{
		bodyPartIndex = 0;
		symmetryType = SymmetryType.Asymmetrical;
		numSegments = 1;
		bodyParts = new();
	}
}

public enum SymmetryType
{
	Asymmetrical = 0,
	Bilateral,
	RadialRotate,
	RadialFlip
}