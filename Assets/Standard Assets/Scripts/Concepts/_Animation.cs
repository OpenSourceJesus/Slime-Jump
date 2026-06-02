using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SlimeJump
{
	[Serializable]
	public class _Animation
	{
		public string name;
		public Transform parent;
		public float frameRate;
		public WrapMode wrapMode;
		[HideInInspector]
		public int playDirection;
		[HideInInspector]
		public _Animator animator;
		[HideInInspector]
		public Transform previousChild;
		[HideInInspector]
		public int nextChildIndex;
		public delegate void OnFrameChanged (int newFrameIndex);
		public event OnFrameChanged onFrameChanged;
		public bool clearOnDone;
		Coroutine switchActiveChildRoutine;
		
		public virtual void Play (int startFrameIndex = 0, bool playForwards = true)
		{
			Clear ();
			previousChild = null;
			nextChildIndex = startFrameIndex;
			if (playForwards)
				playDirection = 1;
			else
				playDirection = -1;
			switchActiveChildRoutine = animator.StartCoroutine(SwitchActiveChild ());
		}
		
		public virtual void Stop (bool clear = false)
		{
			if (switchActiveChildRoutine != null)
				animator.StopCoroutine(switchActiveChildRoutine);
			// animator.StopAllCoroutines();
			if (clear)
				Clear ();
		}
		
		public virtual void Clear ()
		{
			foreach (Transform child in parent)
				child.gameObject.SetActive(false);
		}
		
		public virtual IEnumerator SwitchActiveChild ()
		{
			while (true)
			{
				if (previousChild != null)
				{
					nextChildIndex = previousChild.GetSiblingIndex() + playDirection;
					previousChild.gameObject.SetActive(false);
					if (nextChildIndex + playDirection == parent.childCount + 1)
					{
						if (wrapMode == WrapMode.Loop)
							nextChildIndex = 0;
						else if (wrapMode == WrapMode.PingPong)
						{
							playDirection *= -1;
							nextChildIndex = previousChild.GetSiblingIndex() + playDirection;
						}
					}
					else if (nextChildIndex + playDirection == -1 && wrapMode == WrapMode.PingPong)
					{
						playDirection *= -1;
						nextChildIndex = previousChild.GetSiblingIndex() + playDirection;
					}
					else if (wrapMode != WrapMode.PingPong)
						nextChildIndex = previousChild.GetSiblingIndex() + 1;
				}
				if (nextChildIndex < parent.childCount)
				{
					previousChild = parent.GetChild(nextChildIndex);
					previousChild.gameObject.SetActive(true);
				}
				if (onFrameChanged != null)
					onFrameChanged (nextChildIndex);
				if (nextChildIndex == parent.childCount - 1 && wrapMode == WrapMode.Once)
				{
					animator.Stop (this, clearOnDone);
					yield break;
				}
				yield return new WaitForSeconds(1f / frameRate);
			}
		}
		
		public enum WrapMode
		{
			Once,
			Loop,
			PingPong,
		}
	}
}