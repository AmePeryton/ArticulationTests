using UnityEngine;
using UnityEngine.UI;

public class GhostBodyPartController : MonoBehaviour
{
	[Header("Copied Data")]
	public int id;
	public int parentId;
	public bool isCentered;
	public Vector3 position;	// meters, from parent proximal point
	public Vector3 rotation;	// degrees, from parent's forward vector
	public float length;		// meters, proximal to distal points
	public Vector3 bulkOffset;	// meters, from center between proximal and distal points
	public Vector3 scale;		// meters, bulk size
	public Vector3[] jointLimits;	// Low, High
	public Vector3 stiffness;

	[Header("Parent")]
	public BodyPartController parent;

	[Header("Clones")]
	public int cloneIndex;
	public Vector3 reflectVectorA;
	public Vector3 reflectVectorB;
	public float rotationOffsetAngle;
	public Quaternion rotationQuaternion;

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
	public GameObject jointGizmoHolder;
	public GameObject rotationHolder;
	// Gizmos
	public GameObject proximalBall;
	public GameObject distalBall;
	public GameObject[] arrows;
	public GameObject rotationBall;
	public GameObject[] rotationRings;
	public GameObject[] bulkQuads;  // Left, Right, Bottom, Top, Back, Front
	public GameObject[] angleHandles;
	public Image[] fillImages;

	// Private vars
	private BodyPartController selectedPart;

	public float scaleMult;
	public float zoomFactor;
	public float zoomScale;

	private void Update()
	{
		UpdateVisuals();
	}

	public void UpdateVariables()
	{
		selectedPart = CreatureCreatorUI.instance.selectedBodyPart;

		if (selectedPart != null)
		{
			visuals.SetActive(true);
			transform.parent = selectedPart.transform.parent;
			id = selectedPart.data.id;
			parentId = selectedPart.data.parentId;
			isCentered = selectedPart.data.isCentered;
			position = selectedPart.data.position;
			rotation = selectedPart.data.rotation;
			length = selectedPart.data.length;
			bulkOffset = selectedPart.data.bulkOffset;
			scale = selectedPart.data.scale;
			jointLimits[0] = selectedPart.data.jointLimits[0];
			jointLimits[1] = selectedPart.data.jointLimits[1];
			stiffness = selectedPart.data.stiffness;

			parent = selectedPart.parent;

			cloneIndex = selectedPart.cloneIndex;

			reflectVectorA = selectedPart.reflectVectorA;
			reflectVectorB = selectedPart.reflectVectorB;
			rotationOffsetAngle = selectedPart.rotationOffsetAngle;
			rotationQuaternion = selectedPart.rotationQuaternion;
		}
		else
		{
			visuals.SetActive(false);
			transform.SetParent(null);
			id = -1;
			parentId = -1;
			isCentered = false;
			position = Vector3.zero;
			rotation = Vector3.zero;
			length = 0;
			bulkOffset = Vector3.zero;
			scale = Vector3.zero;
			jointLimits = new Vector3[] { Vector3.zero, Vector3.zero };
			stiffness = Vector3.zero;

			parent = null;

			cloneIndex = -1;
		}
	}

	public void UpdateVisuals()
	{
		zoomScale = scaleMult * (zoomFactor * Vector3.Distance(transform.position, Camera.main.transform.position) + 1 - zoomFactor);
		
		proximalBall.transform.localScale = zoomScale * 0.03f * Vector3.one;
		distalBall.transform.localScale = zoomScale * 0.02f * Vector3.one;
		advancedGizmoHolder.transform.localScale = zoomScale * 2 * Vector3.one;
		jointGizmoHolder.transform.localScale = zoomScale * 0.1f * Vector3.one;

		Vector3 trueCenter = length / 2 * Vector3.forward;

		transform.localPosition = Vector3.Scale(position, reflectVectorA);
		transform.localEulerAngles = Vector3.Scale(rotation, reflectVectorB);
		transform.RotateAround(Vector3.zero, Vector3.up, cloneIndex * rotationOffsetAngle);

		bulkHolder.transform.localPosition = trueCenter + bulkOffset;
		bulkHolder.transform.localScale = scale;

		proximalBall.transform.localPosition = Vector3.zero;
		distalBall.transform.localPosition = length * Vector3.forward;

		if (transform.parent != null)
		{
			advancedGizmoHolder.transform.rotation = transform.parent.rotation;
		}
		else
		{
			advancedGizmoHolder.transform.rotation = Quaternion.identity;
		}
		rotationHolder.transform.rotation = transform.rotation;

		bulkGizmoHolder.transform.localPosition = trueCenter + Vector3.Scale(bulkOffset, reflectVectorA);
		bulkGizmoHolder.transform.localScale = scale;

		angleHandles[0].transform.parent.localEulerAngles = jointLimits[0].x * Vector3.up;
		angleHandles[1].transform.parent.localEulerAngles = jointLimits[1].x * Vector3.up;
		angleHandles[2].transform.parent.localEulerAngles = jointLimits[0].y * Vector3.up;
		angleHandles[3].transform.parent.localEulerAngles = jointLimits[1].y * Vector3.up;
		angleHandles[4].transform.parent.localEulerAngles = jointLimits[0].z * Vector3.up;
		angleHandles[5].transform.parent.localEulerAngles = jointLimits[1].z * Vector3.up;

		fillImages[0].rectTransform.localEulerAngles = new Vector3(0, 270, -jointLimits[0].x);
		fillImages[1].rectTransform.localEulerAngles = new Vector3(90, 0, -jointLimits[0].y);
		fillImages[2].rectTransform.localEulerAngles = new Vector3(0, 0, -jointLimits[0].z);

		fillImages[0].fillAmount = (jointLimits[1].x - jointLimits[0].x) / 360;
		fillImages[1].fillAmount = (jointLimits[1].y - jointLimits[0].y) / 360;
		fillImages[2].fillAmount = (jointLimits[1].z - jointLimits[0].z) / 360;

		if (selectedPart != null)
		{
			if (selectedPart.data.isCentered)
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
				jointGizmoHolder.SetActive(false);
				break;
			case EditMode.Advanced: // Advanced
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(true);
				bulkGizmoHolder.SetActive(false);
				jointGizmoHolder.SetActive(false);
				break;
			case EditMode.Bulk:     // Bulk
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(true);
				jointGizmoHolder.SetActive(false);
				break;
			case EditMode.Joint:    // Joint
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(false);
				jointGizmoHolder.SetActive(true);
				break;
			default:
				normalGizmoHolder.SetActive(false);
				advancedGizmoHolder.SetActive(false);
				bulkGizmoHolder.SetActive(false);
				jointGizmoHolder.SetActive(false);
				break;
		}
	}
}
