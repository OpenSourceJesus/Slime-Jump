// using System;
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// #if UNITY_EDITOR
// using Unity.EditorCoroutines.Editor;
// #endif

// public static class ThreadingUtilities
// {
// 	public class CoroutineWithData
// 	{
// 		public Coroutine coroutine;
// 		public object result;
// 		public IEnumerator target;

// 		public CoroutineWithData ()
// 		{
// 		}

// 		public CoroutineWithData (MonoBehaviour owner, IEnumerator target)
// 		{
// 			this.target = target;
// 			this.coroutine = owner.StartCoroutine(Run ());
// 		}
	 
// 		IEnumerator Run ()
// 		{
// 			while (target.MoveNext())
// 			{
// 				result = target.Current;
// 				yield return result;
// 			}
// 		}
// 	}

// 	public class CoroutineWithData<T> : CoroutineWithData
// 	{
// 	}

// 	public class WaitForReturnedValueOfType : CustomYieldInstruction
// 	{
// 		public override bool keepWaiting
// 		{
// 			get
// 			{
// 				return coroutineWithData.result == null || coroutineWithData.result.GetType() != type;
// 			}
// 		}
// 		public CoroutineWithData coroutineWithData;
// 		public Type type;

// 		public WaitForReturnedValueOfType (CoroutineWithData coroutineWithData, Type type)
// 		{
// 			this.coroutineWithData = coroutineWithData;
// 			this.type = type;
// 		}
// 	}

// 	public class WaitForReturnedValueOfType<T> : CustomYieldInstruction
// 	{
// 		public override bool keepWaiting
// 		{
// 			get
// 			{
// 				return coroutineWithData.result == null || coroutineWithData.result.GetType() != typeof(T);
// 			}
// 		}
// 		public CoroutineWithData coroutineWithData;

// 		public WaitForReturnedValueOfType (CoroutineWithData coroutineWithData)
// 		{
// 			this.coroutineWithData = coroutineWithData;
// 		}
// 	}

// #if UNITY_EDITOR
// 	public class EditorCoroutineWithData
// 	{
// 		public EditorCoroutine editorCoroutine;
// 		public object result;
// 		public IEnumerator target;

// 		public EditorCoroutineWithData ()
// 		{
// 		}

// 		public EditorCoroutineWithData (MonoBehaviour owner, IEnumerator target)
// 		{
// 			this.target = target;
// 			this.editorCoroutine = EditorCoroutineUtility.StartCoroutine(Run (), owner);
// 		}
	 
// 		IEnumerator Run ()
// 		{
// 			while (target.MoveNext())
// 			{
// 				result = target.Current;
// 				yield return result;
// 			}
// 		}
// 	}

// 	public class EditorCoroutineWithData<T> : EditorCoroutineWithData
// 	{
// 	}

// 	public class WaitForReturnedValueOfType_Editor : CustomYieldInstruction
// 	{
// 		public override bool keepWaiting
// 		{
// 			get
// 			{
// 				return editorCoroutineWithData.result == null || editorCoroutineWithData.result.GetType() != type;
// 			}
// 		}
// 		public EditorCoroutineWithData editorCoroutineWithData;
// 		public Type type;

// 		public WaitForReturnedValueOfType_Editor (EditorCoroutineWithData editorCoroutineWithData, Type type)
// 		{
// 			this.editorCoroutineWithData = editorCoroutineWithData;
// 			this.type = type;
// 		}
// 	}

// 	public class WaitForReturnedValueOfType_Editor<T> : CustomYieldInstruction
// 	{
// 		public override bool keepWaiting
// 		{
// 			get
// 			{
// 				return editorCoroutineWithData.result == null || editorCoroutineWithData.result.GetType() != typeof(T);
// 			}
// 		}
// 		public EditorCoroutineWithData editorCoroutineWithData;

// 		public WaitForReturnedValueOfType_Editor (EditorCoroutineWithData editorCoroutineWithData)
// 		{
// 			this.editorCoroutineWithData = editorCoroutineWithData;
// 		}
// 	}
// #endif
// }