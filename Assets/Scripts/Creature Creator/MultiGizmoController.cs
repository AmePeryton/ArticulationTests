using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// Controls interaction between the gizmos of a body part and the data of the body part
public class MultiGizmoController : MonoBehaviour
{
	public BodyPartController bodyPartController;
	//public GizmoType gizmoType;
	public UnityEvent command;
	public Vector3 hitPosition; // Where the mouse first clicked in gizmo local space

	// Keys
	private const KeyCode snapKey = KeyCode.LeftControl;
	private const KeyCode alternateKey = KeyCode.LeftShift;

	// Minimum scale value
	private const float scaleLimit = 0.05f;
	// Distance snapping value
	private const float snapDistance = 0.05f;
	// Angle snapping value
	private const float snapAngle = 15;

	public void ExecuteCommand()
	{
		command.Invoke();
	}

	// Move the part's position when the proximal ball is dragged
	[Obsolete]
	public void ProximalBall()
	{
		Vector3 rawNewPosition;

		if (bodyPartController.data.parentId == -1)
		{
			// If root part, move in worldspace
			rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) - 
				(transform.TransformPoint(hitPosition) - transform.position);
		}
		else
		{
			// If not root, move on parent's surface, or if also holding shift, move on any other non-child body part
			bool success;
			if (Input.GetKey(alternateKey))
			{
				// get all child parts and symmetry parts and exclude them from the surface search
				rawNewPosition = MouseToNonChildSurface(bodyPartController.transform, out success, out BodyPartController hitPart);

				if (hitPart != null && hitPart != bodyPartController.transform.parent.GetComponent<BodyPartController>())
				{
					bodyPartController.data.position = hitPart.transform.InverseTransformPoint(bodyPartController.transform.position);
					bodyPartController.ChangeParent(hitPart);
				}
			}
			else
			{
				rawNewPosition = MouseToColliderSurface(bodyPartController.transform.parent.GetComponent<BodyPartController>().raycastCollider, out success, true);
			}

			if (!success)
			{
				// If the raycast didn't hit the parent's collider, stay in the last position
				rawNewPosition = transform.position;
			}
		}

		Vector3 localNewPosition = bodyPartController.transform.parent.InverseTransformPoint(rawNewPosition);

		// If holding left control, move on 0.1m increments
		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		// Translate worldspace position to local position to store in the data
		bodyPartController.data.position = localNewPosition;
	}

	// Change the part's rotation and length when the distal ball is dragged
	[Obsolete]
	public void DistalBall()
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = bodyPartController.transform.parent.InverseTransformPoint(rawNewPosition);

		// If holding left shift, change length, otherwise change angles
		if (Input.GetKey(alternateKey))
		{
			float newLength = Vector3.Distance(localNewPosition, bodyPartController.data.position);
			float diff = bodyPartController.data.scale.z - bodyPartController.data.length;

			// If holding left control, snap to 0.1m increments
			if (Input.GetKey(snapKey))
			{
				newLength = MathExt.RoundFloat(newLength, snapDistance);
			}

			bodyPartController.data.length = newLength;
			bodyPartController.data.scale.z = newLength + diff;
		}
		else
		{
			Vector3 newAngles = Quaternion.LookRotation(localNewPosition - bodyPartController.data.position).eulerAngles;

			// If holding left control, snap to 15 degree increments
			if (Input.GetKey(snapKey))
			{
				bodyPartController.data.rotation.x = MathExt.RoundFloat(newAngles.x, snapAngle);
				bodyPartController.data.rotation.y = MathExt.RoundFloat(newAngles.y, snapAngle);
			}
			else
			{
				bodyPartController.data.rotation.x = newAngles.x;
				bodyPartController.data.rotation.y = newAngles.y;
			}
		}
	}

	// Change the part's position on the X Y or Z axis
	[Obsolete]
	public void PositionArrow(int axis)
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.parent.position);
		Vector3 localNewPosition = bodyPartController.transform.parent.InverseTransformPoint(rawNewPosition);

		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		switch (axis)
		{
			case 0:
				//CommandMove command = new(bodyPartController.data, new Vector3(localNewPosition.x, bodyPartController.data.position.y, bodyPartController.data.position.z));
				bodyPartController.data.position.x = localNewPosition.x;
				break;
			case 1:
				bodyPartController.data.position.y = localNewPosition.y;
				break;
			case 2:
				bodyPartController.data.position.z = localNewPosition.z;
				break;
			default: 
				break;
		}
	}

	[Obsolete]
	public void RotationBall()
	{
		float collisionRange = 0.1f;

		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(hitPosition)));
		Vector3 localNewPosition = transform.InverseTransformPoint(rawNewPosition);

		if (Mathf.Abs(hitPosition.x) < Mathf.Abs(hitPosition.y) && Mathf.Abs(hitPosition.x) < Mathf.Abs(hitPosition.z))
		{
			if (Mathf.Abs(hitPosition.x) < collisionRange)
			{
				RotationRing(0, localNewPosition);
			}
		}
		else if (Mathf.Abs(hitPosition.y) < Mathf.Abs(hitPosition.x) && Mathf.Abs(hitPosition.y) < Mathf.Abs(hitPosition.z))
		{
			if (Mathf.Abs(hitPosition.y) < collisionRange)
			{
				RotationRing(1, localNewPosition);
			}
		}
		else
		{
			if (Mathf.Abs(hitPosition.z) < collisionRange)
			{
				RotationRing(2, localNewPosition);
			}
		}
	}

	[Obsolete]
	public void RotationRing(int axis, Vector3 localNewPosition)
	{
		Quaternion q = Quaternion.identity;
		float angleOld, angleNew, angleDiff;
		switch (axis)
		{
			case 0:
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.y, hitPosition.z));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.y, localNewPosition.z));
				angleDiff = angleOld - angleNew;
				Debug.Log(angleDiff);
				//if (Input.GetKey(snapKey))
				//{
				//	//angleOld = MathExt.RoundFloat(angleOld, snapAngle);
				//	//angleNew = MathExt.RoundFloat(angleNew, snapAngle);
				//	//angleNew = MathExt.RoundFloat(angleNew, snapAngle);
				//	angleDiff = angleOld - angleNew - (bodyPartController.data.rotation.x - MathExt.RoundFloat(bodyPartController.data.rotation.x, snapAngle));
				//	//angleDiff = MathExt.RoundFloat(angleOld - angleNew, snapAngle);
				//}
				q = Quaternion.AngleAxis(angleDiff, transform.right) * Quaternion.Euler(bodyPartController.data.rotation);
				//if (Input.GetKey(snapKey))
				//{
				//	q = Quaternion.Euler(new (MathExt.RoundFloat(q.eulerAngles.x, snapAngle), q.eulerAngles.y, q.eulerAngles.z));
				//}
				bodyPartController.data.rotation = q.eulerAngles;
				break;
			case 1:
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.x, hitPosition.z));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.x, localNewPosition.z));
				angleDiff = angleOld - angleNew;
				//if (Input.GetKey(snapKey))
				//{
				//	angleDiff = MathExt.RoundFloat(angleDiff, snapAngle);
				//}
				q = Quaternion.AngleAxis(-angleDiff, transform.up) * Quaternion.Euler(bodyPartController.data.rotation);
				bodyPartController.data.rotation = q.eulerAngles;
				break;
			case 2:
				angleOld = MathExt.DirectionAngle(Vector2.zero, new(hitPosition.x, hitPosition.y));
				angleNew = MathExt.DirectionAngle(Vector2.zero, new(localNewPosition.x, localNewPosition.y));
				angleDiff = angleOld - angleNew;
				//if (Input.GetKey(snapKey))
				//{
				//	angleDiff = MathExt.RoundFloat(angleDiff, snapAngle);
				//}
				q = Quaternion.AngleAxis(angleDiff, transform.forward) * Quaternion.Euler(bodyPartController.data.rotation);
				if (Input.GetKey(snapKey))
				{
					q = Quaternion.Euler(new(q.eulerAngles.x, q.eulerAngles.y, MathExt.RoundFloat(q.eulerAngles.z, snapAngle)));
				}
				bodyPartController.data.rotation = q.eulerAngles;
				break;
			default:
				break;
		}
	}

	[Obsolete]
	public void BulkQuad(int index)
	{
		Vector3 surfaceHitPosition = new(hitPosition.x, hitPosition.y, 0);
		Vector3 rawNewPosition = MouseToWorldPlane(new(-Camera.main.transform.forward.normalized, transform.TransformPoint(surfaceHitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = bodyPartController.transform.InverseTransformPoint(rawNewPosition);

		if (Input.GetKey(snapKey))
		{
			localNewPosition = MathExt.RoundVector3(localNewPosition, snapDistance);
		}

		float diff;
		switch (index)
		{
			case 0:
				diff = Mathf.Min(localNewPosition.x, -scaleLimit / 2) - 
					(bodyPartController.data.bulkOffset.x - (bodyPartController.data.scale.x / 2));
				bodyPartController.data.bulkOffset.x += diff / 2;
				bodyPartController.data.scale.x -= diff;
				break;
			case 1:
				diff = Mathf.Max(localNewPosition.x, scaleLimit / 2) - 
					(bodyPartController.data.bulkOffset.x + (bodyPartController.data.scale.x / 2));
				bodyPartController.data.bulkOffset.x += diff / 2;
				bodyPartController.data.scale.x += diff;
				break;
			case 2:
				diff = Mathf.Min(localNewPosition.y, -scaleLimit / 2) - 
					(bodyPartController.data.bulkOffset.y - (bodyPartController.data.scale.y / 2));
				bodyPartController.data.bulkOffset.y += diff / 2;
				bodyPartController.data.scale.y -= diff;
				break;
			case 3:
				diff = Mathf.Max(localNewPosition.y, scaleLimit / 2) - 
					(bodyPartController.data.bulkOffset.y + (bodyPartController.data.scale.y / 2));
				bodyPartController.data.bulkOffset.y += diff / 2;
				bodyPartController.data.scale.y += diff;
				break;
			case 4:
				localNewPosition.z -= bodyPartController.data.length / 2;
				diff = Mathf.Min(localNewPosition.z, - bodyPartController.data.length / 2) - 
					(bodyPartController.data.bulkOffset.z - (bodyPartController.data.scale.z / 2));
				bodyPartController.data.bulkOffset.z += diff / 2;
				//bodyPartController.data.length += diff;
				bodyPartController.data.scale.z -= diff;
				break;
			case 5:
				// NOTE: for front face, length increases instead of center offsetting
				localNewPosition.z -= bodyPartController.data.length / 2;
				diff = Mathf.Max(localNewPosition.z, scaleLimit / 2) - 
					(bodyPartController.data.bulkOffset.z + (bodyPartController.data.scale.z / 2));
				bodyPartController.data.length += diff;
				bodyPartController.data.scale.z += diff;
				break;
			default:
				break;
		}
	}

	[Obsolete]
	public void AngleHandle(int index)
	{
		Vector3 rawNewPosition = MouseToWorldPlane(new(transform.parent.parent.up.normalized, transform.TransformPoint(hitPosition))) -
			(transform.TransformPoint(hitPosition) - transform.position);
		Vector3 localNewPosition = transform.parent.parent.InverseTransformPoint(rawNewPosition);
		float ang = MathExt.DirectionAngle(Vector2.zero, MathExt.Flatten(localNewPosition));

		switch (index)
		{
			case 0:
				bodyPartController.data.jointLimits[0].x = Mathf.Clamp(ang - 360, bodyPartController.data.jointLimits[1].x - 360, 0);
				break;
			case 1:
				bodyPartController.data.jointLimits[1].x = Mathf.Clamp(ang, 0, bodyPartController.data.jointLimits[0].x + 360);
				break;
			case 2:
				bodyPartController.data.jointLimits[0].y = Mathf.Clamp(ang - 360, bodyPartController.data.jointLimits[1].y - 360, 0);
				break;
			case 3:
				bodyPartController.data.jointLimits[1].y = Mathf.Clamp(ang, 0, bodyPartController.data.jointLimits[0].y + 360);
				break;
			case 4:
				bodyPartController.data.jointLimits[0].z = Mathf.Clamp(ang - 360, bodyPartController.data.jointLimits[1].z - 360, 0);
				break;
			case 5:
				bodyPartController.data.jointLimits[1].z = Mathf.Clamp(ang, 0, bodyPartController.data.jointLimits[0].z + 360);
				break;
			default:
				break;
		}
	}

	// * Helper Methods *

	//// Raycast from the mouse position on screen to a line
	//public Vector3 MouseToWorldLine(Vector3 line)
	//{
	//	return Vector3.zero;
	//}

	// Raycast from the mouse position on screen to a plane
	public Vector3 MouseToWorldPlane(Plane plane)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (plane.Raycast(ray, out float dist))
		{
			return ray.GetPoint(dist);
		}

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

//public enum GizmoType
//{
//	None = 0,
//	ProximalBall,
//	DistalBall,
//	ArrowX,
//	ArrowY,
//	ArrowZ,
//	RotationBall,
//	RingX,
//	RingY,
//	RingZ,
//	QuadLeft,
//	QuadRight,
//	QuadBottom,
//	QuadTop,
//	QuadBack,
//	QuadFront,
//	AngleHandleXLow,
//	AngleHandleXHigh,
//	AngleHandleYLow,
//	AngleHandleYHigh,
//	AngleHandleZLow,
//	AngleHandleZHigh
//}

// TODO: scale gizmos with camera distance
// move child body parts when parent is scaled or lengthened