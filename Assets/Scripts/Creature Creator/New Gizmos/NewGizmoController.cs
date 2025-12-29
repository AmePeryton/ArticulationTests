using System.Linq;
using UnityEngine;

public abstract class NewGizmoController : MonoBehaviour
{
	public NewGhostBodyPartController ghostPart;

	public Vector3 hitPosition;

	// Keys
	protected const KeyCode snapKey = KeyCode.LeftControl;
	protected const KeyCode shiftModeKey = KeyCode.LeftShift;
	protected const KeyCode specialKey = KeyCode.LeftAlt;

	// Minimum scale value
	protected const float scaleLimit = 0.05f;
	// Distance snapping value
	protected const float snapDistance = 0.05f;
	// Angle snapping value
	protected const float snapAngle = 15;
	// TODO: move these keys and snap settings to some other settings controller thing

	public virtual void InteractStart(Vector3 hitPosition)
	{
		this.hitPosition = hitPosition;
	}

	//public virtual void InteractCancel() { }

	public abstract void InteractHold();
	public abstract INewEditCommand InteractEnd();
	public abstract void UpdateVisuals();

	// * Helper Methods *

	// Raycast from the mouse position on screen to a plane
	public Vector3 MouseToWorldPlane(Plane plane, Vector3 fallbackValue = new())
	{
		// Get ray from the camera through the mouse position in world space
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		// Check if the ray hits the given plane
		if (plane.Raycast(ray, out float dist))
		{
			// If so, return intersection point
			return ray.GetPoint(dist);
		}

		// Return fallback vector as defult in case the ray does not intersect the plane
		return fallbackValue;
	}

	public Vector3 MouseToWorldAxis(Vector3 direction, Vector3 point)
	{
		Vector3 camFwd = Camera.main.transform.forward;

		Plane plane = new(
			camFwd - Vector3.Project(camFwd, direction),
			point);

		// Get ray from the camera through the mouse position in world space
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		// Default hitPoint of the input point
		Vector3 hitPoint = point;

		// Check if the ray hits the given plane
		if (plane.Raycast(ray, out float dist))
		{
			// If so, return intersection point
			hitPoint = ray.GetPoint(dist);
		}

		// Project the plane hit point to the actual axis, and account for point offset
		return Vector3.Project(hitPoint - point, direction) + point;
	}

	// Get a world point on the mouse at the given depth from the camera
	public Vector3 MouseToWorldScreen(float depth)
	{
		return Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
	}

	// Get a world point on the surface of the specific collider
	public Vector3 MouseToColliderSurface(Collider collider, out bool success, bool passThrough = false)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		// if passThrough, the raycast will detect the collider behind other colliders
		if (passThrough)
		{
			RaycastHit[] hits = Physics.RaycastAll(ray);

			if (hits.Length > 0)
			{
				foreach (RaycastHit hit in hits)
				{
					if (hit.collider == collider)
					{
						success = true;
						return hit.point;
					}
				}
			}
		}
		// if not passThrough, the raycast will only detect the collider if it is in front
		else
		{
			if (Physics.Raycast(ray, out RaycastHit hitData))
			{
				if (hitData.collider == collider)
				{
					success = true;
					return hitData.point;
				}
			}
		}

		success = false;
		return Vector3.zero;
	}

	// Get a world point on the surface of the any collider in the specified layer mask
	public Vector3 MouseToColliderSurface(LayerMask layerMask, out bool success, bool passThrough = false)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (passThrough)
		{
			RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask);

			if (hits.Length > 0)
			{
				RaycastHit hitMin = hits[0];
				foreach (RaycastHit hit in hits)
				{
					if (hit.distance < hitMin.distance)
					{
						hitMin = hit;
					}
				}
				success = true;
				return hitMin.point;
			}
		}
		else
		{
			if (Physics.Raycast(ray, out RaycastHit hitData, 100f, layerMask))
			{
				success = true;
				return hitData.point;
			}
		}

		success = false;
		return Vector3.zero;
	}

	// Get a world point on the surface of a bulk collider that is NOT a child of t
	// TODO: update this
	public Vector3 MouseToNonChildSurface(Transform t, out bool success, out BodyPartController hitPart)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, LayerMask.GetMask("Bulk"));

		if (hits.Length > 0)
		{
			hits = hits.OrderBy(hit => hit.distance).ToArray();

			foreach (RaycastHit hit in hits)
			{
				if (!hit.collider.transform.IsChildOf(t))
				{
					success = true;
					hitPart = hit.collider.transform.parent.parent.GetComponent<BodyPartController>();
					return hit.point;
				}
			}
		}

		success = false;
		hitPart = null;
		return Vector3.zero;
	}

	// Get the equivalent serialized position based on concrete position
	protected Vector3 GetSerializedPosition(Vector3 concretePosition)
	{
		Vector3 serializedPosition = new();

		// Space flipping variables (if rep index chain has an odd number of reflected parts)
		Vector3 flipA = Vector3.one;    // For positions
		Vector3 flipB = Vector3.one;    // For rotations / directions
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

		switch (ghostPart.symmetryType)
		{
			case SymmetryType.Asymmetrical:
				// Asymmetrical parts only have 1 rep, no translations needed
				serializedPosition = concretePosition;
				break;
			case SymmetryType.Bilateral:
				if (ghostPart.repIndex == 0)
				{
					// The concrete part with repIndex 0 has the same position as the serialized body part, no change needed
					// This should include axial parts as well, since they will be repIndex 0 as well
					serializedPosition = concretePosition;
				}
				else
				{
					// The concrete part with repIndex 1 has been reflected across the plane,
					// Reflect the new point again for the corresponding serialized part position
					Plane plane = new(truePlaxisDirection, truePlaxisPoint);
					serializedPosition = 2 * plane.ClosestPointOnPlane(concretePosition) - concretePosition;
				}
				break;
			case SymmetryType.RadialRotate:
				if (ghostPart.repIndex == 0)
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
					Quaternion r = Quaternion.AngleAxis(-ghostPart.repIndex * 360f / ghostPart.numReps, truePlaxisDirection);

					// Revolve position about axis
					serializedPosition = r * (concretePosition - truePlaxisPoint) + truePlaxisPoint;
				}
				break;
		}

		// Apply space flipping vectors (if space is not flipped, does nothing);
		serializedPosition = Vector3.Scale(flipA, serializedPosition);

		return serializedPosition;
	}

	// Get the equivalent serialized rotation based on concrete rotation
	protected Vector3 GetSerializedRotation(Vector3 concreteRotation)
	{
		return concreteRotation;
	}

	// Get the equivalent serialized bulkOffset based on concrete bulkOffset
	protected Vector3 GetSerializedBulkOffset(Vector3 concreteBulkOffset)
	{
		return concreteBulkOffset;
	}
}

/*
 * Menu:
	* change symmetryType
	* change numReps
	* add body part
	* delete body part
 * Proximal Ball: 
	* change parent
	* change isAxial
	* change numReps (if isAxial is changed)
	* change position
	* change rotation???
	* change scale???
	* change bulkOffset???
 * Distal Ball: 
	* change isAxial
	* change rotation
	* change scale
	* change bulkOffset
	* change numReps (if isAxial is changed)
 * Movement Arrow: 
	* change isAxial
	* change numReps (if isAxial is changed)
	* change position
	* change rotation???
	* change scale???
	* change bulkOffset???
 * Rotation Ball: 
	* change isAxial
	* change numReps (if isAxial is changed)
	* change rotation
 * Rotation Ring: 
	* change isAxial
	* change numReps (if isAxial is changed)
	* change rotation 
 * Bulk Quad: 
	* change scale
	* change bulkOffset
 * Plaxis Pont Ball:
	* change plaxisPoint
 * Plaxis Direction Arrow:
	* change plaxisDirection
 */