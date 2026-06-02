using System;
using Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace SlimeJump
{
	public class Intro : SingletonUpdateWhileEnabled<Intro>
	{
		public float holdButtonToSkipDur;
		public TemporaryActiveText skipNotificationTempActiveText;
		public static string LoadSceneNameAtEnd
		{
			get
			{
				return PlayerPrefs.GetString("Load scene name");
			}
			set
			{
				PlayerPrefs.SetString ("Load scene name", value);
			}
		}
		public static bool AlreadyDone
		{
			get
			{
				return PlayerPrefsExtensions.GetBool("Into done");
			}
			set
			{
				PlayerPrefsExtensions.SetBool ("Into done", value);
			}
		}
		List<InputControl> heldButtonControls = new List<InputControl>();
		float holdButtonToSkipTimer;

		public override void Awake ()
		{
			base.Awake ();
			InputSystem.onEvent += OnInputEvent;
		}

		public override void DoUpdate ()
		{
			if (heldButtonControls.Count > 0)
			{
				holdButtonToSkipTimer -= Time.deltaTime;
				if (holdButtonToSkipTimer <= 0)
					End ();
			}
		}

		public override void OnDestroy ()
		{
			base.OnDestroy ();
			InputSystem.onEvent -= OnInputEvent;
		}

		void OnInputEvent (InputEventPtr eventPtr, InputDevice device)
		{
			if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>() || GameManager.framesSinceLevelLoaded < 1) 
				return;
			foreach (InputControl control in eventPtr.EnumerateChangedControls(device))
			{
				if (control is ButtonControl button)
				{
					bool isPressedInEvent = button.IsValueConsideredPressed(button.ReadValueFromEvent(eventPtr));
					if (button.isPressed && !isPressedInEvent)
						heldButtonControls.Remove(button);
					else if (!button.isPressed && isPressedInEvent)
					{
						skipNotificationTempActiveText.Do ();
						holdButtonToSkipTimer = holdButtonToSkipDur;
						heldButtonControls.Add(button);
					}
				}
			}
		}

		public void End ()
		{
			AlreadyDone = true;
			_SceneManager.Instance.LoadScene (LoadSceneNameAtEnd);
		}
	}
}