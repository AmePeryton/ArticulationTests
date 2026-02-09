using System.Collections.Generic;
using UnityEngine;

public class NewGizmoDistalBall : NewGizmoController
{
	public int distalMode;	// too lazy to make an enum for this
	/* Modes:
		* 0: rotation only
		* 1: scale.z only
		* 2: both */

	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 clickPosition)
	{
		base.InteractStart(clickPosition);
		ghostPart.SetMainCubeVisible(true);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		// If pressed [keyToggleDistalMode], switch between rotation only, scale/builk only, and both together (free)
		// if pressed [keyToggleAxial], toggle isAxial value (if applicable)
		// if holding [keySnap], move according to other rules but then round to the grid

		if (ghostPart.isAxial)
		{
			switch (ghostPart.symmetryType)
			{
				case SymmetryType.Asymmetrical:
					// If axial and Asymmetrical, print an error message
					Debug.Log("An asymmetrical part should not be axial!");
					break;
				case SymmetryType.Bilateral:
					// If axial and bilateral, move on the plane of symmetry
					switch (distalMode)
					{
						case 0:
							MoveOnRadiusCircle();
							break;
						case 1:
							MoveOnDirection();
							break;
						case 2:
							MoveOnPlane();
							break;
					}
					break;
				case SymmetryType.RadialRotate:
					// If axial and radial, move on the axis of symmetry
					switch (distalMode)
					{
						case 0:
							MoveOnRadiusLine();
							break;
						case 1:
							MoveOnDirection();
							break;
						case 2:
							MoveOnAxis();
							break;
					}
					break;
				default:
					break;
			}
		}
		else
		{
			// If not axial, move on all 3 dimensions
			switch(distalMode)
			{
				case 0:
					MoveOnRadiusSphere();
					break;
				case 1:
					MoveOnDirection();
					break;
				case 2:
					MoveFree();
					break;
			}
		}

		// Apply snapping afterwards
		if (ghostPart.isSnappingEnabled)
		{
			switch (distalMode)
			{
				case 0:
					ghostPart.rotation = MathExt.RoundVector3(ghostPart.rotation, 15f);
					break;
				case 1:
					ghostPart.scale.z = Mathf.Round(ghostPart.scale.z * 20) / 20;
					break;
				case 2:
					ghostPart.rotation = MathExt.RoundVector3(ghostPart.rotation, 15f);
					ghostPart.scale.z = Mathf.Round(ghostPart.scale.z * 20) / 20;
					break;
			}
		}

		// NOTE: snapping angles for axial parts has the potential to take them off the plane / axis if it is not perfectly on one of the cardinal directions
		// Will need to come up with a better solution for that
	}

	// On ending interaction with this gizmo, return an editCommand or multiEditCommand based on changes taken
	public override INewEditCommand InteractEnd()
	{
		ghostPart.SetMainCubeVisible(false);

		Vector3 newRotation = GetSerializedRotation(ghostPart.rotation);
		Vector3 newScale = ghostPart.scale;
		//Vector3 newBulkOffset = GetSerializedBulkOffset(ghostPart.bulkOffset);
		bool newIsAxial = ghostPart.isAxial;
		int newNumReps = ghostPart.numReps;

		List<INewEditCommand> commands = new();

		if (newRotation != ghostPart.selectedPart.data.sRef.rotation)
		{
			commands.Add(new NewCommandChangeRotation(ghostPart.selectedPart.data.sRef, newRotation));
		}
		if (newScale != ghostPart.selectedPart.data.sRef.scale)
		{
			commands.Add(new NewCommandChangeScale(ghostPart.selectedPart.data.sRef, newScale));
		}
		//if (newBulkOffset != ghostPart.selectedPart.data.sRef.bulkOffset)
		//{
		//	commands.Add(new NewCommandChangeBulkOffset(ghostPart.selectedPart.data.sRef, newBulkOffset));
		//}
		if (newIsAxial != ghostPart.selectedPart.data.sRef.isAxial)
		{
			commands.Add(new NewCommandToggleIsAxial(ghostPart.selectedPart.data.sRef));
		}
		if (newNumReps != ghostPart.selectedPart.data.sRef.numReps)
		{
			commands.Add(new NewCommandChangeNumReps(ghostPart.selectedPart.data.sRef, newNumReps));
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
		transform.localPosition = (ghostPart.scale.z + ghostPart.bulkOffset.z) * Vector3.forward;
		transform.localScale = ghostPart.zoomScale * 0.02f * Vector3.one;
	}

	// Move the distal ball perpendicular to camera, keeping the same depth from the camera
	private void MoveFree()
	{
		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get perpendicular distance from the proximal ball to the camera
		float depth = Camera.main.transform.InverseTransformPoint(prevPoint).z;
		// Get world position of the mouse at the given depth from the camera
		Vector3 rawNewPoint = MouseToWorldScreen(depth);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);
		// Calculate the new scale based on the new distal ball position, clamped to the minimum and maximum values
		float newLength = Mathf.Clamp(Vector3.Distance(ghostPart.position, localNewPoint) - ghostPart.bulkOffset.z, TechnicalConfig.minScale, TechnicalConfig.maxScale);
		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part scale.z to this calculated distance
		ghostPart.scale.z = newLength;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation
	// While originally planned to project to the surface of a sphere (hence the name), currently works identical to MoveFree save for the scale change
	private void MoveOnRadiusSphere()
	{
		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get perpendicular distance from the proximal ball to the camera
		float depth = Camera.main.transform.InverseTransformPoint(prevPoint).z;
		// Get world position of the mouse at the given depth from the camera
		Vector3 rawNewPoint = MouseToWorldScreen(depth);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);
		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation but staying on the symmetry plane
	private void MoveOnRadiusCircle()
	{
		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;	// For positions
		Vector3 flipB = Vector3.one;	// For rotations / directions
		if (ghostPart.parentPart != null)
		{
			if (ghostPart.parentPart.data.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.plaxisPoint);

		// Translate the plane from parent's space to world space
		Plane worldPlane = new(
			ghostPart.parentTransform.TransformDirection(truePlaxisDirection),
			ghostPart.parentTransform.TransformPoint(truePlaxisPoint));

		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane (with a fallback value of the distal ball position)
		Vector3 rawNewPoint = MouseToWorldPlane(worldPlane, prevPoint);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);

		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation but staying on the symmetry axis (only 2 possible configurations)
	private void MoveOnRadiusLine()
	{
		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;	// For positions
		Vector3 flipB = Vector3.one;	// For rotations / directions
		if (ghostPart.parentPart != null)
		{
			if (ghostPart.parentPart.data.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.plaxisPoint);

		// Translate the axis and point from the parent's space into world space
		Vector3 worldAxis = ghostPart.parentTransform.TransformDirection(truePlaxisDirection);
		Vector3 worldPoint = ghostPart.parentTransform.TransformPoint(truePlaxisPoint);

		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane
		Vector3 rawNewPoint = MouseToWorldLine(worldAxis, worldPoint, prevPoint);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);

		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}

	// Move the distal ball to stay at the same rotation, changing only scale and clamping to a minimum length
	private void MoveOnDirection()
	{
		// Translate the direction and position from the parent's space into world space
		Vector3 worldDirection = ghostPart.transform.forward;
		Vector3 worldPosition = ghostPart.parentTransform.TransformPoint(ghostPart.position);

		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane
		Vector3 rawNewPoint = MouseToWorldLine(worldDirection, worldPosition, prevPoint);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);
		// Calculate the new scale based on the new distal ball position, clamped to the minimum and maximum values
		float newLength = Mathf.Clamp(Vector3.Distance(ghostPart.position, localNewPoint) - ghostPart.bulkOffset.z, TechnicalConfig.minScale, TechnicalConfig.maxScale);

		// If the raw point is behind the proximal ball compared to the original distal ball position, clamp to minimum value
		if (Vector3.Distance(rawNewPoint, prevPoint) > Vector3.Distance(rawNewPoint, 2 * ghostPart.parentTransform.TransformPoint(ghostPart.position) - prevPoint))
		{
			newLength = TechnicalConfig.minScale;
		}

		// Set ghost part scale.z to this calculated distance
		ghostPart.scale.z = newLength;
	}

	// Move the distal ball on the plane of symmetry
	private void MoveOnPlane()
	{
		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;	// For positions
		Vector3 flipB = Vector3.one;	// For rotations / directions
		if (ghostPart.parentPart != null)
		{
			if (ghostPart.parentPart.data.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.plaxisPoint);

		// Translate the plane from parent's space to world space
		Plane worldPlane = new(
			ghostPart.parentTransform.TransformDirection(truePlaxisDirection),
			ghostPart.parentTransform.TransformPoint(truePlaxisPoint));

		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane (with a fallback value of the distal ball position)
		Vector3 rawNewPoint = MouseToWorldPlane(worldPlane, prevPoint);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);

		// Calculate the new scale based on the new distal ball position, clamped to the minimum and maximum values
		float newLength = Mathf.Clamp(Vector3.Distance(ghostPart.position, localNewPoint) - ghostPart.bulkOffset.z, TechnicalConfig.minScale, TechnicalConfig.maxScale);
		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part scale.z to this calculated distance
		ghostPart.scale.z = newLength;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}

	// Move the distal ball on the axis of symmetry
	private void MoveOnAxis()
	{
		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;	// For positions
		Vector3 flipB = Vector3.one;	// For rotations / directions
		if (ghostPart.parentPart != null)
		{
			if (ghostPart.parentPart.data.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.plaxisPoint);

		// Translate the axis and point from the parent's space into world space
		Vector3 worldAxis = ghostPart.parentTransform.TransformDirection(truePlaxisDirection);
		Vector3 worldPoint = ghostPart.parentTransform.TransformPoint(truePlaxisPoint);

		// Get fallback position as the original distal ball location in world space
		Vector3 prevPoint = ghostPart.parentTransform.TransformPoint(ghostPart.selectedPart.data.position) + (ghostPart.selectedPart.data.scale.z + ghostPart.selectedPart.data.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane
		Vector3 rawNewPoint = MouseToWorldLine(worldAxis, worldPoint, prevPoint);
		// Get the point in the part's parent's local space
		Vector3 localNewPoint = ghostPart.parentTransform.InverseTransformPoint(rawNewPoint);

		// Calculate the new scale based on the new distal ball position, clamped to the minimum and maximum values
		float newLength = Mathf.Clamp(Vector3.Distance(ghostPart.position, localNewPoint) - ghostPart.bulkOffset.z, TechnicalConfig.minScale, TechnicalConfig.maxScale);
		// Set ghost part scale.z to this calculated distance
		ghostPart.scale.z = newLength;

		// Calculate the new rotation to point to the distal ball
		Vector3 newRotation = Quaternion.LookRotation(localNewPoint - ghostPart.position).eulerAngles;
		// Set ghost part rotation (excluding z rotation)
		newRotation.z = ghostPart.rotation.z;
		ghostPart.rotation = newRotation;
	}
}

/* Distal Ball: 
	* change rotation
	* change scale
	* change bulkOffset???
	* change isAxial
	* change numReps (if isAxial is changed)
	*/

// NOTE: if bulk offset z value is full length of body, the distal and proximal balls will be in the same position
// Best solution would be to limit offset to something like 0.95 * scale.z
