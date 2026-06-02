using SlimeJump;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogAndStory
{
	//[ExecuteInEditMode]
	public class Conversation : MonoBehaviour
	{
		public Dialog[] dialogs = new Dialog[0];
		public Coroutine updateRoutine;
		[HideInInspector]
		public Dialog lastBeganDialog;
		
		public virtual void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
                EditorApplication.update += DoEditorUpdate;
			else
				EditorApplication.update -= DoEditorUpdate;
#endif
		}
		
		public virtual IEnumerator UpdateRoutine ()
		{
			foreach (Dialog dialog in dialogs)
			{
				lastBeganDialog = dialog;
				DialogManager.Instance.StartDialog (dialog);
				yield return new WaitUntil(() => (!dialog.IsActive));
				DialogManager.instance.EndDialog (dialog);
			}
			yield break;
		}
		
		public virtual void OnDisable ()
		{
#if UNITY_EDITOR
			EditorApplication.update -= DoEditorUpdate;
			if (!Application.isPlaying)
				return;
#endif
			if (updateRoutine != null)
			{
				StopCoroutine (updateRoutine);
				updateRoutine = null;
				foreach (Dialog dialog in dialogs)
					DialogManager.instance.EndDialog (dialog);
			}
		}
		
#if UNITY_EDITOR
		public virtual void DoEditorUpdate ()
		{
			if (dialogs.Length == 0)
				dialogs = GetComponentsInChildren<Dialog>();
			foreach (Dialog dialog in dialogs)
				dialog.conversation = this;
		}
#endif
	}
}