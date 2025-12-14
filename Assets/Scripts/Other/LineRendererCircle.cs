using UnityEngine;

public class LineRendererCircle : MonoBehaviour
{
	public float radius;
	public int numPoints;

	private LineRenderer lineRenderer;

	[ContextMenu("Render Circle")]
	public void RenderCircle()
	{
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.positionCount = numPoints;

		for (int i = 0; i < numPoints; i++)
		{
			float angle = ((float)i / numPoints) * (2 * Mathf.PI);
			lineRenderer.SetPosition(i, radius * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
		}
	}
}
