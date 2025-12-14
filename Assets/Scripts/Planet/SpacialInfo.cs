using UnityEngine;

// Store info related to the location of the player
public class SpacialInfo : MonoBehaviour
{
	public static SpacialInfo Instance { get; private set; }

	public Vector2 LatLong;

	private void Awake()
	{
		Instance = this;
	}
}
