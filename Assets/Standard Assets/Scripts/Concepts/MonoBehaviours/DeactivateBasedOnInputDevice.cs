using SlimeJump;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeactivateBasedOnInputDevice : MonoBehaviour
{
	public bool disableIfUsing;
	public InputManager.InputDevice inputDevice;
	
	void Start ()
	{
		InputSystem.onDeviceChange += Do;
		Do ();
	}

	void Do (InputDevice device = null, InputDeviceChange change = default(InputDeviceChange))
	{
		if (inputDevice == InputManager.InputDevice.KeyboardAndMouse)
			gameObject.SetActive((InputManager.UsingKeyboard && InputManager.UsingMouse) != disableIfUsing);
		else if (inputDevice == InputManager.InputDevice.Gamepad)
			gameObject.SetActive(InputManager.UsingGamepad != disableIfUsing);
		else if (inputDevice == InputManager.InputDevice.Phone)
			gameObject.SetActive(InputManager.UsingPhone != disableIfUsing);
	}
	
	void OnDestroy ()
	{
		InputSystem.onDeviceChange -= Do;
	}
}