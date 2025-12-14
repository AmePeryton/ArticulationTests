using System.Collections.Generic;
using UnityEngine;

public class GizmoDistalBall : GizmoController
{
	public override void InteractStart()
	{
		base.InteractStart();
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(true);
	}

	public override void InteractHold()
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);

		// If pressed left alt and eligible to change, center or uncenter the part
		if (ghostPart.parent != null)
		{
			if (Input.GetKeyDown(specialKey) && ghostPart.parent.data.isCentered)
			{
				ghostPart.isCentered = !ghostPart.isCentered;
			}
		}

		// If centered, move on symmetry axes
		if (ghostPart.isCentered)
		{
			switch (selectedPart.bodyController.data.symmetryType)
			{
				case SymmetryType.Asymmetrical:
					break;
				case SymmetryType.Bilateral:
					rawNewPosition = MouseToWorldPlane(new(Vector3.right, Vector3.zero));
					break;
				case SymmetryType.RadialRotate:
					rawNewPosition = Vector3.Scale(rawNewPosition, Vector3.up);
					break;
				default:
					Debug.Log("Symmetry type not yet implemented :(");
					break;
			}
		}

		Vector3 localNewPosition = selectedPart.transform.parent.InverseTransformPoint(rawNewPosition);

		// If holding left shift, change length, otherwise change angles
		if (Input.GetKey(shiftModeKey))
		{
			float newLength = Vector3.Distance(localNewPosition, selectedPart.transform.localPosition);
			float diff = selectedPart.data.scale.z - selectedPart.data.length;

			// If holding left control, snap to 0.1m increments
			if (Input.GetKey(snapKey))
			{
				newLength = MathExt.RoundFloat(newLength, snapDistance);
			}

			ghostPart.length = newLength;
			ghostPart.scale.z = newLength + diff;
		}
		else
		{
			Vector3 newAngles = Quaternion.LookRotation(localNewPosition - selectedPart.transform.localPosition).eulerAngles;

			// Clamp angles -180 to 180
			newAngles = new Vector3((newAngles.x + 180) % 360 - 180, (newAngles.y + 180) % 360 - 180, (newAngles.z + 180) % 360 - 180);

			// If holding left control, snap to 15 degree increments
			if (Input.GetKey(snapKey))
			{
				ghostPart.rotation.x = MathExt.RoundFloat(newAngles.x, snapAngle);
				ghostPart.rotation.y = MathExt.RoundFloat(newAngles.y, snapAngle);
			}
			else
			{
				ghostPart.rotation.x = newAngles.x;
				ghostPart.rotation.y = newAngles.y;
			}
		}
	}

	public override void InteractEnd()
	{
		List<IEditCommand> newCommands = new();
		if (ghostPart.rotation != selectedPart.data.rotation)
		{
			newCommands.Add(new CommandRotate(selectedPart.data, ghostPart.rotation));
		}

		if (ghostPart.length != selectedPart.data.length)
		{
			newCommands.Add(new CommandChangeLength(selectedPart.data, ghostPart.length));
		}

		if (ghostPart.scale != selectedPart.data.scale)
		{
			newCommands.Add(new CommandChangeScale(selectedPart.data, ghostPart.scale));
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
