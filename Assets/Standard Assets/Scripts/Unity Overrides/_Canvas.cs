using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[RequireComponent(typeof(Canvas))]
	[ExecuteInEditMode]
	public class _Canvas : UpdateWhileEnabled
	{
		public Canvas canvas;
		
		void Start ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				canvas = GetComponent<Canvas>();
				return;
			}
#endif
			canvas.worldCamera = GameCamera.Instance.camera;
		}
	}
}