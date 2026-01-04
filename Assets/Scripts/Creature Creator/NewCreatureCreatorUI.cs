using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewCreatureCreatorUI : MonoBehaviour
{
	public static NewCreatureCreatorUI instance;

	public NewBodyController bodyController;
	public NewGhostBodyPartController ghostBodyPartController;

	[Header("Selection / Mouse Navigation")]
	public int currentMouseButton;	// The current button being held (or the first held, if multiple)
	public MouseOverType mouseOver; // What the mouse was over when the current mouse button down started
	public CreatureCreatorCameraController cameraController;
	public NewBodyPartController selectedPartController;
	// Used to identify the selected part after the concrete body is reloaded
	// The list of ints are the repIndexes for the chain of parent concrete parts (root to leaf)
	public SerializedBodyPartData SelectedSerializedPart;
	public List<int> SelectedRepIndexChain;

	[Header("Gizmo Editing")]
	public NewGizmoController heldGizmo;	// The gizmo currently being manipulated (if applicable)

	[Header("Edit History")]
	public List<INewEditCommand> commandHistory;	// List of commands in the edit history
	public int historyIndex;	// Number of edit commands from the end of the editCommands list that are undone

	[Header("UI Elements")]
	public TMP_InputField inputFileName;
	public TMP_InputField inputSelectId;

	public TMP_InputField inputBodyPartId;
	public TMP_InputField inputParentId;
	public TMP_Dropdown dropdownSymmetryType;
	public Toggle toggleIsAxial;
	public TMP_InputField inputNumReps;
	public TMP_InputField[] inputPlaxisDirection;
	public TMP_InputField[] inputPlaxisPoint;
	public TMP_InputField[] inputPosition;
	public TMP_InputField[] inputRotation;
	public TMP_InputField[] inputScale;
	public TMP_InputField[] inputBulkOffset;

	[Header("Prefabs")]
	public GameObject ghostBodyPartPrefab;

	// Will eventually be moved to a dedicated settings / config file, but fine to keep here for now
	[Header("Keybinds")]
	public KeyCode keyResetCamera;
	public KeyCode keySnap;
	public KeyCode keyDistalToggleMode;


	private void Awake()
	{
		instance = this;

		currentMouseButton = -1;
		mouseOver = MouseOverType.None;
		heldGizmo = null;
		selectedPartController = null;
		SelectedSerializedPart = null;
		SelectedRepIndexChain = new();

		commandHistory = new List<INewEditCommand>();
		historyIndex = 0;
	}

	private void Start()
	{
		ButtonNewBody();
	}

	private void Update()
	{
		MouseNavigation();
		KeyboardNavigation();
	}

	// Saves the body's data to a file
	public void SaveBody(string fileName)
	{
		// Calculate full file path from the name
		string filePath = Application.dataPath + "/" + fileName + ".save";
		bodyController.Save(filePath);
	}

	// Loads the data from a file to the body and sets the controllers up
	public void LoadBody(string fileName)
	{
		// Calculate full file path from the name
		string filePath = Application.dataPath + "/" + fileName + ".save";
		bodyController.Load(filePath);

		// Clear command history
		ResetCommandHistory();
		// Unselect part and update UI
		SelectPart(null);
		UpdateUI();
	}

	// Rebuilds the entire concrete body from the stored serialized body and remakes the body part controllers
	public void ConstructConcreteBodyAndControllers()
	{
		selectedPartController = null;  // Invalidate previous selected part controller
		bodyController.ConstructConcreteBodyAndControllers();
	}

	// Sets command history variables, executes the command, and
	public void DoCommand(INewEditCommand command)
	{
		if (command == null)
		{
			Debug.Log("The command was null!");
			return;
		}

		// If there are undone commands in the history, truncate the list to remove them
		if (historyIndex > 0)
		{
			commandHistory.RemoveRange(commandHistory.Count - historyIndex, historyIndex);
		}
		historyIndex = 0;

		// Add the command to the history and execute it
		commandHistory.Add(command);
		command.Execute();

		// For now, also rebuild the concrete body immediately on button press
		ConstructConcreteBodyAndControllers();
		UpdateUI();
	}

	// Parses commands into a multicommand or a single command, if applicable, before actually doing them
	public void DoCommand(List<INewEditCommand> commands)
	{
		if (commands.Count == 0)
		{
			// Ignore empty lists of commands
			Debug.Log("Empty MultiCommand!");
		}
		else if (commands.Count == 1)
		{
			DoCommand(commands[0]);
		}
		else
		{
			DoCommand(new NewMultiCommand(commands));
		}
	}

	// Undo the last command in the history before the historyIndex pointer
	public void UndoCommand()
	{
		if (commandHistory.Count > historyIndex)
		{
			// Undo the command and increment the history index
			commandHistory[commandHistory.Count - historyIndex - 1].Undo();
			historyIndex++;

			// For now, also rebuild the concrete body immediately on button press
			ConstructConcreteBodyAndControllers();
			UpdateUI();
		}
		else
		{
			Debug.Log("Nothing left to undo!");
		}
	}

	// Redo the first command in the history after the historyIndex pointer
	public void RedoCommand()
	{
		if (historyIndex > 0)
		{
			// Redo the command and decrement the history index
			commandHistory[commandHistory.Count - historyIndex].Execute();
			historyIndex--;

			// For now, also rebuild the concrete body immediately on button press
			ConstructConcreteBodyAndControllers();
			UpdateUI();
		}
		else
		{
			Debug.Log("Nothing left to redo!");
		}
	}

	// Print current edit history to the console for debugging
	public void PrintCommandHistory()
	{
		Debug.Log("Printing History: ");
		for (int i = 0; i < commandHistory.Count - historyIndex; i++)
		{
			Debug.Log(commandHistory[i].ToString());
		}
		if (historyIndex > 0)
		{
			Debug.Log("Undid Commands: ");
			for (int i = commandHistory.Count - historyIndex; i < commandHistory.Count; i++)
			{
				Debug.Log(commandHistory[i].ToString());
			}
		}
	}

	// Sets the command history back to defaults, detroying the old commands
	public void ResetCommandHistory()
	{
		commandHistory.Clear();
		historyIndex = 0;
	}

	// Returns the type of object the mouse is over, with optional returns of the object itself and the world position of the raycast hit
	public MouseOverType GetMouseOver(out GameObject g, out Vector3 hitPosition)
	{
		// OutsideOfWindow
		if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
		{
			g = null;
			hitPosition = Vector3.zero;
			return MouseOverType.OutsideOfWindow;
		}

		// UI
		if (EventSystem.current.IsPointerOverGameObject())
		{
			g = null;
			hitPosition = Vector3.zero;
			return MouseOverType.UI;
		}

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		// Gizmo
		if (Physics.Raycast(ray, out RaycastHit hitData, Mathf.Infinity, LayerMask.GetMask("Gizmos")))
		{
			g = hitData.collider.gameObject;
			hitPosition = hitData.point;
			return MouseOverType.Gizmo;
		}

		// Bulk
		if (Physics.Raycast(ray, out hitData, Mathf.Infinity, LayerMask.GetMask("Bulk")))
		{
			g = hitData.collider.transform.parent.parent.gameObject;
			hitPosition = hitData.point;
			return MouseOverType.Bulk;
		}

		// None, default
		g = null;
		hitPosition = Vector3.zero;
		return MouseOverType.None;
	}

	// Control the camera, selection, and edit body parts using the mouse
	public void MouseNavigation()
	{
		// Check if a button began being pressed this frame before any other buttons
		if (currentMouseButton == -1)
		{
			if (Input.GetMouseButtonDown(0))	// If M1 just began being pressed, decide between doing nothing, selecting, and editing
			{
				// If M1 is pressed this frame, update currentMouseButton
				currentMouseButton = 0;

				// Get the type of object clicked, and if applicable, the GameObject and hit position of the click
				mouseOver = GetMouseOver(out GameObject clickedObject, out Vector3 hitPosition);

				// Because selecting is a one-time thing, deal with that here instead of continuously while M1 is held
				switch (mouseOver)
				{
					case MouseOverType.None:
						// If clicked over the background, deselect the current part
						SelectPart(null);
						UpdateUI();
						break;
					case MouseOverType.Bulk:
						// If clicked the bulk of a body part, select it
						SelectPart(clickedObject.GetComponent<NewBodyPartController>());
						// Update the UI on a new selection
						UpdateUI();
						break;
					case MouseOverType.Gizmo:	// TODO: gizmo editing
						// If clicked a gizmo, begin interaction with it
						heldGizmo = clickedObject.GetComponent<NewGizmoController>();
						heldGizmo.InteractStart(heldGizmo.transform.InverseTransformPoint(hitPosition));
						break;
					default:
						break;
				}
			}
			else if (Input.GetMouseButtonDown(1))	// If M2 just began being pressed, decide between doing nothing or camera rotating
			{
				// If M2 is pressed this frame, update currentMouseButton
				currentMouseButton = 1;
				// Get the type of object clicked
				mouseOver = GetMouseOver(out _, out _);
			}
			else if (Input.GetMouseButtonDown(2))	// If M3 just began being pressed, decide between doing nothing or camera moving
			{
				// If M3 is pressed this frame, update currentMouseButton
				currentMouseButton = 2;
				// Get the type of object clicked
				mouseOver = GetMouseOver(out _, out _);
			}
		}

		// Execute continuous methods for the current mouse button being held and mouseover type
		switch (currentMouseButton)
		{
			// No mouse button being held, either zooming or doing nothing while awaiting next button press
			case -1:
				// Mouse wheel zooming
				if (Input.mouseScrollDelta.y != 0)
				{
					// If mouse if scrolled, get the type of object the mouse is over this frame
					MouseOverType tempMouseOver = GetMouseOver(out _, out _);

					if (tempMouseOver != MouseOverType.OutsideOfWindow && tempMouseOver != MouseOverType.UI)
					{
						// If the scroll was not started outside the window or over UI (so over none, gizmo, or bulk), zoom the camera
						cameraController.SetZoom();
					}
				}

				break;
			// Left click being held, either selecting, editing, or doing nothing while awaiting button up
			case 0:
				if (mouseOver == MouseOverType.Gizmo)
				{
					// If clicked on a gizmo, Interact with it
					heldGizmo.InteractHold();
				}

				break;
			// Right click being held, either camera rotating or doing nothing while awaiting button up
			case 1:
				if (mouseOver != MouseOverType.OutsideOfWindow && mouseOver != MouseOverType.UI)
				{
					// If the M2 down was not started outside the window or over UI (so over none, gizmo, or bulk), rotate the camera
					cameraController.SetRotate();
				}

				break;
			// Middle click being held, either camera moving or doing nothing while awaiting button up
			case 2:
				if (mouseOver != MouseOverType.OutsideOfWindow && mouseOver != MouseOverType.UI)
				{
					// If the M3 down was not started outside the window or over UI (so over none, gizmo, or bulk), move the camera
					cameraController.SetMove();
				}

				break;
		}

		// Execute final methods on mouse release (if currentMouseButton != -1)
		if (currentMouseButton != -1)
		{
			if (Input.GetMouseButtonUp(currentMouseButton))
			{
				if (heldGizmo != null)
				{
					// If ended click while holding a gizmo, end the interaction and execute the output command(s)
					DoCommand(heldGizmo.InteractEnd());
					heldGizmo = null;
				}
				// If mouse button is released, update currentMouseButton
				currentMouseButton = -1;
				// Reset the type of object clicked
				mouseOver = MouseOverType.None;
			}
		}
	}

	// Control variables using the keyboard
	// May reorgasnize MouseNavigation and KeyboardNavigation to more fitting methods later
	public void KeyboardNavigation()
	{
		// If spacebar is pressed, reset the camera transform
		if (Input.GetKeyDown(keyResetCamera))
		{
			cameraController.ResetCamera();
		}

		if (Input.GetKeyDown(keyDistalToggleMode))
		{
			ghostBodyPartController.CycleDistalMode();
		}

		if (Input.GetKeyDown(keySnap))
		{
			ghostBodyPartController.SetSnapping(true);
		}
		if (Input.GetKeyUp(keySnap))
		{
			ghostBodyPartController.SetSnapping(false);
		}
	}

	// Sets the selected part while also checking to make sure it is valid
	// Selects the given part if it is valid, otherwise selects null by default or keeps the old part selected if specified
	// Also updates selectedPartInfo, if the new selection is valid
	public void SelectPart(NewBodyPartController part, bool selectOldIfInvalid = false)
	{
		if (part == null)
		{
			// If the given part is null, always make the new selection null regardless of selectOldIfInvalid value
			if (selectedPartController != null)
			{
				// Unselect the old part if it is not null
				selectedPartController.SetSelected(false);
			}
			selectedPartController = null;
			SelectedSerializedPart = null;
			SelectedRepIndexChain = new();
			ghostBodyPartController.UpdateSelection(null);
			return;
		}

		// After actions that could delete the selected serialized part, check if it is still part of the body
		if (bodyController.data.sBody.sBodyParts.Contains(part.data.sRef))
		{
			// If the given part is valid, select it
			if (selectedPartController != null)
			{
				// Unselect the old part if it is not null
				selectedPartController.SetSelected(false);
			}
			// Select the new part
			selectedPartController = part;
			part.SetSelected(true);
			SelectedSerializedPart = part.data.sRef;
			SelectedRepIndexChain = part.data.GetRepIndexChain();
			ghostBodyPartController.UpdateSelection(part);
			return;
		}
		else if (!selectOldIfInvalid)
		{
			// If the given part is not valid, and selectOldIfInvalid is FALSE, select null
			if (selectedPartController != null)
			{
				// Unselect the old part if it is not null
				selectedPartController.SetSelected(false);
			}
			selectedPartController = null;
			SelectedSerializedPart = null;
			SelectedRepIndexChain = new();
			ghostBodyPartController.UpdateSelection(null);
		}
		// If the given part is not valid, but selectOldIfInvalid is TRUE, keep the selection the same

		// Either way, if the part is invalid, print the error message
		Debug.Log("The part [" + selectedPartController.data.sRef.id + "] is not a valid selection!");
	}

	// Alternate SelectPart method to take the part's info instead of a direct reference to the part's controller
	// Used after reloading the body to reselect the new (but equivilent) part controller
	public void SelectPart(SerializedBodyPartData serializedPart, List<int> repIndexChain, bool selectOldIfInvalid = false)
	{
		// why is this outside the if statement below?
		SelectPart(null, selectOldIfInvalid);
		if (serializedPart == null)
		{
			// If the selected part is null, just call the original method with null
			return;
		}
		// Find the corresponding concrete body part from the given info
		ConcreteBodyPartData c = bodyController.data.GetConcreteBodyPartFromInfo(serializedPart, repIndexChain);
		// Make the default selected part null, in case the above search can't find the specified concrete part
		NewBodyPartController n = null;
		if (c != null)
		{
			// Find the corresponding controller from the concrete body part if it is not null
			n = bodyController.bodyPartControllerDict[c];
		}
		// Select this controller using the original method
		SelectPart(n, selectOldIfInvalid);
	}

	// UI ELEMENT METHODS //

	// Update fields in the body and body part menus
	public void UpdateUI()
	{
		// Update part selection in case it got deleted
		if (selectedPartController == null)
		{
			SelectPart(SelectedSerializedPart, SelectedRepIndexChain);
		}

		if (selectedPartController != null)
		{
			inputBodyPartId.interactable = true;
			inputParentId.interactable = true;
			dropdownSymmetryType.interactable = true;
			toggleIsAxial.interactable = (selectedPartController.data.sRef.symmetryType != SymmetryType.Asymmetrical);
			inputNumReps.interactable = true;
			inputPlaxisDirection[0].interactable = true;
			inputPlaxisDirection[1].interactable = true;
			inputPlaxisDirection[2].interactable = true;
			inputPlaxisPoint[0].interactable = true;
			inputPlaxisPoint[1].interactable = true;
			inputPlaxisPoint[2].interactable = true;
			inputPosition[0].interactable = true;
			inputPosition[1].interactable = true;
			inputPosition[2].interactable = true;
			inputRotation[0].interactable = true;
			inputRotation[1].interactable = true;
			inputRotation[2].interactable = true;
			inputScale[0].interactable = true;
			inputScale[1].interactable = true;
			inputScale[2].interactable = true;
			inputBulkOffset[0].interactable = true;
			inputBulkOffset[1].interactable = true;
			inputBulkOffset[2].interactable = true;

			inputSelectId.SetTextWithoutNotify(selectedPartController.data.sRef.id.ToString());
			inputBodyPartId.SetTextWithoutNotify(selectedPartController.data.sRef.id.ToString());
			inputParentId.SetTextWithoutNotify(selectedPartController.data.sRef.parentId.ToString());
			dropdownSymmetryType.SetValueWithoutNotify((int)selectedPartController.data.sRef.symmetryType);
			toggleIsAxial.SetIsOnWithoutNotify(selectedPartController.data.sRef.isAxial);
			inputNumReps.SetTextWithoutNotify(selectedPartController.data.sRef.numReps.ToString());
			inputPlaxisDirection[0].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisDirection.x.ToString());
			inputPlaxisDirection[1].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisDirection.y.ToString());
			inputPlaxisDirection[2].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisDirection.z.ToString());
			inputPlaxisPoint[0].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisPoint.x.ToString());
			inputPlaxisPoint[1].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisPoint.y.ToString());
			inputPlaxisPoint[2].SetTextWithoutNotify(selectedPartController.data.sRef.plaxisPoint.z.ToString());
			inputPosition[0].SetTextWithoutNotify(selectedPartController.data.sRef.position.x.ToString());
			inputPosition[1].SetTextWithoutNotify(selectedPartController.data.sRef.position.y.ToString());
			inputPosition[2].SetTextWithoutNotify(selectedPartController.data.sRef.position.z.ToString());
			inputRotation[0].SetTextWithoutNotify(selectedPartController.data.sRef.rotation.x.ToString());
			inputRotation[1].SetTextWithoutNotify(selectedPartController.data.sRef.rotation.y.ToString());
			inputRotation[2].SetTextWithoutNotify(selectedPartController.data.sRef.rotation.z.ToString());
			inputScale[0].SetTextWithoutNotify(selectedPartController.data.sRef.scale.x.ToString());
			inputScale[1].SetTextWithoutNotify(selectedPartController.data.sRef.scale.y.ToString());
			inputScale[2].SetTextWithoutNotify(selectedPartController.data.sRef.scale.z.ToString());
			inputBulkOffset[0].SetTextWithoutNotify(selectedPartController.data.sRef.bulkOffset.x.ToString());
			inputBulkOffset[1].SetTextWithoutNotify(selectedPartController.data.sRef.bulkOffset.y.ToString());
			inputBulkOffset[2].SetTextWithoutNotify(selectedPartController.data.sRef.bulkOffset.z.ToString());
		}
		else
		{
			inputBodyPartId.interactable = false;
			inputParentId.interactable = false;
			dropdownSymmetryType.interactable = false;
			toggleIsAxial.interactable = false;
			inputNumReps.interactable = false;
			inputPlaxisPoint[0].interactable = false;
			inputPlaxisPoint[1].interactable = false;
			inputPlaxisPoint[2].interactable = false;
			inputPlaxisDirection[0].interactable = false;
			inputPlaxisDirection[1].interactable = false;
			inputPlaxisDirection[2].interactable = false;
			inputPosition[0].interactable = false;
			inputPosition[1].interactable = false;
			inputPosition[2].interactable = false;
			inputRotation[0].interactable = false;
			inputRotation[1].interactable = false;
			inputRotation[2].interactable = false;
			inputBulkOffset[0].interactable = false;
			inputBulkOffset[1].interactable = false;
			inputBulkOffset[2].interactable = false;
			inputScale[0].interactable = false;
			inputScale[1].interactable = false;
			inputScale[2].interactable = false;

			inputSelectId.SetTextWithoutNotify("");
			inputBodyPartId.SetTextWithoutNotify("");
			inputParentId.SetTextWithoutNotify("");
			dropdownSymmetryType.SetValueWithoutNotify(0);
			toggleIsAxial.SetIsOnWithoutNotify(false);
			inputNumReps.SetTextWithoutNotify("");
			inputPlaxisPoint[0].SetTextWithoutNotify("");
			inputPlaxisPoint[1].SetTextWithoutNotify("");
			inputPlaxisPoint[2].SetTextWithoutNotify("");
			inputPlaxisDirection[0].SetTextWithoutNotify("");
			inputPlaxisDirection[1].SetTextWithoutNotify("");
			inputPlaxisDirection[2].SetTextWithoutNotify("");
			inputPosition[0].SetTextWithoutNotify("");
			inputPosition[1].SetTextWithoutNotify("");
			inputPosition[2].SetTextWithoutNotify("");
			inputRotation[0].SetTextWithoutNotify("");
			inputRotation[1].SetTextWithoutNotify("");
			inputRotation[2].SetTextWithoutNotify("");
			inputBulkOffset[0].SetTextWithoutNotify("");
			inputBulkOffset[1].SetTextWithoutNotify("");
			inputBulkOffset[2].SetTextWithoutNotify("");
			inputScale[0].SetTextWithoutNotify("");
			inputScale[1].SetTextWithoutNotify("");
			inputScale[2].SetTextWithoutNotify("");
		}
	}

	public void ButtonUndo()
	{
		UndoCommand();
	}

	public void ButtonRedo()
	{
		RedoCommand();
	}

	public void ButtonPrint()
	{
		PrintCommandHistory();
	}

	public void ButtonNewBody()
	{
		// Create new default body data and instantiate controllers
		bodyController.NewBody();

		// Destroy old Ghost Body Part
		if (ghostBodyPartController != null)
		{
			Destroy(ghostBodyPartController.gameObject);
		}
		// Create new Ghost Body Part
		ghostBodyPartController = Instantiate(ghostBodyPartPrefab).GetComponent<NewGhostBodyPartController>();
		ghostBodyPartController.Initialize(bodyController);

		// Clear command history
		ResetCommandHistory();
		// Unselect part and update UI
		SelectPart(null);
		UpdateUI();
	}

	public void ButtonSave()
	{
		SaveBody(inputFileName.text);
	}

	public void ButtonLoad()
	{
		LoadBody(inputFileName.text);
		SelectPart(null);
		UpdateUI();
	}

	[Obsolete]	// Selection is now done with mouse navigation
	public void ButtonSelect()
	{
		if (!int.TryParse(inputSelectId.text, out int inputId))
		{
			// If the input value s blank or otherwise invalid, do nothing
			Debug.Log("Invalid input!");
			return;
		}
		
		SerializedBodyPartData newSelected = bodyController.data.sBody.GetBodyPart(inputId);
		if (newSelected == null)
		{
			Debug.Log("No serialized part wth this ID! ( " + int.Parse(inputSelectId.text) + " )");
		}

		// Deprecated with new selection system
		//SelectPart(newSelected);

		UpdateUI();
	}

	public void ButtonAddPart()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}

		// Add new part with an ID determined by the sBody's current index
		// Create new part that inherits some values from the parent
		SerializedBodyPartData newSBodyPart = new(bodyController.data.sBody.sBodyPartIndex, selectedPartController.data.sRef.id)
		{
			// If parent is axial, inherit symmetry type, otherwise make it asymmetrical
			symmetryType = selectedPartController.data.sRef.isAxial ? selectedPartController.data.sRef.symmetryType : SymmetryType.Asymmetrical,
			isAxial = selectedPartController.data.sRef.isAxial,
			numReps = 1,
			// If parent is bilateral, PD = (1,0,0), otherwise PD = (0,0,1) (only matters for axial parents)
			plaxisDirection = selectedPartController.data.sRef.symmetryType == SymmetryType.Bilateral ? Vector3.right : Vector3.forward,
			plaxisPoint = Vector3.zero,
			position = (selectedPartController.data.sRef.scale.z + selectedPartController.data.sRef.bulkOffset.z) * Vector3.forward,
			rotation = Vector3.zero,
			scale = selectedPartController.data.sRef.scale,
		};

		DoCommand(new NewCommandAddBodyPart(bodyController.data.sBody, newSBodyPart));
	}

	public void ButtonDeletePart()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}

		List<INewEditCommand> commands = new();

		//Create list of deletions commands for the children from leaf to the selected part
		foreach (SerializedBodyPartData part in bodyController.data.sBody.PostOrder(selectedPartController.data.sRef))
		{
			commands.Add(new NewCommandDeleteBodyPart(bodyController.data.sBody, part));
		}

		if (selectedPartController.data.sRef == bodyController.data.sBody.GetRoot())
		{
			// If the deleted part was the root, add a new root part with an ID determined by the sBody's current index
			SerializedBodyPartData newRoot = new(bodyController.data.sBody.sBodyPartIndex, -1)
			{
				rotation = -90f * Vector3.right,
				scale = 0.5f * Vector3.one,
				bulkOffset = 0.25f * Vector3.forward
			};
			commands.Add(new NewCommandAddBodyPart(bodyController.data.sBody, newRoot));
		}

		// Unselect part
		SelectPart(null);
		UpdateUI();
		DoCommand(commands);
	}

	[Obsolete]	// IDs are set on creation, and should not be changed (and have no real reason to be changed)
	public void InputId()
	{
		// NOTE: likely to be phased out, since part ID is determined by the current body index, and shouldn't be changed
		// HOWEVER, for dev purposes, keep it for now
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		int newId = int.Parse(inputBodyPartId.text);
		if (newId == selectedPartController.data.sRef.id)
		{
			// No change
			return;
		}
		if (bodyController.data.sBody.GetBodyPart(newId) != null)
		{
			Debug.Log("A serialized part with this ID already exists! ( " + newId + " )");
			return;
		}

		DoCommand(new NewCommandChangeId(bodyController.data.sBody, selectedPartController.data.sRef, newId));
	}

	public void InputParentId()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		int newParentId = int.Parse(inputParentId.text);
		if (newParentId == selectedPartController.data.sRef.parentId)
		{
			// No change
			return;
		}
		SerializedBodyPartData newParent = bodyController.data.sBody.GetBodyPart(newParentId);
		if (newParent == null)
		{
			Debug.Log("No serialized part wth this ID! ( " + newParentId + " )");
			return;
		}

		DoCommand(new NewCommandChangeParent(bodyController.data.sBody, selectedPartController.data.sRef, newParent));
	}

	public void DropdownSymmetryType()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		SymmetryType newSymmetryType = (SymmetryType)dropdownSymmetryType.value;
		if (newSymmetryType == selectedPartController.data.sRef.symmetryType)
		{
			// No change
			return;
		}

		List<INewEditCommand> commands = new();

		switch (newSymmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Make sure isAxial = false
				if (selectedPartController.data.sRef.isAxial)
				{
					commands.Add(new NewCommandToggleIsAxial(selectedPartController.data.sRef));
				}
				// Make sure numReps = 1
				if (selectedPartController.data.sRef.numReps != 1)
				{
					commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 1));
				}
				break;
			case SymmetryType.Bilateral:
				if (selectedPartController.data.sRef.isAxial)
				{
					if (selectedPartController.data.sRef.numReps != 1)
					{
						// Make sure numReps = 1
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 1));
					}

					// Check if the position is on the plane
					Plane p = new(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint);
					if (!MathExt.IsPointOnPlane(p, selectedPartController.data.sRef.position))
					{
						// If the position is not on the plane, align the new position to the closest point on the plane
						Vector3 newPosition = MathExt.AlignPointToPlane(p, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the plane
					if (!MathExt.IsRotationOnPlane(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the plane, align the new rotation to be on the plane
						Vector3 newRotation = MathExt.AlignRotationToAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the bulk offset is on the plane
					if (selectedPartController.data.sRef.bulkOffset.x != 0)
					{
						// Set bulkOffset.x to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, selectedPartController.data.sRef.bulkOffset.y, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				else
				{
					if (selectedPartController.data.sRef.numReps != 2)
					{
						// Make sure numReps = 2
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 2));
					}
				}
				break;
			case SymmetryType.RadialRotate:
				if (selectedPartController.data.sRef.isAxial)
				{
					if (selectedPartController.data.sRef.numReps != 1)
					{
						// Make sure numReps = 1
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 1));
					}

					// Check if the position is on the axis
					if (!MathExt.IsPointOnAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint, selectedPartController.data.sRef.position))
					{
						// If the position is not on the axis, align the new position to the closest point on the axis
						Vector3 newPosition = MathExt.AlignPointToAxis(
							selectedPartController.data.sRef.plaxisDirection, 
							selectedPartController.data.sRef.plaxisPoint, 
							selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the axis
					if (!MathExt.IsRotationOnAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the axis, align the new rotation to be on the axis
						Vector3 newRotation = MathExt.AlignRotationToAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the X and Y scales are equal
					if (selectedPartController.data.sRef.scale.x != selectedPartController.data.sRef.scale.y)
					{
						// Average X and Y scales
						float newScalar = (selectedPartController.data.sRef.scale.x + selectedPartController.data.sRef.scale.y) / 2;
						commands.Add(new NewCommandChangeScale(selectedPartController.data.sRef, new Vector3(newScalar, newScalar, selectedPartController.data.sRef.bulkOffset.z)));
					}

					// Check if the bulk offset is on the axis
					if (selectedPartController.data.sRef.bulkOffset.x != 0 || selectedPartController.data.sRef.bulkOffset.y != 0)
					{
						// Set bulkOffset.x and bulkOffset.y to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, 0, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				else
				{
					if (selectedPartController.data.sRef.numReps < 2)
					{
						// Make sure numReps >= 2
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 2));
					}
				}
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		commands.Add(new NewCommandChangeSymmetryType(selectedPartController.data.sRef, newSymmetryType));

		DoCommand(commands);
	}

	public void ToggleAxial()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		bool newIsAxial = toggleIsAxial.isOn;
		if (newIsAxial == selectedPartController.data.sRef.isAxial)
		{
			// No change
			return;
		}

		List<INewEditCommand> commands = new();

		switch (selectedPartController.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical body parts are only nonaxial, no toggling allowed
				return;
			case SymmetryType.Bilateral:
				if (newIsAxial)
				{
					if (selectedPartController.data.sRef.numReps != 1)
					{
						// Make sure numReps = 1
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 1));
					}

					// Check if the position is on the plane
					Plane p = new(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint);
					if (!MathExt.IsPointOnPlane(p, selectedPartController.data.sRef.position))
					{
						// If the position is not on the plane, align the new position to the closest point on the plane
						Vector3 newPosition = MathExt.AlignPointToPlane(p, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the plane
					if (!MathExt.IsRotationOnPlane(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the plane, align the new rotation to be on the plane
						Vector3 newRotation = MathExt.AlignRotationToAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the bulk offset is on the plane
					if (selectedPartController.data.sRef.bulkOffset.x != 0)
					{
						// Set bulkOffset.x to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, selectedPartController.data.sRef.bulkOffset.y, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				else
				{
					if (selectedPartController.data.sRef.numReps != 2)
					{
						// Make sure numReps = 2
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 2));
					}
				}
				break;
			case SymmetryType.RadialRotate:
				if (newIsAxial)
				{
					if (selectedPartController.data.sRef.numReps != 1)
					{
						// Make sure numReps = 1
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 1));
					}

					// Check if the position is on the axis
					if (!MathExt.IsPointOnAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint, selectedPartController.data.sRef.position))
					{
						// If the position is not on the axis, align the new position to the closest point on the axis
						Vector3 newPosition = MathExt.AlignPointToAxis(
							selectedPartController.data.sRef.plaxisDirection,
							selectedPartController.data.sRef.plaxisPoint,
							selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the axis
					if (!MathExt.IsRotationOnAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the axis, align the new rotation to be on the axis
						Vector3 newRotation = MathExt.AlignRotationToAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the X and Y scales are equal
					if (selectedPartController.data.sRef.scale.x != selectedPartController.data.sRef.scale.y)
					{
						// Average X and Y scales
						float newScalar = (selectedPartController.data.sRef.scale.x + selectedPartController.data.sRef.scale.y) / 2;
						commands.Add(new NewCommandChangeScale(selectedPartController.data.sRef, new Vector3(newScalar, newScalar, selectedPartController.data.sRef.bulkOffset.z)));
					}

					// Check if the bulk offset is on the axis
					if (selectedPartController.data.sRef.bulkOffset.x != 0 || selectedPartController.data.sRef.bulkOffset.y != 0)
					{
						// Set bulkOffset.x and bulkOffset.y to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, 0, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				else
				{
					if (selectedPartController.data.sRef.numReps < 2)
					{
						// Make sure numReps >= 2
						commands.Add(new NewCommandChangeNumReps(selectedPartController.data.sRef, 2));
					}
				}
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		commands.Add(new NewCommandToggleIsAxial(selectedPartController.data.sRef));

		DoCommand(commands);
	}

	public void InputNumReps()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		int newNumReps = int.Parse(inputNumReps.text);
		if (newNumReps == selectedPartController.data.sRef.numReps)
		{
			// No change
			return;
		}
		if (selectedPartController.data.sRef.symmetryType != SymmetryType.RadialRotate || selectedPartController.data.sRef.isAxial)
		{
			// Asymmetrical body parts can only have 1 rep, no changing allowed
			// Axial body parts can only have 1 rep, no changing allowed
			// Bilateral nonaxial body parts can only have 2 reps, no changing allowed
			// Only Radial nonaxial body parts can have a variable number of reps
			Debug.Log("Cannot change numReps for this symmetry configuration!");
			return;
		}
		if (newNumReps < 2)
		{
			// Radial nonaxial body parts must have at least 2 reps
			Debug.Log("Not enough reps!");
			return;
		}
		if (newNumReps > 12)
		{
			// (For now) Radial nonaxial body parts must have no more than 12 reps
			Debug.Log("Too many reps!");
			return;
		}

		DoCommand(new NewCommandChangeNumReps(selectedPartController.data.sRef, newNumReps));
	}

	public void InputPlaxis()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		Vector3 newPlaxisPoint = new(
			float.Parse(inputPlaxisPoint[0].text), 
			float.Parse(inputPlaxisPoint[1].text), 
			float.Parse(inputPlaxisPoint[2].text));
		Vector3 newPlaxisDirection = new(
			float.Parse(inputPlaxisDirection[0].text),
			float.Parse(inputPlaxisDirection[1].text),
			float.Parse(inputPlaxisDirection[2].text));
		if (newPlaxisDirection.magnitude == 0)
		{
			Debug.Log("Invalid plaxis direction; cannot be the zero vector!");
		}
		if (newPlaxisDirection == selectedPartController.data.sRef.plaxisDirection && newPlaxisPoint == selectedPartController.data.sRef.plaxisPoint)
		{
			// No change
			return;
		}

		List<INewEditCommand> commands = new();

		switch (selectedPartController.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// No extra commands for changing the plaxis of an asymmetrical part (since it doesn't matter at all)
				break;
			case SymmetryType.Bilateral:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the position is on the new plane
					Plane p = new(newPlaxisDirection, newPlaxisPoint);
					if (!MathExt.IsPointOnPlane(p, selectedPartController.data.sRef.position))
					{
						// If the position is not on the new plane, align the new position to the closest point on the new plane
						Vector3 newPosition = MathExt.AlignPointToPlane(p, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the new plane
					if (!MathExt.IsRotationOnPlane(newPlaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the new plane, align the new rotation to be on the new plane
						Vector3 newRotation = MathExt.AlignRotationToAxis(newPlaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the bulk offset is on the plane
					if (selectedPartController.data.sRef.bulkOffset.x != 0)
					{
						// Set bulkOffset.x to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, selectedPartController.data.sRef.bulkOffset.y, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				// No additional commands for nonaxial body parts
				break;
			case SymmetryType.RadialRotate:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the position is on the newaxis
					if (!MathExt.IsPointOnAxis(newPlaxisDirection, newPlaxisPoint, selectedPartController.data.sRef.position))
					{
						// If the position is not on the new axis, align the new position to the closest point on the new axis
						Vector3 newPosition = MathExt.AlignPointToAxis(
							newPlaxisDirection,
							newPlaxisPoint,
							selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
					}

					// Check if the rotation is on the new axis
					if (!MathExt.IsRotationOnAxis(newPlaxisDirection, selectedPartController.data.sRef.rotation))
					{
						// If the rotation is not on the new axis, align the new rotation to be on the new axis
						Vector3 newRotation = MathExt.AlignRotationToAxis(newPlaxisDirection, selectedPartController.data.sRef.position);
						commands.Add(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
					}

					// Check if the X and Y scales are equal
					if (selectedPartController.data.sRef.scale.x != selectedPartController.data.sRef.scale.y)
					{
						// Average X and Y scales
						float newScalar = (selectedPartController.data.sRef.scale.x + selectedPartController.data.sRef.scale.y) / 2;
						commands.Add(new NewCommandChangeScale(selectedPartController.data.sRef, new Vector3(newScalar, newScalar, selectedPartController.data.sRef.bulkOffset.z)));
					}

					// Check if the bulk offset is on the axis
					if (selectedPartController.data.sRef.bulkOffset.x != 0 || selectedPartController.data.sRef.bulkOffset.y != 0)
					{
						// Set bulkOffset.x and bulkOffset.y to be 0
						commands.Add(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, new Vector3(0, 0, selectedPartController.data.sRef.bulkOffset.z)));
					}
				}
				// No additional commands for nonaxial body parts
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		commands.Add(new NewCommandChangePlaxis(selectedPartController.data.sRef, newPlaxisDirection, newPlaxisPoint));

		DoCommand(commands);
	}

	public void InputPosition()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		Vector3 newPosition = new(
			float.Parse(inputPosition[0].text),
			float.Parse(inputPosition[1].text),
			float.Parse(inputPosition[2].text));

		if (newPosition == selectedPartController.data.sRef.position)
		{
			// No change
			return;
		}

		switch (selectedPartController.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical parts can move freely
				break;
			case SymmetryType.Bilateral:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the position is on the plane, within a certain allowed distance
					Plane p = new(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint);
					if (!MathExt.IsPointOnPlane(p, newPosition, TechnicalConfig.maxAllowedDistance))
					{
						// Bilateral axial body parts must stay on the plane
						Debug.Log("New position is not on the plane of symmetry!");
						return;
					}
				}
				// Nonaxial parts can move freely
				break;
			case SymmetryType.RadialRotate:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the position is on the axis, within a certain allowed distance
					if (!MathExt.IsPointOnAxis(selectedPartController.data.sRef.plaxisDirection, selectedPartController.data.sRef.plaxisPoint, newPosition, TechnicalConfig.maxAllowedDistance))
					{
						// Radial axial body parts must stay on the axis
						Debug.Log("New position is not on the axis of symmetry!");
						return;
					}
				}
				// Nonaxial parts can move freely
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		DoCommand(new NewCommandChangePosition(selectedPartController.data.sRef, newPosition));
	}

	public void InputRotation()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		Vector3 newRotation = new(
			float.Parse(inputRotation[0].text),
			float.Parse(inputRotation[1].text),
			float.Parse(inputRotation[2].text));
		if (newRotation == selectedPartController.data.sRef.rotation)
		{
			// No change
			return;
		}

		switch (selectedPartController.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical parts can rotate freely
				break;
			case SymmetryType.Bilateral:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the rotation is on the plane, within a certain allowed angle
					Plane p0 = new(selectedPartController.data.sRef.plaxisDirection, Vector3.zero);
					if (!MathExt.IsRotationOnPlane(p0, newRotation, TechnicalConfig.maxAllowedAngle))
					{
						// Bilateral axial body parts must stay rotated on the plane
						Debug.Log("New rotation is not on the plane of symmetry!");
						return;
					}
				}
				// Nonaxial parts can move freely
				break;
			case SymmetryType.RadialRotate:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the rotation is on the axis, within a certain allowed angle
					if (!MathExt.IsRotationOnAxis(selectedPartController.data.sRef.plaxisDirection, newRotation, TechnicalConfig.maxAllowedAngle))
					{
						// Radial axial body parts must stay rotated on the axis
						Debug.Log("New rotation is not on the axis of symmetry!");
						return;
					}
				}
				// Nonaxial parts can rotate freely
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		DoCommand(new NewCommandChangeRotation(selectedPartController.data.sRef, newRotation));
	}

	public void InputBulkOffset()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		Vector3 newBulkOffset = new(
			float.Parse(inputBulkOffset[0].text),
			float.Parse(inputBulkOffset[1].text),
			float.Parse(inputBulkOffset[2].text));
		if (newBulkOffset == selectedPartController.data.sRef.bulkOffset)
		{
			// No change
			return;
		}

		switch (selectedPartController.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical parts can be offset freely
				break;
			case SymmetryType.Bilateral:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the bulk offset is on the plane
					if (newBulkOffset.x != 0)
					{
						// Bilateral axial body parts must stay offset on the plane
						Debug.Log("New bulkOffset is not on the plane of symmetry!");
						return;
					}
				}
				// Nonaxial parts can be offset freely
				break;
			case SymmetryType.RadialRotate:
				if (selectedPartController.data.sRef.isAxial)
				{
					// Check if the bulk offset is on the axis
					if (newBulkOffset.x != 0 || newBulkOffset.y != 0)
					{
						// Radial axial body parts must stay offset on the axis
						Debug.Log("New bulkOffset is not on the axis of symmetry!");
						return;
					}
				}
				// Nonaxial parts can be offset freely
				break;
			default:
				Debug.Log("Defaulted!");
				break;
		}

		DoCommand(new NewCommandChangeBulkOffset(selectedPartController.data.sRef, newBulkOffset));
	}

	public void InputScale()
	{
		if (selectedPartController == null)
		{
			Debug.Log("No body part selected!");
			return;
		}
		Vector3 newScale = new(
			float.Parse(inputScale[0].text),
			float.Parse(inputScale[1].text),
			float.Parse(inputScale[2].text));
		if (newScale == selectedPartController.data.sRef.scale)
		{
			// No change
			return;
		}
		if (selectedPartController.data.sRef.isAxial && selectedPartController.data.sRef.symmetryType == SymmetryType.RadialRotate && newScale.x != newScale.y)
		{
			// Radial axial body parts must stay scaled on the axis
			Debug.Log("New scale is not on the axis of symmetry!");
			return;
		}

		DoCommand(new NewCommandChangeScale(selectedPartController.data.sRef, newScale));
	}
}

// Readonly values for technical specifications such as angle limits
public class TechnicalConfig
{
	// Maximum allowed distance to be considered on the plane / axis
	public static readonly float maxAllowedDistance = 0.000001f;
	// Maximum allowed angle to be considered on the plane / axis
	public static readonly float maxAllowedAngle = 0.00005f;
	// The smallest value that a part's scale can have
	public static readonly float minScale = 0.01f;
	// The greatest value that a part's scale can have
	public static readonly float maxScale = 100f;

}

/* TODO:
 * + Change from menu to point and click system (IN PROGRESS)
 * + Validate and possibly change symmetry data when changing a body part's parent
	* Possibly not necessary if we make the mouse based part moving system already account for this?
 * + If changing symmetry type from asymmetrical to bilateral / radial, make parts close to the plaxis axial
	* Maybe not necessary if we use a manual toggle for the player to decide instead?
 * Create partial concrete reload method so that only the affected concrete body parts are affected on a change
 * Make a way of limiting the number of concrete parts now that theres no mor concrete IDs
	* For now, make sure not to have too many nested repetitions, since it will create an exponentially larger number of parts
 * Decide if the UI should load data from files instead of the data types accessing the files
 * Deletion option to reparent the children instead?
	* How to handle reparenting if the root part was deleted? pick the first child as new root?
 * Make way of translating changes to a concrete part affect the serialized part (with commands?)
 * Maybe get a way of making unique serialized IDs besides the index?
 * Eventually, change names of monobehaviours and such to reflect being the main version rather than new experimental versions
	* NewBodyController ==> BodyController
	* RadialRotate ==> Radial
	* Etc.
 */
