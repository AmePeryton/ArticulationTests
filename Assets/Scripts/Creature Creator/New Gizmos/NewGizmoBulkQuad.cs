using System.Collections.Generic;
using UnityEngine;

public class NewGizmoBulkQuad : NewGizmoController
{
	[SerializeField]
	private BulkQuadAxis axis;
	[SerializeField]
	private int polarity;

	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 hitPosition)
	{
		base.InteractStart(hitPosition);
		ghostPart.SetMainCubeVisible(true);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		// TODO: rework this to not need to raycast to the surface every frame, maybe use MouseToWorldLine through the original hitpoint and along the axis being changed
		Vector3 surfaceHitPosition = new(hitPosition.x, hitPosition.y, 0);
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, 
			transform.TransformPoint(surfaceHitPosition))) - (transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = ghostPart.transform.InverseTransformPoint(rawNewPosition);

		float diff;
		Vector3 scaleDiff = new();
		Vector3 offsetDiff = new();
		switch (axis)
		{
			case BulkQuadAxis.X:
				diff = polarity * Mathf.Max(polarity * localNewPosition.x, TechnicalConfig.minScale / 2) - (ghostPart.bulkOffset.x + polarity * (ghostPart.scale.x / 2));
				scaleDiff.x += polarity * diff;
				offsetDiff.x += diff / 2;
				break;
			case BulkQuadAxis.Y:
				diff = polarity * Mathf.Max(polarity * localNewPosition.y, TechnicalConfig.minScale / 2) - (ghostPart.bulkOffset.y + polarity * (ghostPart.scale.y / 2));
				scaleDiff.y += polarity * diff;
				offsetDiff.y += diff / 2;
				break;
			case BulkQuadAxis.Z:
				// TODO: fix Z axis bulk limits
				localNewPosition.z -= ghostPart.scale.z / 2;
				diff = localNewPosition.z - (ghostPart.bulkOffset.z + polarity * (ghostPart.scale.z / 2));
				scaleDiff.z += polarity * diff;
				offsetDiff.z += diff * (1 - polarity) / 2;
				break;
		}

		if (ghostPart.isAxial)
		{
			if (ghostPart.symmetryType == SymmetryType.Bilateral)
			{
				scaleDiff.x *= 2;
				offsetDiff.x = 0;
			}
			else if (ghostPart.symmetryType == SymmetryType.RadialRotate)
			{
				if (axis == BulkQuadAxis.X)
				{
					scaleDiff.x *= 2;
					scaleDiff.y = scaleDiff.x;
					offsetDiff.x = 0;

				}
				else if (axis == BulkQuadAxis.Y)
				{
					scaleDiff.y *= 2;
					scaleDiff.x = scaleDiff.y;
					offsetDiff.y = 0;
				}
			}
		}

		ghostPart.scale += scaleDiff;
		ghostPart.bulkOffset += offsetDiff;
	}

	// On ending interaction with this gizmo, return an editCommand or multiEditCommand based on changes taken
	public override INewEditCommand InteractEnd()
	{
		ghostPart.SetMainCubeVisible(false);

		Vector3 newScale = ghostPart.scale;
		Vector3 newBulkOffset = GetSerializedBulkOffset(ghostPart.bulkOffset);

		List<INewEditCommand> commands = new();

		if (newScale != ghostPart.selectedPart.data.sRef.scale)
		{
			commands.Add(new NewCommandChangeScale(ghostPart.selectedPart.data.sRef, newScale));
		}
		if (newBulkOffset != ghostPart.selectedPart.data.sRef.bulkOffset)
		{
			commands.Add(new NewCommandChangeBulkOffset(ghostPart.selectedPart.data.sRef, newBulkOffset));
		}

		if (commands.Count > 1)
		{
			return (new NewMultiCommand(commands));
		}
		else if (commands.Count == 1)
		{
			return commands[0];
		}
		else
		{
			return null;
		}
	}

	// Updates visuals
	public override void UpdateVisuals()
	{
		// All visuals updated by changes to the Bulk Quads Holder in thye GBP controller
	}

	private enum BulkQuadAxis
	{
		X,
		Y,
		Z
	}
}
