using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SlimeJump;

[ExecuteInEditMode]
public class _ParticleSystem : Spawnable
{
	public ParticleSystem particleSystem;

	public override void Start ()
	{
		base.Start ();
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			if (particleSystem == null)
				particleSystem = GetComponent<ParticleSystem>();
			return;
		}
#endif
	}
}