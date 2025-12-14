using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public float lookSpeed;
	public float yRotationLimit;
	public GameObject head; // The body part with the main sight organs

	private Vector2 lookRotation = Vector2.zero;
	private const string xAxis = "Mouse X";
	private const string yAxis = "Mouse Y";

	public Vector3 bodyOffset;
	public List<WalkingFootController> feet;
	public WalkingFootController currentFoot;
	public Transform feetAvg;
	public float stepIncrementValue;

	private void Awake()
	{
		Cursor.visible = false;
		currentFoot = feet[0];
	}

	private void Update()
	{
		Look();
	}

	private void FixedUpdate()
	{
		MoveFeet();
		Move();
	}

	public void Look()
	{
		lookRotation.x += Input.GetAxis(xAxis) * lookSpeed;
		lookRotation.y += Input.GetAxis(yAxis) * lookSpeed;
		lookRotation.y = Mathf.Clamp(lookRotation.y, -yRotationLimit, yRotationLimit);
		var xQuat = Quaternion.AngleAxis(lookRotation.x, Vector3.up);
		var yQuat = Quaternion.AngleAxis(lookRotation.y, Vector3.left);
		head.transform.rotation = xQuat * yQuat;
	}

	public void MoveFeet()
	{
		Vector3 moveDir =
			Input.GetAxis("Vertical") * head.transform.forward +
			Input.GetAxis("Horizontal") * head.transform.right +
			Input.GetAxis("Jump") * head.transform.up;
		moveDir = Vector3.ClampMagnitude(moveDir, 1);

		if (moveDir.magnitude >= 0.1f)
		{
			// If buttons pressed to move...
			if (!currentFoot.isMoving)
			{
				// If current foot has stopped moving, reanalyze next steps
				// make each foot check its next step and find the foot farthest from the next step
				currentFoot = feet[0];
				foreach (WalkingFootController foot in feet)
				{
					foot.CheckNextStep(moveDir);
					if (foot.GetNextStepDistance() > currentFoot.GetNextStepDistance())
					{
						currentFoot = foot;
					}
				}
			}

			// Move the foot
			currentFoot.MoveIncrement();
		}
		else
		{
			// Else, make each foot set next step at rest position
			foreach (WalkingFootController foot in feet)
			{
				foot.Rest();
			}
		}

		//if (Vector3.Distance(currentFoot.transform.position, currentFoot.floorTarget.position) <= stopStepThreshold)
		//{
		//	currentFoot.StepFull();
		//	currentFoot = feet[0];
		//	float maxStepDistance = stopStepThreshold;
		//	foreach (WalkingFootController foot in feet)
		//	{
		//		foot.CheckNextStep(moveDir);

		//		if (foot.nextStepDist > maxStepDistance)
		//		{
		//			maxStepDistance = foot.nextStepDist;
		//			currentFoot = foot;
		//		}
		//	}
		//}

		//if (Vector3.Distance(currentFoot.transform.position, currentFoot.floorTarget.position) > stopStepThreshold)
		//{
		//	currentFoot.StepIncrement(stepIncrementValue);
		//}

		Vector3 feetAvgPoint = Vector3.zero;
		foreach (var foot in feet)
		{
			feetAvgPoint += foot.transform.position;
		}
		feetAvgPoint /= feet.Count;
		feetAvg.transform.position = feetAvgPoint;
	}

	public void Move()
	{
		Vector3 moveDiff = feetAvg.transform.position + bodyOffset - transform.position;
		transform.position += moveDiff;
		foreach (WalkingFootController foot in feet)
		{
			foot.transform.position -= moveDiff;
		}
	}
}
