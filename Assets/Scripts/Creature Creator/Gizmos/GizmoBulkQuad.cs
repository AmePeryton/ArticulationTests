using System.Collections.Generic;
using UnityEngine;

public class GizmoBulkQuad : GizmoController
{
	public int index;

	public override void InteractStart()
	{
		base.InteractStart();
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(true);
	}

	public override void InteractHold()
	{
		Vector3 surfaceHitPosition = new(hitPosition.x, hitPosition.y, 0);
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(surfaceHitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = ghostPart.transform.InverseTransformPoint(rawNewPosition);

		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		float diff;
		switch (index)
		{
			case 0:
				diff = Mathf.Min(localNewPosition.x, -scaleLimit / 2) -
					(ghostPart.bulkOffset.x - (ghostPart.scale.x / 2));
				if (selectedPart.data.isCentered)
				{
					ghostPart.scale.x -= diff * 2;
				}
				else
				{
					ghostPart.bulkOffset.x += diff / 2;
					ghostPart.scale.x -= diff;
				}
				break;
			case 1:
				diff = Mathf.Max(localNewPosition.x, scaleLimit / 2) -
					(ghostPart.bulkOffset.x + (ghostPart.scale.x / 2));
				if (selectedPart.data.isCentered)
				{
					ghostPart.scale.x += diff * 2;
				}
				else
				{
					ghostPart.bulkOffset.x += diff / 2;
					ghostPart.scale.x += diff;
				}
				break;
			case 2:
				diff = Mathf.Min(localNewPosition.y, -scaleLimit / 2) -
					(ghostPart.bulkOffset.y - (ghostPart.scale.y / 2));
				ghostPart.bulkOffset.y += diff / 2;
				ghostPart.scale.y -= diff;
				break;
			case 3:
				diff = Mathf.Max(localNewPosition.y, scaleLimit / 2) -
					(ghostPart.bulkOffset.y + (ghostPart.scale.y / 2));
				ghostPart.bulkOffset.y += diff / 2;
				ghostPart.scale.y += diff;
				break;
			case 4:
				localNewPosition.z -= ghostPart.length / 2;
				diff = Mathf.Min(localNewPosition.z, -ghostPart.length / 2) -
					(ghostPart.bulkOffset.z - (ghostPart.scale.z / 2));
				ghostPart.bulkOffset.z += diff / 2;
				ghostPart.scale.z -= diff;
				break;
			case 5:
				// NOTE: for front face, length increases instead of center offsetting
				localNewPosition.z -= ghostPart.length / 2;
				diff = Mathf.Max(localNewPosition.z, scaleLimit / 2) -
					(ghostPart.bulkOffset.z + (ghostPart.scale.z / 2));
				ghostPart.length += diff;
				ghostPart.scale.z += diff;
				break;
			default:
				break;
		}
	}

	public override void InteractEnd()
	{
		List<IEditCommand> newCommands = new();
		if (ghostPart.bulkOffset != selectedPart.data.bulkOffset)
		{
			newCommands.Add(new CommandChangeBulkOffset(selectedPart.data, ghostPart.bulkOffset));
		}

		if (ghostPart.length != selectedPart.data.length)
		{
			newCommands.Add(new CommandChangeLength(selectedPart.data, ghostPart.length));
		}

		if (ghostPart.scale != selectedPart.data.scale)
		{
			newCommands.Add(new CommandChangeScale(selectedPart.data, ghostPart.scale));
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
