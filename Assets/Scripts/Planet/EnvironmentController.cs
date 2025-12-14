using UnityEngine;

// Calculate and control the local environment
public class EnvironmentController : MonoBehaviour
{
	public static EnvironmentController Instance { get; private set; }

	public float localTime;
	public float cloudCover;

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		localTime = MathExt.PosMod(PlanetInfo.Instance.timeOfDay + SpacialInfo.Instance.LatLong.y / 360f, 1);
	}
}
