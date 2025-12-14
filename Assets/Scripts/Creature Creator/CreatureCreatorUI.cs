using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreatureCreatorUI : MonoBehaviour
{
	public static CreatureCreatorUI instance;

	[Header("Edit Mode Panel")]
	public EditMode editMode;
	public Button[] editModeButtons;

	[Header("Physics Panel")]
	public Toggle togglePhysics;

	[Header("Symmetry Panel")]
	public Button[] symmetryTypeButtons;
	public GameObject numSegmentsSliderPanel;
	public TMP_Text numSegmentsLabel;
	public Slider numSegmentsSlider;

	[Header("Save/Load Panel")]
	public TMP_InputField inputFileName;

	[Header("Selection")]
	public BodyController bodyController;
	public BodyPartController selectedBodyPart;
	public Material gizmoHighlightMaterial;

	[Header("Camera Control")]
	public CreatureCreatorCameraController cameraController;

	[Header("Gizmo Editing")]
	public GhostBodyPartController ghostBodyPart;
	public GizmoController heldGizmo;	// The gizmo currently being manipulated (if applicable)

	// Private vars
	private List<IEditCommand> commandHistory;	// List of commands in the edit history
	private int historyIndex;		// Number of edit commands from the end of the editCommands list that are undone
	private bool isHoldingLeft;		// Custom GetMouseButton(0) so that pressing escape will cancel the action
	private MouseOverType mouseOver;	// The type of object that the moise pointer is over

	[Header("Experimental: Symmetry")]
	public GameObject symmetryPlanePrefab;
	public GameObject symmetryPlaneHalfPrefab;
	public List<GameObject> symmetryPlanes;		// Symmetry plane display GameObjects

	private void Awake()
	{
		instance = this;
		mouseOver = MouseOverType.None;
		commandHistory = new List<IEditCommand>();
		historyIndex = 0;
		ghostBodyPart.mainCube.SetActive(false);
		symmetryPlanes = new List<GameObject>();
	}

	private void Start()
	{
		SelectBodyPart(null);
		ChangeEditMode((int)editMode);

		// Create a new body and select the root part
		bodyController.NewBody();
		SelectBodyPart(bodyController.GetRootPart());
		UpdateVisuals();
	}

	private void Update()
	{
		// On first frame left mouse button is down
		if (Input.GetMouseButtonDown(0))
		{
			// Get the object that was clicked
			isHoldingLeft = true;
			mouseOver = GetMouseOver(out GameObject clickedObject, out Vector3 hitPosition);

			switch (mouseOver)
			{
				case MouseOverType.Bulk:
					// If clicked the bulk of a body part, select it
					SelectBodyPart(clickedObject.GetComponent<BodyPartController>());
					break;
				case MouseOverType.Gizmo:
					// If clicked a gizmo, begin interaction with it
					heldGizmo = clickedObject.GetComponent<GizmoController>();
					heldGizmo.hitPosition = heldGizmo.transform.InverseTransformPoint(hitPosition);
					//heldGizmo.InteractStart();

					// Give the gizmo the highlight material
					if (heldGizmo.TryGetComponent(out Renderer ren))
					{
						List<Material> mats = ren.materials.ToList();
						mats.Add(gizmoHighlightMaterial);
						ren.SetMaterials(mats);
					}
					//
					// : if tabbed out while left mouse is held, the material will not be removed. This is a bug
					break;
				case MouseOverType.UI:
					// If clicked over a UI element (interactable or not), do nothing
					break;
				default:
					// If clicked over nothing or outside of the window, unselect current part
					SelectBodyPart(null);
					break;
			}
		}

		// While holding left mouse button (and not after cancelling action)
		if (isHoldingLeft)
		{
			if (mouseOver == MouseOverType.None)
			{
				// If click started over nothing, rotate the camera
				cameraController.SetRotate();
			}
			if (mouseOver == MouseOverType.Gizmo)
			{
				// If click started over gizmo, interact with it
				heldGizmo.InteractHold();
			}
		}

		// On frame when the left mouse button is released
		if (Input.GetMouseButtonUp(0))
		{
			// If left mouse button is released, set values to defaults
			isHoldingLeft = false;
			mouseOver = MouseOverType.None;
			
			if (heldGizmo != null)
			{
				// If holding a gizmo, end the interaction
				heldGizmo.InteractEnd();
				// Remove the highlight material from the gizmo
				if (heldGizmo.TryGetComponent(out Renderer ren))
				{
					List<Material> mats = ren.materials.ToList();
					mats.RemoveAt(mats.Count - 1);
					ren.SetMaterials(mats);
				}

				// Set values to defaults
				heldGizmo.hitPosition = Vector3.zero;
				heldGizmo = null;

				// If the currently selected clone body part is about to be deleted next frame, select the main part
				if (selectedBodyPart.cloneIndex >= selectedBodyPart.clones.Count)
				{
					SelectBodyPart(selectedBodyPart.clones[0]);
				}
				//else
				//{
				//	SelectBodyPart(selectedBodyPart.clones[selectedBodyPart.cloneIndex]);
				//}
			}
		}

		// Interaction canceling
		if (Input.GetKey(KeyCode.Escape))
		{
			// If pressed escape, set values to defaults
			isHoldingLeft = false;
			mouseOver = MouseOverType.None;

			if (heldGizmo != null)
			{
				// If holding a gizmo, cancel the interaction
				heldGizmo.InteractCancel();
				// Remove the highlight material from the gizmo
				if (heldGizmo.TryGetComponent(out Renderer ren))
				{
					List<Material> mats = ren.materials.ToList();
					mats.RemoveAt(mats.Count - 1);
					ren.SetMaterials(mats);
				}

				// Set values to defaults
				heldGizmo.hitPosition = Vector3.zero;
				heldGizmo = null;
			}
		}

		// Right mouse button camera rotation
		if (!Input.GetMouseButton(0))
		{
			// If left mouse button is not down...
			if (Input.GetMouseButtonDown(1))
			{
				// On first frame right mouse button is clicked, get the type of object the mouse is over
				mouseOver = GetMouseOver(out _, out _);
			}

			if (Input.GetMouseButton(1))
			{
				// While right mouse button is held...
				if (mouseOver != MouseOverType.OutsideOfWindow && mouseOver != MouseOverType.UI)
				{
					// If the click was not started outside the window or over UI (so over none, gizmo, or bulk), rotate the camera
					cameraController.SetRotate();
				}
			}

			if (Input.GetMouseButtonUp(1))
			{
				// On right mouse button release, set values to defaults
				mouseOver = MouseOverType.None;
			}
		}

		// Middle mouse button camera panning
		if (!Input.GetMouseButton(0))
		{
			// If left mouse button is not down...
			if (Input.GetMouseButtonDown(2))
			{
				// On first frame middle mouse button is clicked, get the type of object the mouse is over
				mouseOver = GetMouseOver(out _, out _);
			}

			if (Input.GetMouseButton(2))
			{
				// While middle mouse buttons are held...
				if (mouseOver != MouseOverType.OutsideOfWindow && mouseOver != MouseOverType.UI)
				{
					// If the click was not started outside the window or over UI (so over none, gizmo, or bulk), pan the camera
					cameraController.SetMove();
				}
			}

			if (Input.GetMouseButtonUp(2))
			{
				// On middle mouse button release, set values to defaults
				mouseOver = MouseOverType.None;
			}
		}

		// Mouse wheel zooming
		if (Input.mouseScrollDelta.y != 0)
		{
			// If mouse if scrolled, get the type of object the mouse is over
			MouseOverType tempMouseOver = GetMouseOver(out _, out _);

			if (tempMouseOver != MouseOverType.OutsideOfWindow && tempMouseOver != MouseOverType.UI)
			{
				// If the scroll was not started outside the window or over UI (so over none, gizmo, or bulk), zoom the camera
				cameraController.SetZoom();
			}
		}

		// Always change the camera offset
		// NOTE: make this change based off of the panels open, once panel minimizing is implemented
		cameraController.SetOffset();

		// Undo / Redo hotkeys
		// WARNING: using these in the editor will also apply to Unity's undo / redo
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				if (Input.GetKeyDown(KeyCode.Z))
				{
					// If holding control + shift + z, redo the last command if applicable
					RedoCommand();
				}
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.Z))
				{
					// If holding control + z, but not shift, undo the last command if applicable
					UndoCommand();
				}
				else if (Input.GetKeyDown(KeyCode.Y))
				{
					// If holding control + y, but not shift, redo the last command if applicable
					RedoCommand();
				}
			}
		}
	}

	// NOTE: not meant to run every frame: too resource intensive
	public void UpdateVisuals()
	{
		for (int i = 0; i < symmetryTypeButtons.Length; i++)
		{
			// Disable symmetry button that is of the current symmetry type
			symmetryTypeButtons[i].interactable = (i != (int)bodyController.data.symmetryType);
		}

		// Set symmetry segment slider panel values
		numSegmentsLabel.text = bodyController.data.numSegments.ToString();
		numSegmentsSlider.SetValueWithoutNotify(bodyController.data.numSegments);
		if (bodyController.data.symmetryType == SymmetryType.Asymmetrical || bodyController.data.symmetryType == SymmetryType.Bilateral)
		{
			numSegmentsSliderPanel.SetActive(false);
		}
		else
		{
			numSegmentsSliderPanel.SetActive(true);
		}

		// Update symmetry plane GameObjects
		SetSymmetryPlanes();
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

	// Deselects current body part and selects the new one
	public void SelectBodyPart(BodyPartController bodyPart)
	{
		if (selectedBodyPart != null)
		{
			selectedBodyPart.SetSelected(false);
		}
		selectedBodyPart = bodyPart;
		if (selectedBodyPart != null)
		{
			selectedBodyPart.SetSelected(true);
		}

		// Update ghost part variables
		ghostBodyPart.UpdateVariables();
		//ghostBodyPart.SetEditMode(editMode);
	}

	// Change the current editing mode
	public void ChangeEditMode(int newMode)
	{
		editMode = newMode switch
		{
			0 => EditMode.Normal,
			1 => EditMode.Advanced,
			2 => EditMode.Bulk,
			3 => EditMode.Joint,
			_ => EditMode.None,
		};

		// Update ghost part variables
		ghostBodyPart.SetEditMode(editMode);

		for (int i = 0; i < editModeButtons.Length; i++)
		{
			// Disable edit mode button that is of the current edit mode type
			editModeButtons[i].interactable = (i != newMode);
		}
	}

	// Update symmetry plane GameObjects
	public void SetSymmetryPlanes()
	{
		foreach (GameObject plane in symmetryPlanes)
		{
			Destroy(plane);
		}
		symmetryPlanes.Clear();

		switch (bodyController.data.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				break;
			case SymmetryType.Bilateral:
				symmetryPlanes.Add(Instantiate(symmetryPlanePrefab));
				break;
			case SymmetryType.RadialRotate:
			case SymmetryType.RadialFlip:
				for (int i = 0; i < bodyController.data.numSegments; i++)
				{
					GameObject newSymmetryPlane = Instantiate(symmetryPlaneHalfPrefab);
					newSymmetryPlane.transform.Rotate(i * 360f / bodyController.data.numSegments * Vector3.up);
					symmetryPlanes.Add(newSymmetryPlane);
				}
				break;
			default:
				break;
		}
	}

	// Command Pattern Methods //
	// Execute a command for the first time
	public void DoCommand(IEditCommand command)
	{
		if (command.GetType() == typeof(MultiCommand))
		{
			if (((MultiCommand)command).subCommands.Count == 0)
			{
				Debug.Log("Empty MultiCommand!");
				return;
			}
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
		command.LateExecute();

		ghostBodyPart.UpdateVariables();
	}

	// Undo the last executed command before the history index marker
	public void UndoCommand()
	{
		if (commandHistory.Count > historyIndex)
		{
			// Undo the command and increment the history index
			commandHistory[commandHistory.Count - historyIndex - 1].Undo();
			commandHistory[commandHistory.Count - historyIndex - 1].LateExecute();
			historyIndex++;
		}
		else
		{
			Debug.Log("UndoCommand: Nothing left to undo!");
		}

		// Update ghost part variables
		ghostBodyPart.UpdateVariables();
	}

	// Redo the first command after the history index marker
	public void RedoCommand()
	{
		if (historyIndex > 0)
		{
			// Redo the command and decrement the history index
			commandHistory[^historyIndex].Execute();
			commandHistory[^historyIndex].LateExecute();
			historyIndex--;
		}
		else
		{
			Debug.Log("RedoCommand: Nothing left to redo!");
		}

		// Update ghost part variables
		ghostBodyPart.UpdateVariables();
	}

	// Print current edit history to the console for debugging
	[ContextMenu("PrintHistory")]
	public void PrintHistory()
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

	// Reset command history
	[ContextMenu("PurgeHistory")]
	public void PurgeHistory()
	{
		commandHistory.Clear();
		historyIndex = 0;
	}

	// Interpret a string in the console to a command
	// TODO: implement this
	public void ConsoleCommand(string commandString)
	{
		// add [parentId]
		// delete [id]
		// changeId [id] [newId]
		// changeParent [id] [newParentId]
		// changeName [id] [newName]
		// move [id] [newPosition]
		// rotate [id] [newRotation]
		// changeLength [id] [newLength]
		// changeBulkOffset [id] [newBulkOffset]
		// changeScale [id] [newScale]
		// changeJointLimits [id] [newJointLimitsLow] [newJointLimitsHigh]
		// undo [num = 1]
		// redo [num = 1]

		string[] subStrings = commandString.Split();

		switch (subStrings[0])
		{
			case "add":
				break;
			case "delete":
				break;
			case "changeId":
				break;
			case "changeParent":
				break;
			case "changeName":
				break;
			case "move":
				break;
			case "rotate":
				break;
			case "changeLength":
				break;
			case "changeBulkOffset":
				break;
			case "changeScale":
				break;
			case "changeJointLimits":
				break;
			case "undo":
				break;
			case "redo":
				break;
			default:
				Debug.Log("StringToCommand: Command not recognized: " + subStrings[0]);
				break;
		}
	}

	// Button Methods //
	// Start or stop simulating physics on the body
	public void EnablePhysicsToggle()
	{
		SelectBodyPart(null);

		if (togglePhysics.isOn)
		{
			foreach (GameObject symmetryPlane in symmetryPlanes)
			{
				symmetryPlane.SetActive(false);
			}
			bodyController.StartSimulatingPhysics();
		}
		else
		{
			foreach (GameObject symmetryPlane in symmetryPlanes)
			{
				symmetryPlane.SetActive(true);
			}
			bodyController.StopSimulatingPhysics();
		}
	}

	// Change the current symmetry mode of the body
	public void SymmetryButton(int symmetryType)
	{
		SelectBodyPart(null);

		MultiCommand command = new(new List<IEditCommand>()); ;
		BodyPartController rootPart = bodyController.GetRootPart();
		//List<BodyPartController> partsTree = bodyController.GetChildren(rootPart, false).ToList();
		switch ((SymmetryType)symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Add commands to uncenter all parts
				command.subCommands.AddRange(UncenterPartsRecursive(rootPart));
				// Add commands to change body symmetry type
				command.subCommands.Add(new CommandChangeSymmetry(bodyController, SymmetryType.Asymmetrical));
				command.subCommands.Add(new CommandChangeNumSegments(bodyController, 1));
				DoCommand(command);
				break;
			case SymmetryType.Bilateral:
				// Add commands to make parts close to the symmetry plane centered
				command.subCommands.AddRange(CenterPartsRecursive(rootPart, SymmetryType.Bilateral));
				// Add commands to change body symmetry type
				command.subCommands.Add(new CommandChangeSymmetry(bodyController, SymmetryType.Bilateral));
				command.subCommands.Add(new CommandChangeNumSegments(bodyController, 2));
				DoCommand(command);
				break;
			case SymmetryType.RadialRotate:
				// Add commands to make parts close to the symmetry axis centered
				command.subCommands.AddRange(CenterPartsRecursive(rootPart, SymmetryType.RadialRotate));
				// Add commands to change body symmetry type
				command.subCommands.Add(new CommandChangeSymmetry(bodyController, SymmetryType.RadialRotate));
				command.subCommands.Add(new CommandChangeNumSegments(bodyController, 6));
				DoCommand(command);
				break;
			case SymmetryType.RadialFlip:
				// Add commands to make parts close to the symmetry axis centered
				command.subCommands.AddRange(CenterPartsRecursive(rootPart, SymmetryType.RadialFlip));
				// Add commands to change body symmetry type
				command.subCommands.Add(new CommandChangeSymmetry(bodyController, SymmetryType.RadialFlip));
				command.subCommands.Add(new CommandChangeNumSegments(bodyController, 6));
				DoCommand(command);
				break;
			default:
				break;
		}
		UpdateVisuals();
	}

	// Adds a new body part to the selected body part
	public void AddPartButton()
	{
		// If a part is selected
		if (selectedBodyPart != null)
		{
			// Create new data for a part with the selected part as the parent
			BodyPartData newBodyPartData = new(
				bodyController.data.bodyPartIndex++, 
				selectedBodyPart.data.id, 
				selectedBodyPart.data.isCentered, 
				selectedBodyPart.data.length * Vector3.forward, 
				Vector3.zero, 
				new Vector3(0.1f, 0.1f, 0.2f));

			// Execute command to add part to body data and add appropriate controllers
			DoCommand(new CommandAddPart(bodyController, newBodyPartData));

			// Select new body part that is the child of the curently selected part
			SelectBodyPart(bodyController.bodyPartsDict[newBodyPartData.id].clones[selectedBodyPart.cloneIndex]);
		}
	}

	// Executes commands to delete a body part and its children
	public void DeletePartButton()
	{
		BodyPartController nextSelection = null;
		// Don't delete root or null
		if (selectedBodyPart != null && selectedBodyPart.parent != null)
		{
			nextSelection = selectedBodyPart.parent;
			// Get all main part children of the selected part, children before parents
			List<BodyPartController> children = bodyController.GetChildren(selectedBodyPart.clones[0], false).ToList();
			children.Reverse();

			MultiCommand multiCommand = new(new());

			foreach (BodyPartController child in children)
			{
				// Add command to delete the part
				multiCommand.subCommands.Add(new CommandDeletePart(bodyController, child.data));
			}

			// Execute the command(s)
			if (multiCommand.subCommands.Count > 1)
			{
				DoCommand(multiCommand);
			}
			else if (multiCommand.subCommands.Count > 0)
			{
				DoCommand(multiCommand.subCommands[0]);
			}
		}

		SelectBodyPart(nextSelection);
	}

	// Change the number of (radial) symmetry segments of the body
	public void ChangeNumSegmentsSlider()
	{
		SelectBodyPart(null);
		if (bodyController.data.numSegments != (int)numSegmentsSlider.value)
		{
			DoCommand(new CommandChangeNumSegments(bodyController, (int)numSegmentsSlider.value));
		}
	}

	// Save the body to a text file
	public void SaveButton()
	{
		bodyController.Save(inputFileName.text);
	}

	// Load a body from a text file
	public void LoadButton()
	{
		SelectBodyPart(null);
		// Clear command history
		commandHistory = new List<IEditCommand>();
		historyIndex = 0;
		// Load file to body
		bodyController.Load(inputFileName.text);

		UpdateVisuals();
	}

	// Helper Methods //
	// Checks if a part should be centered, if so, returns commands to center it and checks its children
	// NOTE: make sure this is only ran on main parts
	public IEnumerable<IEditCommand> CenterPartsRecursive(BodyPartController controller, SymmetryType symmetryType, bool centerOverride = false)
	{
		bool isCenterable = false;
		switch (symmetryType)
		{
			case SymmetryType.Bilateral:
				// If the part is the root OR is close to the center plane and mostly angled toward it...
				if (centerOverride || controller.data.parentId == -1 ||
					(controller.transform.position.x >= -0.01f &&
					controller.transform.position.x <= 0.01f &&
					controller.transform.up.x >= -0.1f &&
					controller.transform.up.x <= 0.1f &&
					controller.transform.forward.x >= -0.1f &&
					controller.transform.forward.x <= 0.1f &&
					controller.data.bulkOffset.x >= -0.1f * controller.data.scale.x &&
					controller.data.bulkOffset.x <= 0.1f * controller.data.scale.x))
				{
					isCenterable = true;
				}
				break;
			case SymmetryType.RadialRotate:
			case SymmetryType.RadialFlip:
				// If the part is the root OR is close to the center axis and mostly angled straight up or down...
				if (centerOverride || controller.data.parentId == -1 ||
					(Vector2.Distance(MathExt.Flatten(controller.transform.position), Vector2.zero) <= 0.01f &&
					controller.transform.right.y >= -0.1f &&
					controller.transform.right.y <= 0.1f &&
					controller.transform.up.y >= -0.1f &&
					controller.transform.up.y <= 0.1f &&
					controller.data.bulkOffset.x >= -0.1f * controller.data.scale.x &&
					controller.data.bulkOffset.x <= 0.1f * controller.data.scale.x &&
					controller.data.bulkOffset.y >= -0.1f * controller.data.scale.y &&
					controller.data.bulkOffset.y <= 0.1f * controller.data.scale.y))
				{
					isCenterable = true;
				}
				break;
			default:
				isCenterable = false;
				break;
		}

		if (isCenterable)
		{
			// Get commands to center the body part
			List<IEditCommand> newCommands = CenterBodyPart(controller.data, symmetryType, controller.data.parentId != -1);
			foreach (IEditCommand command in newCommands)
			{
				// Return the commands in order
				yield return command;
			}

			foreach (BodyPartController child in controller.children)
			{
				if (child.cloneIndex == 0)
				{
					// For each main part child...
					foreach (IEditCommand x in CenterPartsRecursive(child, symmetryType))
					{
						// Get the commands to center (or uncenter) children and return them in order
						yield return x;
					}
				}
			}
		}
		else
		{
			// Return command to uncenter this part
			yield return new CommandChangeCentered(controller.data, false);

			foreach (BodyPartController child in controller.children)
			{
				if (child.cloneIndex == 0)
				{
					// For each main part child...
					foreach (IEditCommand x in UncenterPartsRecursive(child))
					{
						// Get the commands to uncenter children and return them in order
						yield return x;
					}
				}
			}
		}
	}

	public List<IEditCommand> CenterBodyPart(BodyPartData part, SymmetryType symmetryType, bool isParentCentered)
	{
		List<IEditCommand> output = new();
		switch (symmetryType)
		{
			case SymmetryType.Bilateral:
				output = new()
				{
					new CommandMove(part, new(0, part.position.y, part.position.z)),
					new CommandRotate(part, new(part.rotation.x, 0, 0)),
					new CommandChangeBulkOffset(part, new(0, part.bulkOffset.y, part.bulkOffset.z)),
					new CommandChangeJointLimits(part, new Vector3[] { 
						new(part.jointLimits[0].x, (part.jointLimits[0].y - part.jointLimits[1].y) / 2, (part.jointLimits[0].z - part.jointLimits[1].z) / 2), 
						new(part.jointLimits[1].x, (-part.jointLimits[0].y + part.jointLimits[1].y) / 2, (-part.jointLimits[0].z + part.jointLimits[1].z) / 2) }),
					new CommandChangeCentered(part, true)
				};
				break;
			case SymmetryType.RadialRotate:
			case SymmetryType.RadialFlip:
				if (isParentCentered)
				{
					output = new()
					{
						new CommandMove(part, new(0, 0, part.position.z)),
						new CommandRotate(part, Vector3.zero),
						new CommandChangeBulkOffset(part, new(0, 0, part.bulkOffset.z)),
						new CommandChangeJointLimits(part, new Vector3[] {
							new(part.jointLimits[0].x, (part.jointLimits[0].y - part.jointLimits[1].y) / 2, (part.jointLimits[0].z - part.jointLimits[1].z) / 2),
							new(part.jointLimits[1].x, (-part.jointLimits[0].y + part.jointLimits[1].y) / 2, (-part.jointLimits[0].z + part.jointLimits[1].z) / 2) }),
						new CommandChangeCentered(part, true)
					};
				}
				else
				{
					output = new()
					{
						new CommandMove(part, new(0, part.position.y, 0)),
						new CommandRotate(part, new((part.rotation.x >= 0)? 90 : -90, 0, 0)),
						new CommandChangeBulkOffset(part, new(0, 0, part.bulkOffset.z)),
						new CommandChangeJointLimits(part, new Vector3[] {
							new(part.jointLimits[0].x, (part.jointLimits[0].y - part.jointLimits[1].y) / 2, (part.jointLimits[0].z - part.jointLimits[1].z) / 2),
							new(part.jointLimits[1].x, (-part.jointLimits[0].y + part.jointLimits[1].y) / 2, (-part.jointLimits[0].z + part.jointLimits[1].z) / 2) }),
						new CommandChangeCentered(part, true)
					};
				}
				break;
			default:
				break;
		}
		return output;
	}

	// Helper Methods //
	// Returns list of commands to uncenter a body part and all of its children
	public IEnumerable<IEditCommand> UncenterPartsRecursive(BodyPartController controller)
	{
		yield return new CommandChangeCentered(controller.data, false);

		foreach (BodyPartController child in controller.children)
		{
			if (child.cloneIndex == 0)
			{
				foreach (IEditCommand x in UncenterPartsRecursive(child))
				{
					yield return x;
				}
			}
		}
	}
}

public enum EditMode
{
	None = -1,
	Normal,
	Advanced,
	Bulk,
	Joint
}

public enum MouseOverType
{
	OutsideOfWindow = -1,
	None,
	UI,
	Gizmo,
	Bulk
}

/* TODO:
 * Make ghost part properly select clone parts
 * Get all gizmos to properly interact with centered parts
 * Symmetry masking
 * Show ghost parts for children and clones
 * Highlight the main body part(s)?
 * exclude clones and clones' children from MouseToNonChildSurface
 * fix distal/proximal centering so that it doesnt reset the new position
 * highlight new parent when changing with gizmos
 */