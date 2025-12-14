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

	public abstract void InteractHold();
	public abstract INewEditCommand InteractEnd();
	//public virtual void InteractCancel() { }

	// * Helper Methods *

	// Raycast from the mouse position on screen to a plane
	public Vector3 MouseToWorldPlane(Plane plane)
	{
		// Get ray from the camera through the mouse position in world space
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		// Check if the ray hits the given plane
		if (plane.Raycast(ray, out float dist))
		{
			// If so, return intersection point
			return ray.GetPoint(dist);
		}

		// Return zero vector as defult in case the ray does not intersect the plane
		return Vector3.zero;
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