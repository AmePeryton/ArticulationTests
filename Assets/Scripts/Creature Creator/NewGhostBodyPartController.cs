using UnityEngine;

public class NewGhostBodyPartController : MonoBehaviour
{
	[Header("Copied Part Data")]
	public NewBodyController body;
	public NewBodyPartController selectedPart;
	public NewBodyPartController parentPart;
	public Transform parentTransform;	// the parent transform of the part, either another part or the body itself
	public Vector3 position;	// meters, from parent proximal point
	public Vector3 rotation;	// degrees, from parent's forward vector
	public Vector3 scale;		// meters, bulk size
	public Vector3 bulkOffset;  // meters, from center between proximal and distal points

	[Header("Gizmos")]
	public NewGizmoProximalBall proximalBall;

	[Header("Parts")]
	// Visuals
	public GameObject visuals;
	public GameObject mainCube;
	// Holders
	public GameObject bulkHolder;
	public GameObject gizmoHolder;
	public GameObject normalGizmoHolder;
	public GameObject advancedGizmoHolder;
	public GameObject bulkGizmoHolder;
	public GameObject rotationHolder;
	// Gizmos
	//public GameObject proximalBall;
	public GameObject distalBall;
	public GameObject[] arrows;
	public GameObject rotationBall;
	public GameObject[] rotationRings;
	public GameObject[] bulkQuads;  // Left, Right, Bottom, Top, Back, Front

	[Header("Visual Settings")]
	public float scaleMult;
	public float zoomFactor;
	public float zoomScale;

	private void Awake()
	{
		mainCube.SetActive(false);
		// TODO: properly activate and deactivate different visuals
	}

	private void Update()
	{
		UpdateVisuals();
	}

	public void Initialize(NewBodyController body)
	{
		this.body = body;
		parentPart = null;
		parentTransform = body.transform;
		UpdateSelection(null);
	}

	public void UpdateSelection(NewBodyPartController newSelectedPart)
	{
		selectedPart = newSelectedPart;
		SetVisible(newSelectedPart != null);
		CopyVariables(newSelectedPart);
	}

	// Copies the relevant variables from the original part into the ghost part for manipulation
	public void CopyVariables(NewBodyPartController part)
	{
		if (part != null)
		{
			if (part.data.parent != null)
			{
				parentPart = body.bodyPartControllerDict[part.data.parent];
			}
			else
			{
				parentPart = null;
			}
			parentTransform = part.transform.parent;
			position = part.data.position;
			rotation = part.data.rotation;
			scale = part.data.scale;
			bulkOffset = part.data.bulkOffset;
		}
		else
		{
			parentPart = null;
			parentTransform = body.transform;
			position = Vector3.zero;
			rotation = Vector3.zero;
			scale = Vector3.zero;
			bulkOffset = Vector3.zero;
		}
	}

	public void UpdateVisuals()
	{
		zoomScale = scaleMult * (zoomFactor * Vector3.Distance(transform.position, Camera.main.transform.position) + 1 - zoomFactor);

		proximalBall.UpdateVisuals();
		//proximalBall.transform.localScale = zoomScale * 0.03f * Vector3.one;
		distalBall.transform.localScale = zoomScale * 0.02f * Vector3.one;
		advancedGizmoHolder.transform.localScale = zoomScale * 2 * Vector3.one;

		transform.position = parentTransform.TransformPoint(position);
		Quaternion parentTrueQuaternion = parentTransform.rotation;
		Quaternion desiredRotationQuaternion = Quaternion.Euler(rotation);
		transform.rotation = parentTrueQuaternion * desiredRotationQuaternion;

		bulkHolder.transform.localPosition = bulkOffset;
		bulkHolder.transform.localScale = scale;

		//proximalBall.transform.localPosition = Vector3.zero;
		distalBall.transform.localPosition = (0.5f * scale.z + bulkOffset.z) * Vector3.forward;

		if (transform.parent != null)
		{
			advancedGizmoHolder.transform.rotation = transform.parent.rotation;
		}
		else
		{
			advancedGizmoHolder.transform.rotation = Quaternion.identity;
		}
		rotationHolder.transform.rotation = transform.rotation;

		bulkGizmoHolder.transform.localPosition = bulkOffset;
		bulkGizmoHolder.transform.localScale = scale;

		if (selectedPart != null)
		{
			if (selectedPart.data.sRef.isAxial)
			{
				arrows[0].SetActive(false);
				rotationRings[1].SetActive(false);
				rotationRings[2].SetActive(false);
			}
			else
			{
				arrows[0].SetActive(true);
				rotationRings[1].SetActive(true);
				rotationRings[2].SetActive(true);
			}
		}
	}

	// Switch edit modes and update visuals
	public void SetEditMode(EditMode editMode)
	{
		switch (editMode)
		{
			case EditMode.Normal:   // Normal
				normalGizmoHolder.SetActive(true);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(false);
				break;
			case EditMode.Advanced: // Advanced
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(true);
				bulkGizmoHolder.SetActive(false);
				break;
			case EditMode.Bulk:     // Bulk
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(true);
				break;
			default:
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(false);
				break;
		}
	}

	public void SetVisible(bool isVisible)
	{
		visuals.SetActive(isVisible);
	}

	public void SetMainCubeVisible(bool isMainCubeVisible)
	{
		mainCube.SetActive(isMainCubeVisible);
	}
}
