using UnityEngine;

public class ReflectionTester : MonoBehaviour
{
	public GameObject plane;	// KEEP PARENTED TO SAME OBJECT AS THE BASALS

	public GameObject ABasal;
	public GameObject BBasal;

	public GameObject AMedial;
	public GameObject BMedial;

	public GameObject ADistal;
	public GameObject BDistal;

	[ContextMenu("UpdateReflection")]
	public void UpdateReflection()
	{
		// pretend these are ripped right from the data instead of the transform
		// All in local space
		Vector3 planeNormal = plane.transform.parent.InverseTransformDirection(plane.transform.up);
		Vector3 planePoint = plane.transform.localPosition;
		Vector3 Apos = ABasal.transform.localPosition;
		Vector3 Arot = ABasal.transform.localEulerAngles;

		// Get A's forward vector in local space
		Vector3 Afwd = Quaternion.Euler(Arot) * Vector3.forward;
		// Get A's up vector in local space
		Vector3 Aup = Quaternion.Euler(Arot) * Vector3.up;

		// Reflect fwd for B
		Vector3 Bfwd = 2 * new Plane(planeNormal, Vector3.zero).ClosestPointOnPlane(Afwd) - Afwd;
		// Reflect up for B
		Vector3 Bup = 2 * new Plane(planeNormal, Vector3.zero).ClosestPointOnPlane(Aup) - Aup;

		// Reflect local position for B
		Vector3 Bpos = 2 * new Plane(planeNormal, planePoint).ClosestPointOnPlane(Apos) - Apos;
		// Reflect local rotation for B
		Vector3 Brot = Quaternion.LookRotation(Bfwd, Bup).eulerAngles;

		// Set transforms
		BBasal.transform.localPosition = Bpos;
		BBasal.transform.localEulerAngles = Brot;

		// it seems like in child parts after the first reflected one, the position should be reflected on X...
		// but the rotation only makes the y and z components negative, with the same x
		// this is because the parent space gets completely reflected over the plane, BUT the x axis gets flipped
		BMedial.transform.localPosition = Vector3.Scale(new Vector3(-1, 1, 1), AMedial.transform.localPosition);
		BMedial.transform.localEulerAngles = Vector3.Scale(new Vector3(1, -1, -1), AMedial.transform.localEulerAngles);

		BDistal.transform.localPosition = Vector3.Scale(new Vector3(-1, 1, 1), ADistal.transform.localPosition);
		BDistal.transform.localEulerAngles = Vector3.Scale(new Vector3(1, -1, -1), ADistal.transform.localEulerAngles);
	}

	private void Update()
	{
		UpdateReflection();
	}
}
