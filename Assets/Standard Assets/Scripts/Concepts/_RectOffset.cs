using System;
using UnityEngine;

[Serializable]
public struct _RectOffset
{
	public Vector2 offsetMin;
	public Vector2 offsetMax;

	public _RectOffset (Vector2 offsetMin, Vector2 offsetMax)
	{
		this.offsetMin = offsetMin;
		this.offsetMax = offsetMax;
	}

	public Rect Apply (Rect rect)
	{
		Rect output = rect;
		output.min += offsetMin;
		output.max += offsetMax;
		return output;
	}
}