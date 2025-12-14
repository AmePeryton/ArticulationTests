using UnityEngine;

public class GizmoMoveArrow : GizmoController
{
	public char axis;	// Uppercase X, Y, or Z

	public override void InteractStart()
	{
		base.InteractStart();
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(true);
	}

	public override void InteractHold()
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.parent.position);
		Vector3 localNewPosition = ghostPart.transform.parent.InverseTransformPoint(rawNewPosition);

		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		switch (axis)
		{
			case 'X':
				ghostPart.position.x = localNewPosition.x;
				break;
			case 'Y':
				ghostPart.position.y = localNewPosition.y;
				break;
			case 'Z':
				ghostPart.position.z = localNewPosition.z;
				break;
			default:
				Debug.Log("Unrecognized axis: " + axis);
				break;
		}
	}

	public override void InteractEnd()
	{
		if (ghostPart.position != selectedPart.data.position)
		{
			CreatureCreatorUI.instance.DoCommand(new CommandMove(selectedPart.data, ghostPart.position));
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
