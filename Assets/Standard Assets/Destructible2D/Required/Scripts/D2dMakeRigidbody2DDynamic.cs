using UnityEngine;
using CW.Common;

namespace Destructible2D
{
	/// <summary>When enabled in play mode, this component will turn the attached Rigidbody2D into a dynamic one.
	/// Unity 6 removed Rigidbody2D.isKinematic, so this component simplifies cross-compatibility.</summary>
	[RequireComponent(typeof(Rigidbody2D))]
	[HelpURL(D2dCommon.HelpUrlPrefix + "D2dMakeRigidbody2DDynamic")]
	[AddComponentMenu(D2dCommon.ComponentMenuPrefix + "Make Rigidbody2D Dynamic")]
	public class D2dMakeRigidbody2DDynamic : MonoBehaviour
	{
		protected virtual void OnEnable()
		{
			var body = GetComponent<Rigidbody2D>();

			#if UNITY_6000_0_OR_NEWER
				body.bodyType = RigidbodyType2D.Dynamic;
			#else
				body.isKinematic = false;
			#endif
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			enabled = false;
		}
#endif
	}
}

#if UNITY_EDITOR
namespace Destructible2D.Inspector
{
	using UnityEditor;
	using TARGET = D2dMakeRigidbody2DDynamic;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class D2dMakeRigidbody2DDynamic_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Info("When enabled in play mode, this component will turn the attached Rigidbody2D into a dynamic one.");
		}
	}
}
#endif