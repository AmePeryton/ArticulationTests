using System;
using UnityEngine;

// NOTE: Extend this class to a different script to properly use
public abstract class ThirdPersonCameraController : MonoBehaviour
{
	[Header("Camera Settings")]
	public MoveSettings moveSettings;
	public RotateSettings rotateSettings;
	public ZoomSettings zoomSettings;
	public OffsetSettings offsetSettings;

	[Header("Focus Point")]
	public Vector3 focusPointTarget;	// Default position to point at
	public Vector3 focusPointCurrent;	// Current position pointed at

	[Header("Angle")]
	public Vector3 angleTarget;		// Target angle from target to camera
	public Vector3 angleCurrent;	// Current angle from target to camera
	protected Vector3 trueOffsetAxis;

	[Header("Zoom")]
	public float zoomPercent;	// Target percentage of zoom in/out (0 to 1)
	public float zoomTarget;	// Target distance calculated from the percentage
	public float zoomCurrent;	// Current distance that is being lerped to the target distance

	[Header("Offset Arm")]
	public GameObject offsetArm;
	public Vector3 offsetTarget;
	public Vector3 offsetCurrent;

	// Initialize the variables to their defaults
	public void CameraInit()
	{
		focusPointTarget = moveSettings.defaultFocusPoint;
		focusPointCurrent = focusPointTarget;

		angleTarget = rotateSettings.defaultRotation;
		angleCurrent = angleTarget;

		SetZoomDistance(zoomSettings.defaultDistance);
		zoomCurrent = zoomTarget;

		offsetTarget = offsetSettings.defaultOffset;
		offsetCurrent = offsetTarget;

		Apply();
	}

	// The standard execution order, can be overridden
	// May also be omitted if the constituent methods are called by a different script
	protected virtual void StandardUpdate()
	{
		Move();
		Rotate();
		Zoom();
		Offset();

		Apply();
	}

	// Abstract methods, differ depending on preferred implementation
	// Used to set variables related to the camera based on inputs
	public abstract void SetMove();
	public abstract void SetRotate();
	public abstract void SetZoom();
	public abstract void SetOffset();
	public abstract void ResetCamera();

	// Calculate movement variables
	protected void Move()
	{
		focusPointCurrent = Vector3.Lerp(focusPointTarget, focusPointCurrent, moveSettings.smoothing);
	}

	// Calculate rotation variables
	protected void Rotate()
	{
		angleCurrent = Vector3.Lerp(angleTarget, angleCurrent, rotateSettings.smoothing);

		MatrixMath();
	}

	// Apply rotation to the axis
	protected void MatrixMath()
	{
		Vector3 referenceVector = Vector3.back;	// At all angles 0, the camera should be behind target

		// Calculate rotation about all 3 axes
		Matrix4x4 rotX = new Matrix4x4(
			new Vector4(1, 0, 0, 0),
			new Vector4(0, Mathf.Cos(angleCurrent.x * Mathf.Deg2Rad), -Mathf.Sin(angleCurrent.x * Mathf.Deg2Rad), 0),
			new Vector4(0, Mathf.Sin(angleCurrent.x * Mathf.Deg2Rad), Mathf.Cos(angleCurrent.x * Mathf.Deg2Rad), 0),
			new Vector4());

		Matrix4x4 rotY = new Matrix4x4(
			new Vector4(Mathf.Cos(angleCurrent.y * Mathf.Deg2Rad), 0, Mathf.Sin(angleCurrent.y * Mathf.Deg2Rad), 0),
			new Vector4(0, 1, 0, 0),
			new Vector4(-Mathf.Sin(angleCurrent.y * Mathf.Deg2Rad), 0, Mathf.Cos(angleCurrent.y * Mathf.Deg2Rad), 0),
			new Vector4());

		Matrix4x4 rotZ = new Matrix4x4(
			new Vector4(Mathf.Cos(angleCurrent.z * Mathf.Deg2Rad), -Mathf.Sin(angleCurrent.z * Mathf.Deg2Rad), 0, 0),
			new Vector4(Mathf.Sin(angleCurrent.z * Mathf.Deg2Rad), Mathf.Cos(angleCurrent.z * Mathf.Deg2Rad), 0, 0),
			new Vector4(0, 0, 1, 0),
			new Vector4());

		// Combine rotations in reverse order with the backwards vector
		trueOffsetAxis = rotY * rotX * rotZ * referenceVector;
	}

	// Calculate zoom variables
	protected void Zoom()
	{
		// Calculate real zoom distance based on zoom percentage and zoom settings
		zoomTarget = (zoomSettings.farthest - zoomSettings.closest) * Mathf.Pow(zoomPercent, zoomSettings.exponent) + zoomSettings.closest;

		// Lerp current distance to target distance
		zoomCurrent = Mathf.Lerp(zoomTarget, zoomCurrent, zoomSettings.smoothing);
	}

	// Directly set zoom by raw distance (rather than by percentage)
	public void SetZoomDistance(float distance)
	{
		float safeDistance = Mathf.Clamp(distance, zoomSettings.closest, zoomSettings.farthest);
		zoomPercent = Mathf.Pow((safeDistance - zoomSettings.closest) / (zoomSettings.farthest - zoomSettings.closest), 1f / zoomSettings.exponent);
		zoomTarget = (zoomSettings.farthest - zoomSettings.closest) * Mathf.Pow(zoomPercent, zoomSettings.exponent) + zoomSettings.closest;
	}

	// Calculate offset variables
	public void Offset()
	{
		offsetCurrent = Vector3.Lerp(offsetTarget, offsetCurrent, offsetSettings.smoothing);
	}

	// Apply variables to the transforms
	public void Apply()
	{
		// Set gameObject position
		transform.position = focusPointCurrent + zoomCurrent * trueOffsetAxis;

		// Look at current view point
		transform.eulerAngles = -angleCurrent;

		// Set offset arm
		offsetArm.transform.localPosition = offsetCurrent;
	}

}

[Serializable]
public struct MoveSettings
{
	public Vector3 defaultFocusPoint;
	public float speed;			// The constant component of the camera speed (speed = zoomFactor * zoomCurrent + speed)
	public float zoomFactor;	// The linear component of the camera speed based on distance from the focus point
	public float smoothing;		// Value used to Lerp positions, 0 for no lerping, up to 1 for smoother zooming

	public MoveSettings(Vector3 defaultFocusPoint, float speed, float zoomFactor, float smoothing)
	{
		this.defaultFocusPoint = defaultFocusPoint;
		this.speed = speed;
		this.zoomFactor = zoomFactor;
		this.smoothing = smoothing;
	}
}

[Serializable]
public struct RotateSettings
{
	public Vector3 defaultRotation;
	public float speedHorizontal;	// Speed of horizontal rotation (about y axis)
	public float speedVertical;		// Speed of vertical rotation (about x axis)
	public float smoothing;			// Value used to Lerp positions, 0 for no lerping, up to 1 for smoother rotating
	public Vector2 verticalLimits;	// Min and max vertical angles (in degrees)

	public RotateSettings(Vector3 defaultRotation, float speedHorizontal, float speedVertical, float smoothing, Vector2 verticalLimits)
	{
		this.defaultRotation = defaultRotation;
		this.speedHorizontal = speedHorizontal;
		this.speedVertical = speedVertical;
		this.smoothing = smoothing;
		this.verticalLimits = verticalLimits;
	}
}

[Serializable]
public struct ZoomSettings
{
	public float defaultDistance;	// The starting distance the camera will be from the focus point
	public float closest;	// Smallest distance the camera can get from the focus point
	public float farthest;	// Greatest distance the camera can get from the focus point
	public float step;		// Speed of the camera zoom in percentage change per scroll delta
	public float smoothing;	// Value used to Lerp positions, 0 for no lerping, up to 1 for smoother zooming
	public float exponent;	// Exponent to define the zoom curve, higher values zoom faster at larger distances, 1 is linear

	public ZoomSettings(float closest, float farthest, float defaultDistance, float step, float smoothing, float exponent)
	{
		this.closest = closest;
		this.farthest = farthest;
		this.defaultDistance = defaultDistance;
		this.step = step;
		this.smoothing = smoothing;
		this.exponent = exponent;
	}

	/* Practical Limits:
		* -inf < defaultDistance < inf
		* closest >= 0
		* closest < farthest
		* -inf < step < inf, step < 0 for reverse scrolling, step = 0 to disable scrolling
		* 0 <= smoothing < 1, best values around 0.95 - 0.99
		* 0 < exponent < inf, recommended to stay between 1 (linear) and ~10 (highly curved, much faster farther away)
	 */
}

[Serializable]
public struct OffsetSettings
{
	public Vector3 defaultOffset;
	public float smoothing;

	public OffsetSettings(Vector3 defaultOffset, float smoothing)
	{
		this.defaultOffset = defaultOffset;
		this.smoothing = smoothing;
	}
}