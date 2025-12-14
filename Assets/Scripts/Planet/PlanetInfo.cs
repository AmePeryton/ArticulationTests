using UnityEngine;

// Store info relating to the entire planet
public class PlanetInfo : MonoBehaviour
{
	public static PlanetInfo Instance { get; private set; }

	[Header("Orbit")]
	public float orbitProgress;     // Time of year as a 0-1 float, 0 = northern winter solstice
	public float orbitalDistance;   // Distance of the planet from the star in AU
	public float orbitSpeed;        // Speed of the orbit as compared to Earth

	[Header("Planet")]
	public float planetRadius;
	public float gravityScale;
	public float axialTilt;         // Tilt of the planet away from the sun in degrees during northern winter solstice
	public float rotateSpeed;       // Speed of the rotation as compared to Earth

	[Header("Time")]
	public float timeOfDay;			// Time of day at 0,0 latlong as a 0-1 float, 0 = midnight

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		orbitProgress = MathExt.PosMod(orbitProgress + 1f / 31536000f * orbitSpeed * Settings.Instance.timeScale * Time.deltaTime, 1);
		timeOfDay = MathExt.PosMod(timeOfDay + 1f/86400f * rotateSpeed * Settings.Instance.timeScale * Time.deltaTime, 1);
	}
}
