using UnityEngine;

public class GizmoAngleHandle : GizmoController
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
		Vector3 rawNewPosition = MouseToWorldPlane(new(transform.parent.parent.up.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = transform.parent.parent.InverseTransformPoint(rawNewPosition);
		float ang = MathExt.DirectionAngle(Vector2.zero, MathExt.Flatten(localNewPosition));

		switch (index)
		{
			case 0:
				ghostPart.jointLimits[0].x = Mathf.Clamp(ang - 360, ghostPart.jointLimits[1].x - 360, 0);
				break;
			case 1:
				ghostPart.jointLimits[1].x = Mathf.Clamp(ang, 0, ghostPart.jointLimits[0].x + 360);
				break;
			case 2:
				if (selectedPart.data.isCentered)
				{
					ghostPart.jointLimits[0].y = Mathf.Clamp(ang - 360, -180, 0);
					ghostPart.jointLimits[1].y = Mathf.Clamp(360 - ang, 0, 180);
				}
				else
				{
					ghostPart.jointLimits[0].y = Mathf.Clamp(ang - 360, ghostPart.jointLimits[1].y - 360, 0);
				}
				break;
			case 3:
				if (selectedPart.data.isCentered)
				{
					ghostPart.jointLimits[0].y = Mathf.Clamp(-ang, -180, 0);
					ghostPart.jointLimits[1].y = Mathf.Clamp(ang, 0, 180);
				}
				else
				{
					ghostPart.jointLimits[1].y = Mathf.Clamp(ang, 0, ghostPart.jointLimits[0].y + 360);
				}
				break;
			case 4:
				if (selectedPart.data.isCentered)
				{
					ghostPart.jointLimits[0].z = Mathf.Clamp(ang - 360, -180, 0);
					ghostPart.jointLimits[1].z = Mathf.Clamp(360 - ang, 0, 180);
				}
				else
				{
					ghostPart.jointLimits[0].z = Mathf.Clamp(ang - 360, ghostPart.jointLimits[1].z - 360, 0);
				}
				break;
			case 5:
				if (selectedPart.data.isCentered)
				{
					ghostPart.jointLimits[0].z = Mathf.Clamp(-ang, -180, 0);
					ghostPart.jointLimits[1].z = Mathf.Clamp(ang, 0, 180);
				}
				else
				{
					ghostPart.jointLimits[1].z = Mathf.Clamp(ang, 0, ghostPart.jointLimits[0].z + 360);
				}
				break;
			default:
				break;
		}
	}

	public override void InteractEnd()
	{
		if (ghostPart.jointLimits[0] != selectedPart.data.jointLimits[0] || ghostPart.jointLimits[1] != selectedPart.data.jointLimits[1])
		{
			CreatureCreatorUI.instance.DoCommand(new CommandChangeJointLimits(selectedPart.data, ghostPart.jointLimits));
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
