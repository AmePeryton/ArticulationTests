using UnityEngine;

// Calculate and control the skybox
public class SkyboxController : MonoBehaviour
{
	public static SkyboxController Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		RenderSettings.skybox.SetFloat("_SunSize", 0.04f / PlanetInfo.Instance.orbitalDistance);
	}
}
