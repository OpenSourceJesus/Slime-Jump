/*
	This file defines a GameObject that can be temporarily and locally activated
*/

using System;
using SlimeJump;
using Extensions;
using UnityEngine;
using System.Collections;

[Serializable]
public class TemporaryActiveGameObject : IUpdatable
{
	public GameObject obj;
	public float duration;
	public bool realtime;
	float timer;

	public virtual void Do ()
	{
		obj.SetActive(true);
		timer = 0;
		GameManager.updatables = GameManager.updatables.Add(this);
	}

	public void DoUpdate ()
	{
		if (realtime)
			timer += Time.unscaledDeltaTime;
		else
			timer += Time.deltaTime;
		if (timer >= duration)
		{
			if (obj != null)
				obj.SetActive(false);
			GameManager.updatables = GameManager.updatables.Remove(this);
		}
	}
}