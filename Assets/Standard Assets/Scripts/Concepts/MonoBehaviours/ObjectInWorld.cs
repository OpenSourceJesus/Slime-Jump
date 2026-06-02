using UnityEngine;

namespace SlimeJump
{
	public class ObjectInWorld : MonoBehaviour
	{
		public Transform trs;
		// [HideInInspector]
		public WorldPiece pieceIAmIn;
		// [HideInInspector]
		public ObjectInWorld duplicate;
		public ObjectInWorld[] objectsToLoadAndUnloadWithMe = new ObjectInWorld[0];
		public bool dontUnloadAfterLoad;
		public bool keepParented;

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (trs == null)
				trs = GetComponent<Transform>();
			if (objectsToLoadAndUnloadWithMe.Length == 0)
				objectsToLoadAndUnloadWithMe = GetComponentsInChildren<ObjectInWorld>();
		}
#endif

		void OnEnable ()
		{
			if (dontUnloadAfterLoad)
				trs.SetParent(null);
		}
	}
}