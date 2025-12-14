using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialMenuController : MonoBehaviour
{
	public List<UnityEvent> actions;
	public List<Sprite> icons;
	public List<string> labels;
	public TMP_Text labelCenter;
	public Vector2 centerPos;
	public float mouseAngle;

	[Header("Settings")]
	public float innerRadius;
	public float outerRadius;

	[Header("Prefabs")]
	public GameObject imagePrefab;

	// Private
	private List<Image> images;
	private int selectedIndex;

	private void Awake()
	{
		images = new List<Image>();
	}

	private void Start()
	{
		UpdateLayout();
	}

	private void Update()
	{
		UpdateVisuals();
	}

	public void ExecuteSelection()
	{
		if (selectedIndex < 0 || selectedIndex >= actions.Count)
		{
			Debug.Log("Cancelled");
			return;
		}

		actions[selectedIndex].Invoke();
	}

	public void CheckLists()
	{
		if (icons.Count < actions.Count)
		{
			while (icons.Count < actions.Count)
			{
				icons.Add(null);
			}
		}

		if (labels.Count < actions.Count)
		{
			while (labels.Count < actions.Count)
			{
				labels.Add("N/A");
			}
		}
	}

	[ContextMenu("UpdateLayout")]
	public void UpdateLayout()
	{
		CheckLists();

		foreach (Image image in images)
		{
			Destroy(image.gameObject);
		}
		images.Clear();

		for (int i = 0; i < actions.Count; i++)
		{
			float distance = (outerRadius + innerRadius) / 2;
			float angle = (2 * i + 1) * 180 / actions.Count * Mathf.Deg2Rad;
			Vector2 position = distance * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
			Image newImage = Instantiate(imagePrefab, transform).GetComponent<Image>();
			newImage.transform.localPosition = position;
			newImage.sprite = icons[i];
			images.Add(newImage.GetComponent<Image>());
		}
	}

	public void UpdateVisuals()
	{
		foreach (Image image in images)
		{
			image.color = Color.white;
		}

		transform.position = centerPos;

		Vector2 relativeMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - centerPos;
		if (Vector2.Distance(Vector2.zero, relativeMousePosition) >= innerRadius)
		{
			mouseAngle = 180 - Vector2.SignedAngle(Vector2.down, relativeMousePosition);
			selectedIndex = Mathf.FloorToInt(mouseAngle * actions.Count / 360f);
			labelCenter.text = labels[selectedIndex].ToString();
			images[selectedIndex].color = Color.blue;
		}
		else
		{
			selectedIndex = -1;
			labelCenter.text = "Cancel";
		}

		// 0: no outer ring
		// 1: single outer button
		// 2: divide up down
		// 3: divide from top, clockwise

		// other script enables radial menu
		// other script sets center
		// radial menu does stuff
		// other script disables radial menu
	}
}
