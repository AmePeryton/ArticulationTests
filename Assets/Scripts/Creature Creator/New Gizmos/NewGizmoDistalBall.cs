using System.Collections.Generic;
using UnityEngine;

public class NewGizmoDistalBall : NewGizmoController
{
	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 hitPosition)
	{
		base.InteractStart(hitPosition);
		ghostPart.SetMainCubeVisible(true);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		// If pressed [keyToggleDistalMode], switch between rotation only, scale/builk only, and both together (free)
		// if pressed [keyToggleAxial], toggle isAxial value (if applicable)
		// if holding [keySnap], move according to other rules but then round to the grid

		int distalMode = 0;	// TEMP

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
	}

	// On ending interaction with this gizmo, return an editCommand or multiEditCommand based on changes taken
	public override INewEditCommand InteractEnd()
	{
		ghostPart.SetMainCubeVisible(false);

		Vector3 newRotation = ghostPart.selectedPart.data.sRef.rotation;		// TEMP
		Vector3 newScale = ghostPart.scale;
		Vector3 newBulkOffset = ghostPart.selectedPart.data.sRef.bulkOffset;	// TEMP
		bool newIsAxial = ghostPart.isAxial;
		int newNumReps = ghostPart.numReps;

		List<INewEditCommand> commands = new();

		if (newRotation != ghostPart.selectedPart.data.sRef.rotation)
		{
			Debug.Log("Changed rotation!");
			commands.Add(new NewCommandChangeRotation(ghostPart.selectedPart.data.sRef, newRotation));
		}
		if (newScale != ghostPart.selectedPart.data.sRef.scale)
		{
			Debug.Log("Changed scale!");
			commands.Add(new NewCommandChangeScale(ghostPart.selectedPart.data.sRef, newScale));
		}
		if (newBulkOffset != ghostPart.selectedPart.data.sRef.bulkOffset)
		{
			Debug.Log("Changed bulk offset!");
			commands.Add(new NewCommandChangeBulkOffset(ghostPart.selectedPart.data.sRef, newBulkOffset));
		}
		if (newIsAxial != ghostPart.selectedPart.data.sRef.isAxial)
		{
			Debug.Log("Toggled is axial!");
			commands.Add(new NewCommandToggleIsAxial(ghostPart.selectedPart.data.sRef));
		}
		if (newNumReps != ghostPart.selectedPart.data.sRef.numReps)
		{
			Debug.Log("Changed num reps!");
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
		transform.localPosition = (ghostPart.scale.z + ghostPart.bulkOffset.z) * Vector3.forward;
		transform.localScale = ghostPart.zoomScale * 0.02f * Vector3.one;

		//transform.localPosition = new(0, 0, ghostPart.scale.z);
		//transform.localScale = ghostPart.zoomScale * 0.03f * Vector3.one;
	}

	// Move the distal ball perpendicular to camera, keeping the same depth from the camera
	private void MoveFree()
	{

	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation
	private void MoveOnRadiusSphere()
	{

	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation but staying on the symmetry plane
	private void MoveOnRadiusCircle()
	{

	}

	// Move the distal ball to stay the same distance from the proximal ball, changing only rotation but staying on the symmetry axis (only 2 possible configurations)
	private void MoveOnRadiusLine()
	{

	}

	// Move the distal ball to stay at the same rotation, changing only scale and / or bulkOffset
	// May also change rotation to 180 around if the new position is behind the proximal ball
	private void MoveOnDirection()
	{

	}

	// Move the distal ball on the plane of symmetry
	private void MoveOnPlane()
	{

	}

	// Move the distal ball on the axis of symmetry
	private void MoveOnAxis()
	{

	}
}

/* Distal Ball: 
	* change rotation
	* change scale
	* change bulkOffset
	* change isAxial
	* change numReps (if isAxial is changed)
	*/
