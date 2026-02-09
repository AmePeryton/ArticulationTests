using System.Collections.Generic;
using UnityEngine;

public class NewGizmoProximalBall : NewGizmoController
{
	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 clickPosition)
	{
		base.InteractStart(clickPosition);
		ghostPart.SetMainCubeVisible(true);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		// If holding [keyChangeParent], move on surface of all parts that are not self, clones of self, or children of self (or children of the part's clones, etc.)
		// If holding [keyMoveProximalOnly], move the proximal ball but modify the rotation, scale, bulk offset,m etc. to keep everything else in place
		// If pressed [keyToggleAxial], toggle isAxial value (if applicable)
		// If holding [keySnap], move according to other rules but then round to the grid
			// NOTE: For parent-surface-moving actions, maybe still snap except also move to parent surface (and included corner/side/center snaps)

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
					MoveOnPlane();
					break;
				case SymmetryType.RadialRotate:
					// If axial and radial, move on the axis of symmetry
					MoveOnAxis();
					break;
				default:
					break;
			}
		}
		else
		{
			if (ghostPart.parentPart == null)
			{
				// If nonaxial and the root, move freely (on plane orthogonal to camera forward vector)
				MoveFree();
			}
			else
			{
				// If nonaxial and not the root, move on the parent part's surface
				MoveOnParent();
			}
		}
	}

	// On ending interaction with this gizmo, return an editCommand or multiEditCommand based on changes taken
	public override INewEditCommand InteractEnd()
	{
		ghostPart.SetMainCubeVisible(false);

		Vector3 newPosition = GetSerializedPosition(ghostPart.position);
		Vector3 newRotation = ghostPart.selectedPart.data.sRef.rotation;		// TEMP
		Vector3 newScale = ghostPart.scale;
		Vector3 newBulkOffset = ghostPart.selectedPart.data.sRef.bulkOffset;	// TEMP
		bool newIsAxial = ghostPart.isAxial;
		int newNumReps = ghostPart.numReps;

		List<INewEditCommand> commands = new();

		if (newPosition != ghostPart.selectedPart.data.sRef.position)
		{
			return new NewCommandChangePosition(ghostPart.selectedPart.data.sRef, newPosition);
		}
		if (newRotation != ghostPart.selectedPart.data.sRef.rotation)
		{
			commands.Add(new NewCommandChangeRotation(ghostPart.selectedPart.data.sRef, newRotation));
		}
		if (newScale != ghostPart.selectedPart.data.sRef.scale)
		{
			commands.Add(new NewCommandChangeScale(ghostPart.selectedPart.data.sRef, newScale));
		}
		if (newBulkOffset != ghostPart.selectedPart.data.sRef.bulkOffset)
		{
			commands.Add(new NewCommandChangeBulkOffset(ghostPart.selectedPart.data.sRef, newBulkOffset));
		}
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

	public override void UpdateVisuals()
	{
		transform.localPosition = Vector3.zero;
		transform.localScale = ghostPart.zoomScale * 0.03f * Vector3.one;
	}

	// Move the proximal ball perpendicular to camera, keeping the same depth from the camera
	private void MoveFree()
	{
		// Get perpendicular distance from the proximal ball to the camera
		float depth = Camera.main.transform.InverseTransformPoint(transform.position).z;
		// Get world position of the mouse at the given depth from the camera
		Vector3 rawNewPosition = MouseToWorldScreen(depth);
		// Get the point in the part's parent's local space (since it is the root, the parent is just the body controler)
		Vector3 localNewPosition = ghostPart.parentTransform.InverseTransformPoint(rawNewPosition);
		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}

	// Move the proximal ball on the parent part's surface
	private void MoveOnParent()
	{
		// Put rawPosition on parent surface
		Vector3 rawNewPosition = MouseToColliderSurface(ghostPart.parentPart.surfaceCollider, out bool success, true);

		// Get the point in the part's parent's local space
		Vector3 localNewPosition = ghostPart.parentTransform.InverseTransformPoint(rawNewPosition);

		if (!success)
		{
			// If the raycast didn't hit the parent collider, stay in the original position
			localNewPosition = ghostPart.selectedPart.data.position;
		}

		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}

	// Move the proximal ball on the plane of symmetry
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

		// Get world position of the mouse on the plane (with a fallback value of the original world position)
		Vector3 rawNewPosition = MouseToWorldPlane(worldPlane, ghostPart.selectedPart.transform.position);
		// Get the point in the part's parent's local space (since it is the root, the parent is just the body controler)
		Vector3 localNewPosition = ghostPart.parentTransform.InverseTransformPoint(rawNewPosition);
		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}

	// Move the proximal ball on the axis of symmetry
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

		// Get fallback position
		Vector3 prevPoint = ghostPart.selectedPart.transform.position + (ghostPart.scale.z + ghostPart.bulkOffset.z) * ghostPart.selectedPart.transform.forward;
		// Get world position of the mouse on the plane
		Vector3 rawNewPosition = MouseToWorldLine(worldAxis, worldPoint, prevPoint);
		// Get the point in the part's parent's local space (since it is the root, the parent is just the body controler)
		Vector3 localNewPosition = ghostPart.parentTransform.InverseTransformPoint(rawNewPosition);
		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}
}

/* Proximal Ball: 
	* change parent
	* change isAxial
	* change numReps (if isAxial is changed)
	* [DONE] change position
	* change rotation???
	* change scale???
	* change bulkOffset??????
	*/
// For now, just implement position changing