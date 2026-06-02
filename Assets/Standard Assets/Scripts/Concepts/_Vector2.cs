using System;
using UnityEngine;

[Serializable]
public struct _Vector2
{
	public float x;
	public float y;

	public _Vector2 (float x, float y)
	{
		this.x = x;
		this.y = y;
	}

	public Vector2 ToVec2 ()
	{
		return new Vector2(x, y);
	}

	public static _Vector2 FromVec2 (Vector2 v)
	{
		return new _Vector2(v.x, v.y);
	}
}