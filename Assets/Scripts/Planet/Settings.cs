using UnityEngine;

// Game related settings
public class Settings : MonoBehaviour
{
	public static Settings Instance { get; private set; }

	public float timeScale;

	private void Awake()
	{
		Instance = this;
	}
}
