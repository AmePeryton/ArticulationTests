using UnityEngine;

public class WalkingFootController : MonoBehaviour
{
	public float stepDistance;
	public float raycastDistance;

	public Transform restTarget;
	public Transform directionTarget;
	public Transform floorTarget;

	public bool isMoving;
	public float incrementValue;

	private void Awake()
	{
		//directionTarget = transform;
	}

	private void Start()
	{
		Rest();
		StepFull();
	}

	public void Rest()
	{
		// Needed to raycast from underside of terrain
		Physics.queriesHitBackfaces = true;

		directionTarget.position = restTarget.position;

		floorTarget.position = directionTarget.position;

		Ray ray = new(directionTarget.position, Vector3.down);
		if (Physics.Raycast(ray, out RaycastHit info, raycastDistance, LayerMask.GetMask("Ground")))
		{
			floorTarget.position = info.point;
		}
		else
		{
			ray = new(directionTarget.position, Vector3.up);
			if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
			{
				floorTarget.position = info.point;
			}
			else
			{
				ray = new(restTarget.position, Vector3.down);
				if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
				{
					floorTarget.position = info.point;
				}
				else
				{
					ray = new(restTarget.position, Vector3.up);
					if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
					{
						floorTarget.position = info.point;
					}
					else
					{
						floorTarget.position = transform.position;
					}
				}
			}
		}
	}

	public void CheckNextStep(Vector3 direction)
	{
		// Needed to raycast from underside of terrain
		Physics.queriesHitBackfaces = true;

		directionTarget.position = restTarget.position + stepDistance * direction;

		floorTarget.position = directionTarget.position;

		Ray ray = new(directionTarget.position, Vector3.down);
		if (Physics.Raycast(ray, out RaycastHit info, raycastDistance, LayerMask.GetMask("Ground")))
		{
			floorTarget.position = info.point;
		}
		else
		{
			ray = new(directionTarget.position, Vector3.up);
			if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
			{
				floorTarget.position = info.point;
			}
			else
			{
				ray = new(restTarget.position, Vector3.down);
				if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
				{
					floorTarget.position = info.point;
				}
				else
				{
					ray = new(restTarget.position, Vector3.up);
					if (Physics.Raycast(ray, out info, raycastDistance, LayerMask.GetMask("Ground")))
					{
						floorTarget.position = info.point;
					}
					else
					{
						floorTarget.position = transform.position;
					}
				}
			}
		}
	}

	public float GetNextStepDistance()
	{
		return Vector3.Distance(transform.position, floorTarget.position);
	}

	public void StepFull()
	{
		//Vector3 moveDiff = floorTarget.position - transform.position;
		//transform.position += moveDiff;
		//directionTarget.position -= moveDiff;
		//floorTarget.position -=moveDiff;
	}

	public void StepIncrement(float value)
	{
		//Vector3 moveDiff = Vector3.Lerp(transform.position, floorTarget.position, value) - transform.position;
		//transform.position += moveDiff;
		//directionTarget.position -= moveDiff;
		//floorTarget.position -= moveDiff;
	}

	public void MoveIncrement()
	{
		Vector3 moveDiff;
		if (Vector3.Distance(transform.position, floorTarget.position) < 0.01f)
		{
			isMoving = false;
			moveDiff = floorTarget.position - transform.position;
		}
		else
		{
			isMoving = true;
			moveDiff = Vector3.Lerp(transform.position, floorTarget.position, incrementValue) - transform.position;
		}

		transform.position += moveDiff;
		directionTarget.position -= moveDiff;
		floorTarget.position -= moveDiff;
	}
}
