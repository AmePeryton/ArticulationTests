using UnityEngine;

public class NewGizmoProximalBall : NewGizmoController
{
	// On start of interaction with this gizmo
	public override void InteractStart(Vector3 hitPosition)
	{
		/* Needs to know:
			* the selected part controller (along with its concrete and serialized data)
			* the selected part's current parent
			* the body controller (multiple reasons, including the transform and the body data to sift through)
		 */
		base.InteractStart(hitPosition);
		ghostPart.SetMainCubeVisible(true);
	}

	// On continuous interaction with this gizmo
	public override void InteractHold()
	{
		switch (ghostPart.selectedPart.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				if (ghostPart.selectedPart.data.sRef.isAxial)
				{
					Debug.Log("An asymmetrical part should not be axial!");
				}
				else
				{
					if (ghostPart.selectedPart.data.parent == null)
					{
						MoveFree();
					}
					else
					{
						MoveOnParent();
					}
				}
				break;
			case SymmetryType.Bilateral:
				if (ghostPart.selectedPart.data.sRef.isAxial)
				{
					// plane
					MoveOnPlane();
				}
				else
				{
					if (ghostPart.selectedPart.data.parent == null)
					{
						MoveFree();
					}
					else
					{
						MoveOnParent();
					}
				}
				break;
			case SymmetryType.RadialRotate:
				if (ghostPart.selectedPart.data.sRef.isAxial)
				{
					// axis
					MoveOnAxis();
				}
				else
				{
					if (ghostPart.selectedPart.data.parent == null)
					{
						MoveFree();
					}
					else
					{
						MoveOnParent();
					}
				}
				break;
			default:
				break;
		}
		/* if root:
			* if axial:
				* if asymmetrical:
					* should not happen, print error message
				* if bilateral:
					* project mouse to symmetry plane
				* if radial:
					* project mouse to symmetry axis
			* if not axial:
				* move freely
		 * if not root:
			* if axial:
				* if asymmetrical:
					* should not happen, print error message
				* if bilateral:
					* move on symmetry plane
				* if radial:
					* move on symmetry axis
			* if not axial:
				* move on parent surface
		 */
	}

	// On ending interaction with this gizmo, return an editCommand or multiEditCommand based on changes taken
	public override INewEditCommand InteractEnd()
	{
		ghostPart.SetMainCubeVisible(false);

		Vector3 newPosition = GetSerializedPosition(ghostPart.position);

		if (newPosition != ghostPart.selectedPart.data.position)
		{
			return new NewCommandChangePosition(ghostPart.selectedPart.data.sRef, newPosition);
			// if the part has a new position, translate it from whatever reflection or rotation was done on this concrete part
			// from the original serialized part 
		}
		else
		{
			return null;
		}
	}

	// Move proximal ball perpendicular to camera, without restriction
	private void MoveFree()
	{
		// Get perpendicular distance from the proximal ball to the camera
		// TODO: check if it would be better to set this once instead of recalculating over and over
		float depth = Camera.main.transform.InverseTransformPoint(transform.position).z;
		// Get world position of the mouse at the given depth from the camera
		Vector3 rawNewPosition = MouseToWorldScreen(depth);
		// Get the point in the part's parent's local space (since it is the root, the parent is just the body controler)
		Vector3 localNewPosition = ghostPart.body.transform.InverseTransformPoint(rawNewPosition);
		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}

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

	private void MoveOnPlane()
	{
		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;    // For positions
		Vector3 flipB = Vector3.one;    // For rotations / directions
		if (ghostPart.selectedPart.data.parent != null)
		{
			if (ghostPart.selectedPart.data.parent.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.selectedPart.data.sRef.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.selectedPart.data.sRef.plaxisPoint);

		// Translate the plane from parent's space to world space
		Plane worldPlane = new(
			ghostPart.selectedPart.transform.parent.InverseTransformDirection(truePlaxisDirection), 
			ghostPart.selectedPart.transform.parent.InverseTransformPoint(truePlaxisPoint));

		// Get world position of the mouse on the plane
		Vector3 rawNewPosition = MouseToWorldPlane(worldPlane);
		// Get the point in the part's parent's local space (since it is the root, the parent is just the body controler)
		Vector3 localNewPosition = ghostPart.selectedPart.transform.parent.InverseTransformPoint(rawNewPosition);
		// Set ghost part position to this local position
		ghostPart.position = localNewPosition;
	}

	private void MoveOnAxis()
	{

	}

	// Get the position after it has been un-transformed (idk what to call it, unreflected ? unrotated?
	// Basically the position to set the new serialized position such that the selected concrete goes to the position
	private Vector3 GetSerializedPosition(Vector3 concretePosition)
	{
		Vector3 serializedPosition = new();

		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;	// For positions
		Vector3 flipB = Vector3.one;    // For rotations / directions
		if (ghostPart.selectedPart.data.parent != null)
		{
			if (ghostPart.selectedPart.data.parent.IsSpaceFlipped())
			{
				// If the parent part's space (the one that this part moves in) is reflected, set flip variables
				flipA = new(-1, 1, 1);
				flipB = new(1, -1, -1);
			}
		}

		// Get true plaxis direction
		Vector3 truePlaxisDirection = Vector3.Scale(flipB, ghostPart.selectedPart.data.sRef.plaxisDirection);
		// Get true plaxis point
		Vector3 truePlaxisPoint = Vector3.Scale(flipA, ghostPart.selectedPart.data.sRef.plaxisPoint);

		switch (ghostPart.selectedPart.data.sRef.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical parts only have 1 rep, no translations needed
				serializedPosition = concretePosition;
				break;
			case SymmetryType.Bilateral:
				if (ghostPart.selectedPart.data.repIndex == 0)
				{
					// The concrete part with repIndex 0 has the same position as the serialized body part, no change needed
					// This should include axial parts as well, since they will be repIndex 0 as well
					serializedPosition = concretePosition;
				}
				else
				{
					// The concrete part with repIndex 1 has been reflected across the plane,
					// Reflect the new point again for the corresponding serialized part position
					Plane plane = new (truePlaxisDirection, truePlaxisPoint);
					serializedPosition = 2 * plane.ClosestPointOnPlane(concretePosition) - concretePosition;
				}
				break;
			case SymmetryType.RadialRotate:
				if (ghostPart.selectedPart.data.repIndex == 0)
				{
					// The concrete part with repIndex 0 has the same position as the serialized body part, no change needed
					// This should include axial parts as well, since they will be repIndex 0 as well
					serializedPosition = concretePosition;
					// NOTE: might not be necessarily true for children of nonaxial symmetrical parts
				}
				else
				{
					// The concrete parts with repindexes that are not zero have been rotated about the axis
					// Rotate the new point in the reverse direction for the corresponding serialized part position

					// Get revolution in quaternion form
					Quaternion r = Quaternion.AngleAxis(-ghostPart.selectedPart.data.repIndex * 360f / ghostPart.selectedPart.data.sRef.numReps, truePlaxisDirection);

					// Revolve position about axis
					serializedPosition = r * (concretePosition - truePlaxisPoint) + truePlaxisPoint;
				}
				break;
		}

		// Apply space flipping vectors (if space is not flipped, does nothing);
		serializedPosition = Vector3.Scale(flipA, serializedPosition);

		return serializedPosition;
	}

	public void UpdateVisuals()
	{
		transform.localPosition = Vector3.zero;
		transform.localScale = ghostPart.zoomScale * 0.03f * Vector3.one;
	}
}

/* Proximal Ball: 
	* change parent
	* change isAxial
	* change numReps (if isAxial is changed)
	* change position
	* change rotation???
	* change scale???
	* change bulkOffset???
	*/
// For now, just implement position changing