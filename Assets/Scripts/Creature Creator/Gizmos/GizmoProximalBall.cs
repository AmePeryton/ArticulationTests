using System.Collections.Generic;
using UnityEngine;

public class GizmoProximalBall : GizmoController
{
	public override void InteractStart()
	{
		base.InteractStart();
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(true);
	}

	public override void InteractHold()
	{
		// If pressed left alt and eligible to change, center or uncenter the part
		if (ghostPart.parent != null)
		{
			if (Input.GetKeyDown(specialKey) && ghostPart.parent.data.isCentered)
			{
				ghostPart.isCentered = !ghostPart.isCentered;
			}
		}

		// Change newPosition and/or newParent
		Vector3 rawNewPosition = Vector3.zero;

		if (ghostPart.parentId == -1)
		{
			switch (selectedPart.bodyController.data.symmetryType)
			{
				case SymmetryType.Asymmetrical:
					// If asymmetrical root, put rawposition on camera plane
					rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) -
						(transform.TransformPoint(hitPosition) - transform.position);
					break;
				case SymmetryType.Bilateral:
					// If bilateral root, put rawposition on symmetry plane
					rawNewPosition = MouseToWorldPlane(new(Vector3.right, Vector3.zero));
					break;
				case SymmetryType.RadialRotate:
				case SymmetryType.RadialFlip:
					// If radial root, put rawposition on symmetry axis
					rawNewPosition = Vector3.Scale(
						MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) - (transform.TransformPoint(hitPosition) - transform.position), 
						Vector3.up);
					break;
				default:
					break;
			}
		}
		else
		{
			bool success;
			if (!Input.GetKey(shiftModeKey))
			{
				// If not holding left shift, put rawPosition on parent surface
				rawNewPosition = MouseToColliderSurface(ghostPart.parent.raycastCollider, out success, true);
			}
			else
			{
				// If holding left shift, put rawposition on non-child-or-clone surface
				rawNewPosition = MouseToNonChildSurface(selectedPart.transform, out success, out BodyPartController hitPart);

				if (hitPart != null && hitPart != ghostPart.parent)
				{
					ghostPart.parentId = hitPart.data.id;
					ghostPart.parent = hitPart;
					ghostPart.transform.parent = hitPart.transform;
				}
			}

			if (ghostPart.isCentered)
			{
				switch (selectedPart.bodyController.data.symmetryType)
				{
					case SymmetryType.Asymmetrical:
						// If centered asymmetrical child, put rawposition on parent surface
						break;
					case SymmetryType.Bilateral:
						// If centered bilateral child, put rawposition on symmetry plane and parent surface
						// get parent surface, then make x = 0
						rawNewPosition.x = 0;
						break;
					case SymmetryType.RadialRotate:
					case SymmetryType.RadialFlip:
						// If centered radial child, put rawposition on symmetry axis and parent surface
						// get parent surface, then make x and z = 0
						rawNewPosition.x = 0;
						rawNewPosition.z = 0;
						break;
					default:
						break;
				}
			}

			if (!success)
			{
				// If the raycast didn't hit an eligible collider, stay in the last position
				rawNewPosition = transform.position;
			}
		}

		Vector3 localNewPosition = ghostPart.transform.parent.InverseTransformPoint(rawNewPosition);

		// If holding left control, move on 0.1m increments
		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		// Translate worldspace position to local position to store in the data
		ghostPart.position = localNewPosition;
	}

	public override void InteractEnd()
	{
		List<IEditCommand> newCommands = new();
		if (ghostPart.parent != selectedPart.parent)
		{
			newCommands.Add(new CommandChangeParent(selectedPart.bodyController, selectedPart.data, ghostPart.parent.data.id));
		}

		if (ghostPart.position != selectedPart.data.position)
		{
			newCommands.Add(new CommandMove(selectedPart.data, ghostPart.position));
		}

		if (ghostPart.isCentered && !selectedPart.data.isCentered)
		{
			newCommands.AddRange(CreatureCreatorUI.instance.CenterPartsRecursive(
				selectedPart.clones[0],
				selectedPart.bodyController.data.symmetryType,
				true));
		}
		else if (!ghostPart.isCentered && selectedPart.data.isCentered)
		{
			newCommands.AddRange(CreatureCreatorUI.instance.UncenterPartsRecursive(selectedPart.clones[0]));
		}

		IEditCommand newCommand;
		if (newCommands.Count > 0)
		{
			if (newCommands.Count == 1)
			{
				newCommand = newCommands[0];
			}
			else
			{
				newCommand = new MultiCommand(newCommands);
			}
			CreatureCreatorUI.instance.DoCommand(newCommand);
		}

		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(false);
	}

	public override void InteractCancel()
	{
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(false);
	}
}
