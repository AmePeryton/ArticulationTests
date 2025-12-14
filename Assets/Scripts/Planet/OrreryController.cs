using UnityEngine;

public class OrreryController : MonoBehaviour
{
	public static OrreryController Instance { get; private set; }

	public GameObject planetTilt;
	public GameObject planet;
	public GameObject locationHolder;

	public GameObject sunTracker;

	public GameObject sunSimulator;

	[Header("True Rotation")]
	public Vector3 trueRotation;

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		PlanetInfo p = PlanetInfo.Instance;
		SpacialInfo s = SpacialInfo.Instance;

		planetTilt.transform.localPosition = p.orbitalDistance * new Vector3(
			-Mathf.Sin(p.orbitProgress * 2 * Mathf.PI), 
			0, 
			Mathf.Cos(p.orbitProgress * 2 * Mathf.PI));
		planetTilt.transform.localEulerAngles = p.axialTilt * Vector3.right;
		
		planet.transform.localEulerAngles = MathExt.PosMod(p.timeOfDay + p.orbitProgress, 1) * -360f * Vector3.up;

		locationHolder.transform.localEulerAngles = new Vector3(-s.LatLong.x, -s.LatLong.y, 0);

		sunTracker.transform.localRotation = Quaternion.LookRotation(-locationHolder.transform.InverseTransformDirection(transform.position - locationHolder.transform.position));

		sunSimulator.transform.localEulerAngles = new Vector3(
			-sunTracker.transform.localEulerAngles.x,
			sunTracker.transform.localEulerAngles.y,
			sunTracker.transform.localEulerAngles.z);
	}
}
