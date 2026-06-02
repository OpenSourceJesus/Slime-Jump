using System;
using Extensions;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SlimeJump
{
	public class InputManager : SingletonMonoBehaviour<InputManager>
	{
		public InputSettings settings;
		public static InputSettings Settings
		{
			get
			{
				return Instance.settings;
			}
		}
		public static bool UsingGamepad
		{
			get
			{
				return Gamepad.current != null;
			}
		}
		public static bool UsingMouse
		{
			get
			{
				return Mouse.current != null;
			}
		}
		public static bool UsingKeyboard
		{
			get
			{
				return Keyboard.current != null;
			}
		}
		public static bool UsingPhone
		{
			get
			{
#if UNITY_ANDROID || UNITY_IOS
				return true;
#else
				return false;
#endif
			}
		}
		public static bool LeftClickInput
		{
			get
			{
				return UsingMouse && Mouse.current.leftButton.isPressed;
			}
		}
		public bool _LeftClickInput
		{
			get
			{
				return LeftClickInput;
			}
		}
		public static bool RightClickInput
		{
			get
			{
				return UsingMouse && Mouse.current.rightButton.isPressed;
			}
		}
		public bool _RightClickInput
		{
			get
			{
				return RightClickInput;
			}
		}
		public static Vector2? MousePosition
		{
			get
			{
				if (UsingMouse)
					return Mouse.current.position.ReadValue();
				else
					return null;
			}
		}
		public Vector2? _MousePosition
		{
			get
			{
				return MousePosition;
			}
		}
		public static bool SubmitInput
		{
			get
			{
				if (UsingGamepad)
					return Gamepad.current.aButton.isPressed;
				else
					return Keyboard.current.enterKey.isPressed;// || Mouse.current.leftButton.isPressed;
			}
		}
		public bool _SubmitInput
		{
			get
			{
				return SubmitInput;
			}
		}
		public static Vector2 UIMovementInput
		{
			get
			{
				if (UsingGamepad)
					return Vector2.ClampMagnitude(Gamepad.current.leftStick.ReadValue(), 1);
				else
				{
					int x = 0;
					if (Keyboard.current.dKey.isPressed)
						x ++;
					if (Keyboard.current.aKey.isPressed)
						x --;
					int y = 0;
					if (Keyboard.current.wKey.isPressed)
						y ++;
					if (Keyboard.current.sKey.isPressed)
						y --;
					return Vector2.ClampMagnitude(new Vector2(x, y), 1);
				}
			}
		}
		public Vector2 _UIMovementInput
		{
			get
			{
				return UIMovementInput;
			}
		}
		public static float MoveInput
		{
			get
			{
				float output = 0;
				if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
					output --;
				if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
					output ++;
				if (output == 0 && UsingGamepad)
					return Mathf.Clamp(Gamepad.current.leftStick.ReadValue().x, -1, 1);
				return output;
			}
		}
		public float _MoveInput
		{
			get
			{
				return MoveInput;
			}
		}
		public static bool JumpInput
		{
			get
			{
				return (UsingKeyboard && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) || (UsingGamepad && (Gamepad.current.leftStickButton.isPressed || Gamepad.current.leftShoulder.isPressed || Gamepad.current.aButton.isPressed || Gamepad.current.bButton.isPressed));
			}
		}
		public bool _JumpInput
		{
			get
			{
				return JumpInput;
			}
		}
		public static bool LassoInput
		{
			get
			{
				return (UsingMouse && Mouse.current.rightButton.isPressed) || (UsingGamepad && (Gamepad.current.leftTrigger.isPressed || Gamepad.current.rightStickButton.isPressed || Gamepad.current.xButton.isPressed || Gamepad.current.yButton.isPressed)) || (UsingPhone && Touchscreen.current.primaryTouch.isInProgress);
			}
		}
		public bool _LassoInput
		{
			get
			{
				return LassoInput;
			}
		}
		public static int ChangeLassoLengthInput
		{
			get
			{
				int output = 0;
				if ((UsingKeyboard && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) || (UsingGamepad && Gamepad.current.leftShoulder.isPressed))
					output --;
				else if ((UsingKeyboard && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) || (UsingGamepad && Gamepad.current.rightShoulder.isPressed))
					output ++;
				return output;
			}
		}
		public int _ChangeLassoLengthInput
		{
			get
			{
				return ChangeLassoLengthInput;
			}
		}
		public static bool AttackInput
		{
			get
			{
				return (UsingMouse && Mouse.current.leftButton.isPressed) || (UsingGamepad && Gamepad.current.rightTrigger.isPressed);
			}
		}
		public bool _AttackInput
		{
			get
			{
				return AttackInput;
			}
		}

		public static float GetAxis (InputControl<float> positiveButton, InputControl<float> negativeButton)
		{
			return positiveButton.ReadValue() - negativeButton.ReadValue();
		}

		public static Vector2 GetAxis2D (InputControl<float> positiveXButton, InputControl<float> negativeXButton, InputControl<float> positiveYButton, InputControl<float> negativeYButton)
		{
			Vector2 output = new Vector2();
			output.x = positiveXButton.ReadValue() - negativeXButton.ReadValue();
			output.y = positiveYButton.ReadValue() - negativeYButton.ReadValue();
			output = Vector2.ClampMagnitude(output, 1);
			return output;
		}
		
		public enum HotkeyState
		{
			Down,
			Held,
			Up
		}
		
		public enum InputDevice
		{
			KeyboardAndMouse,
			Phone,
			Gamepad
		}
	}
}