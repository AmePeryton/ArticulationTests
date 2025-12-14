using UnityEngine;

public class CreatureCreatorCameraController : ThirdPersonCameraController
{
	private void Awake()
	{
		CameraInit();
	}

	private void Update()
	{
		StandardUpdate();
	}

	// Move with mouse drag
	public override void SetMove()
	{
		focusPointTarget -= (moveSettings.zoomFactor * zoomCurrent + moveSettings.speed) * Input.GetAxis("Mouse Y") * transform.up;
		focusPointTarget -= (moveSettings.zoomFactor * zoomCurrent + moveSettings.speed) * Input.GetAxis("Mouse X") * transform.right;
	}

	// Rotate with M1 drag
	public override void SetRotate()
	{
		angleTarget.x = Mathf.Clamp(angleTarget.x + rotateSettings.speedVertical * Input.GetAxis("Mouse Y"),
			rotateSettings.verticalLimits.x, rotateSettings.verticalLimits.y);
		angleTarget.y -= rotateSettings.speedHorizontal * Input.GetAxis("Mouse X");
	}

	// Zoom with mousewheel scroll
	public override void SetZoom()
	{
		// Input mouse scroll and clamp 0-1
		zoomPercent -= Input.mouseScrollDelta.y * zoomSettings.step;
		zoomPercent = Mathf.Clamp01(zoomPercent);
	}

	// Offset based on panels covering screen
	public override void SetOffset()
	{
		//offsetTarget.x = -0.3f * zoomCurrent;
	}

	// Reset to original values with spacebar
	public override void ResetCamera()
	{
		CameraInit();
	}
}
