using System;
using TMPro;
using SlimeJump;
using Extensions;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace DialogAndStory
{
	// [ExecuteInEditMode]
	public class Dialog : UpdateWhileEnabled
	{
		public bool IsActive
		{
			get
			{
				return gameObject.activeInHierarchy && text.gameObject.activeInHierarchy;
			}
			set
			{
				gameObject.SetActive(value);
				text.gameObject.SetActive(value);
			}
		}
		public TMP_Text text;
		[Multiline(25)]
		public string textStr;
		public float writeSpeed;
		public int maxCharacters = int.MaxValue;
		public WaitEvent[] waitEvents;
		public SetMaxCharactersEvent[] setMaxCharactersEvents = new SetMaxCharactersEvent[0];
		public RequireInputDeviceEvent[] requireInputDeviceEvents;
		public CustomDialogEvent[] customDialogEvents;
		bool shouldDisplayCurrentChar;
		[HideInInspector]
		public Conversation conversation;
		[HideInInspector]
		public bool isDone;
		public bool IsDone
		{
			get
			{
				return isDone;
			}
			set
			{
				isDone = value;
			}
		}
		public UnityEvent onBeganEvent;
		public UnityEvent onDoneEvent;
		public UnityEvent onLeftWhileNotDoneEvent;
		public bool autoEnd;
#if UNITY_EDITOR
		public bool autoSetTextSize;
#endif
		string textStrCopy;
		float writeTimer;
		float writeDelayTime;
		int charIdx;

#if UNITY_EDITOR
        void OnValidate ()
        {
            if (autoSetTextSize)
                EditorCoroutineUtility.StartCoroutine(AutoSetTextSize (), this);
        }
#endif

		public override void OnEnable ()
		{
            base.OnEnable ();
			// StartCoroutine(AutoSetTextSize ());
			charIdx = 0;
			text.text = "";
			isDone = false;
			writeTimer = 0;
			writeDelayTime = 0;
		}

		public override void DoUpdate ()
		{
			textStrCopy = textStr;
            writeTimer += Time.unscaledDeltaTime;
			if (writeTimer > 1f / writeSpeed + writeDelayTime)
			{
				shouldDisplayCurrentChar = true;
				writeTimer -= (1f / writeSpeed + writeDelayTime);
				writeDelayTime = 0;
				foreach (WaitEvent waitEvent in waitEvents)
				{
					if (textStrCopy.IndexOf(waitEvent.indicator, charIdx) == charIdx)
					{
						writeDelayTime = waitEvent.duration;
						charIdx += waitEvent.indicator.Length;
						shouldDisplayCurrentChar = false;
						break;
					}
				}
				if (writeDelayTime == 0)
				{
					foreach (SetMaxCharactersEvent setMaxCharactersEvent in setMaxCharactersEvents)
					{
						if (textStrCopy.IndexOf(setMaxCharactersEvent.indicator, charIdx) == charIdx)
						{
							maxCharacters = setMaxCharactersEvent.maxCharacters;
							charIdx += setMaxCharactersEvent.indicator.Length;
							shouldDisplayCurrentChar = false;
							break;
						}
					}
					if (shouldDisplayCurrentChar)
					{
						if (textStrCopy.IndexOf(RequireInputDeviceEvent.endIndicator, charIdx) == charIdx)
						{
							charIdx += RequireInputDeviceEvent.endIndicator.Length;
							shouldDisplayCurrentChar = false;
						}
						else
						{
							foreach (RequireInputDeviceEvent requireInputDeviceEvent in requireInputDeviceEvents)
							{
								if (textStrCopy.IndexOf(requireInputDeviceEvent.startIndicator, charIdx) == charIdx)
								{
									shouldDisplayCurrentChar = false;
									if (InputSystem.devices.Contains(requireInputDeviceEvent.inputDevice) == requireInputDeviceEvent.mustHaveInputDevice)
									{
										textStrCopy = textStrCopy.Remove(textStrCopy.IndexOf(RequireInputDeviceEvent.endIndicator, charIdx), RequireInputDeviceEvent.endIndicator.Length);
										charIdx += requireInputDeviceEvent.startIndicator.Length;
									}
									else
									{
										int indexOfEventEnd = textStrCopy.IndexOf(RequireInputDeviceEvent.endIndicator, charIdx) + RequireInputDeviceEvent.endIndicator.Length;
										textStrCopy = textStrCopy.RemoveStartEnd(charIdx, indexOfEventEnd);
										charIdx = indexOfEventEnd;
									}
								}
							}
						}
						foreach (CustomDialogEvent customDialogEvent in customDialogEvents)
						{
							if (textStrCopy.IndexOf(customDialogEvent.indicator, charIdx) == charIdx)
							{
								shouldDisplayCurrentChar = false;
								charIdx += customDialogEvent.indicator.Length;
								customDialogEvent._event.Invoke();
							}
						}
					}
				}
				if (shouldDisplayCurrentChar)
				{
					if (charIdx < textStrCopy.Length)
					{
						text.text += textStrCopy[charIdx];
						while (text.text.Length > maxCharacters)
							text.text = text.text.Substring(1);
						charIdx ++;
					}
					else
					{
						isDone = true;
						onDoneEvent.Invoke ();
						if (autoEnd)
							DialogManager.Instance.EndDialog (this);
					}
				}
			}
		}

		IEnumerator AutoSetTextSize ()
		{
#if UNITY_EDITOR
			autoSetTextSize = false;
#endif
			textStrCopy = textStr;
			foreach (WaitEvent waitEvent in waitEvents)
				textStrCopy = textStrCopy.Replace(waitEvent.indicator, "");
			foreach (SetMaxCharactersEvent setMaxCharactersEvent in setMaxCharactersEvents)
				textStrCopy = textStrCopy.Replace(setMaxCharactersEvent.indicator, "");
			foreach (RequireInputDeviceEvent requireInputDeviceEvent in requireInputDeviceEvents)
				textStrCopy = textStrCopy.Replace(requireInputDeviceEvent.startIndicator, "");
			textStrCopy = textStrCopy.Replace(RequireInputDeviceEvent.endIndicator, "");
			foreach (CustomDialogEvent customDialogEvent in customDialogEvents)
				textStrCopy = textStrCopy.Replace(customDialogEvent.indicator, "");
			float textSize;
			text.enableAutoSizing = true;
			text.text = textStrCopy;
			yield return new WaitForEndOfFrame();
			textSize = text.fontSize;
			text.enableAutoSizing = false;
			text.fontSize = textSize;
			yield break;
		}

		[Serializable]		
		public class Event
		{
		}

		[Serializable]
		public class WaitEvent : Event
		{
			public string indicator;
			public float duration;
		}
		
		[Serializable]
		public class SetMaxCharactersEvent : Event
		{
			public string indicator;
			public int maxCharacters;
		}
		
		[Serializable]
		public class RequireInputDeviceEvent : Event
		{
			public string startIndicator;
			public const string endIndicator = "{end}";
			public InputDevice inputDevice;
			public bool mustHaveInputDevice;
		}

		[Serializable]
		public class CustomDialogEvent : Event
		{
			public string indicator;
			public UnityEvent _event;
		}
	}
}