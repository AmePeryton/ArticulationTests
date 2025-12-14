using UnityEngine;

public class GizmoRotationBall : GizmoController
{
	public float collisionRange;	// Distance from the axis that the click on it will be detected

	//public override void InteractStart()
	//{
	//	base.InteractStart();
	//	ghostPart.UpdateVariables();
	//	ghostPart.mainCube.SetActive(true);
	//}

	public override void InteractHold()
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition)));
		Vector3 localNewPosition = transform.InverseTransformPoint(rawNewPosition);

		if (Mathf.Abs(hitPosition.x) < Mathf.Abs(hitPosition.y) && Mathf.Abs(hitPosition.x) < Mathf.Abs(hitPosition.z))
		{
			if (Mathf.Abs(hitPosition.x) < collisionRange)
			{
				RotateAxis('X', localNewPosition);
			}
		}
		else if (Mathf.Abs(hitPosition.y) < Mathf.Abs(hitPosition.x) && Mathf.Abs(hitPosition.y) < Mathf.Abs(hitPosition.z))
		{
			if (Mathf.Abs(hitPosition.y) < collisionRange)
			{
				RotateAxis('Y', localNewPosition);
			}
		}
		else
		{
			if (Mathf.Abs(hitPosition.z) < collisionRange)
			{
				RotateAxis('Z', localNewPosition);
			}
		}
	}

	public override void InteractEnd()
	{
		if (ghostPart.rotation != selectedPart.data.rotation)
		{
			CreatureCreatorUI.instance.DoCommand(new CommandRotate(selectedPart.data, ghostPart.rotation));
		}

		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(false);
	}

	public override void InteractCancel()
	{
		ghostPart.UpdateVariables();
		ghostPart.mainCube.SetActive(false);
	}

	private void RotateAxis(char axis, Vector3 localNewPosition)
	{
		float angleOld, angleNew, angleDiff;
		switch (axis)
		{
			case 'X':
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.y, hitPosition.z));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.y, localNewPosition.z));
				angleDiff = angleOld - angleNew;
				ghostPart.rotation = (Quaternion.AngleAxis(angleDiff, transform.parent.right) * Quaternion.Euler(ghostPart.rotation)).eulerAngles;
				break;
			case 'Y':
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.x, hitPosition.z));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.x, localNewPosition.z));
				angleDiff = angleOld - angleNew;
				ghostPart.rotation = (Quaternion.AngleAxis(-angleDiff, transform.parent.up) * Quaternion.Euler(ghostPart.rotation)).eulerAngles;
				break;
			case 'Z':
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.x, hitPosition.y));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.x, localNewPosition.y));
				angleDiff = angleOld - angleNew;
				ghostPart.rotation = (Quaternion.AngleAxis(angleDiff, transform.parent.forward) * Quaternion.Euler(ghostPart.rotation)).eulerAngles;
				break;
			default:
				break;
		}

		// Clamp angles -180 to 180
		ghostPart.rotation = new Vector3(
			(ghostPart.rotation.x + 180) % 360 - 180, 
			(ghostPart.rotation.y + 180) % 360 - 180, 
			(ghostPart.rotation.z + 180) % 360 - 180);
	}
}

// BROKEN: does not rotate on axis properly on child parts