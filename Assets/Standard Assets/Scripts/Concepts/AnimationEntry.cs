using System;
using UnityEngine;

namespace SlimeJump
{
	[Serializable]
	public struct AnimationEntry
	{
		public string animatorStateName;
		public Animator animator;
		public float length;
		public int layer;

		public void Play (float normalizedTime = float.NegativeInfinity)
		{
			animator.Play(animatorStateName, layer, normalizedTime);
		}

		public bool IsPlaying ()
		{
			return animator.GetCurrentAnimatorStateInfo(layer).IsName(animatorStateName);
		}
	}
}