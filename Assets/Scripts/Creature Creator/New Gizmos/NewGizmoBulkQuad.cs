using System;
using System.Collections.Generic;
using UnityEngine;

public class NewGizmoBulkQuad : NewGizmoController
{
	[SerializeField]
	private BulkQuadAxis axis;
	[SerializeField]
	private int polarity;
	[SerializeField]
	private float[] prevDists;	// The previous distances of each face from the origin point

	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 clickPosition)
	{
		base.InteractStart(clickPosition);
		ghostPart.SetMainCubeVisible(true);
		prevDists = new float[6]{ 
			-0.5f * ghostPart.scale.x + ghostPart.bulkOffset.x,
			0.5f * ghostPart.scale.x + ghostPart.bulkOffset.x,
			-0.5f * ghostPart.scale.y + ghostPart.bulkOffset.y,
			0.5f * ghostPart.scale.y + ghostPart.bulkOffset.y,
			ghostPart.bulkOffset.z,
			ghostPart.scale.z + ghostPart.bulkOffset.z};
		//Debug.Log(prevDists[0] + ", " + prevDists[1] + ", " + prevDists[2] + ", " + prevDists[3] + ", " + prevDists[4] + ", " + prevDists[5]);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		// The current position of the mouse projected onto the line along the quad face normal and intersecting the clickPosition, in world space
		Vector3 rawNewPosition = MouseToWorldLine(transform.forward, clickPosition);

		// The current position of the mouse projected to the line, in the ghost part's local space
		Vector3 localNewPosition = ghostPart.transform.InverseTransformPoint(rawNewPosition);

		float[] newDists = new float[6];
		Array.Copy(prevDists, newDists, 6);

		int ind = 2 * (int)axis + ((polarity + 1) / 2);
		switch (axis)
		{
			case BulkQuadAxis.X:
				newDists[ind] = polarity * Mathf.Clamp(polarity * localNewPosition.x, 0.5f * TechnicalConfig.minScale, 0.5f * TechnicalConfig.maxScale);
				break;
			case BulkQuadAxis.Y:
				newDists[ind] = polarity * Mathf.Clamp(polarity * localNewPosition.y, 0.5f * TechnicalConfig.minScale, 0.5f * TechnicalConfig.maxScale);
				break;
			case BulkQuadAxis.Z:
				int mod = (polarity - 1) / 2;
				newDists[ind] = polarity * Mathf.Clamp(polarity * localNewPosition.z, TechnicalConfig.minScale + mod * TechnicalConfig.minScale, TechnicalConfig.maxScale + mod * TechnicalConfig.minScale);
				break;
		}

		if (ghostPart.isAxial)
		{
			float abs = MathF.Abs(newDists[ind]);
			if (ghostPart.symmetryType == SymmetryType.Bilateral && axis == BulkQuadAxis.X)
			{
				newDists[0] = -abs;
				newDists[1] = abs;
			}
			else if (ghostPart.symmetryType == SymmetryType.RadialRotate && axis != BulkQuadAxis.Z)
			{
				newDists[0] = -abs;
				newDists[1] = abs;
				newDists[2] = -abs;
				newDists[3] = abs;
			}
		}

		// Calculate the new scale from face dists
		Vector3 newScale = new(newDists[1] - newDists[0], newDists[3] - newDists[2], newDists[5] - newDists[4]);
		// Calculate the new bulk offset from face dists
		Vector3 newBulkOffset = new(0.5f * (newDists[0] + newDists[1]), 0.5f * (newDists[2] + newDists[3]), newDists[4]);

		ghostPart.scale = newScale;
		ghostPart.bulkOffset = newBulkOffset;
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
		X = 0,
		Y,
		Z
	}
}
