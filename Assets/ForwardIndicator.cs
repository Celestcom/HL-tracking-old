using UnityEngine;
using System.Collections;

public class ForwardIndicator : MonoBehaviour 
{
	public bool DrawIndicator = true;
	public Color GizmoColor = Color.yellow;
	void OnDrawGizmos() 
	{
		if (DrawIndicator)
		{
			Gizmos.matrix = transform.localToWorldMatrix;           // For the rotation bug
			Gizmos.color = GizmoColor;
			Gizmos.DrawFrustum(transform.position, Camera.main.fieldOfView, 1.5f, .25f, Camera.main.aspect);
		}
	}
}
