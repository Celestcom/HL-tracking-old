using UnityEngine;
using System.Collections;

public class ForwardIndicator : MonoBehaviour 
{
	public bool DrawIndicator = true;
	public Color GizmoColor = Color.yellow;
	public Vector2 clip = new Vector2(.25f, 1.5f);
	void OnDrawGizmos() 
	{
		if (DrawIndicator)
		{
			Gizmos.matrix = transform.localToWorldMatrix;           // For the rotation bug
			Gizmos.color = GizmoColor;
			Gizmos.DrawFrustum(transform.position, Camera.main.fieldOfView, clip.x, clip.y, Camera.main.aspect);
		}
	}
}
