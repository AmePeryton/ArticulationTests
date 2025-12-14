using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// INCOMPLETE
public class UILineRenderer : Graphic
{
	public float lineThickness;
	public Color lineColor;
	public List<Vector2> points;

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();

		if (points.Count < 2)
		{
			return;
		}

		for (int i = 0; i < points.Count; i++)
		{

		}
	}
}
