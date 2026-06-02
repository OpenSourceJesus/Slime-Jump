using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class AffectedByVortex : MonoBehaviour
	{
		public Transform trs;
		[HideInInspector]
		public Vector2 velocity;
		public static List<AffectedByVortex> instances = new List<AffectedByVortex>();

		void OnEnable ()
		{
			instances.Add(this);
		}
		
		void OnDisable ()
		{
			instances.Remove(this);
		}
	}
}