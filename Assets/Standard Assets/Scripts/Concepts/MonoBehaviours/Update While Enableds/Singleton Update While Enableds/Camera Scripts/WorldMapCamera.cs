using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeJump
{
	public class WorldMapCamera : CameraScript
	{
		public new static WorldMapCamera instance;
		public new static WorldMapCamera Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<WorldMapCamera>(true);
				return instance;
			}
		}
		public float sizeMultiplier;
		public float zoomRate;
		public float multitplyZoomRateWithGamepad;
		public FloatRange sizeMultiplierRange;
		[SerializeField]
		Vector2 initViewSize;
		// Rect viewRectWhenStartedZoom;
		Rect previousViewRect;
		// Vector2 mousePositionWhenStartedZoom;
		Vector2 previousMousePosition;

		public override void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				initViewSize = viewSize;
				return;
			}
#endif
			base.Awake ();
		}

		public void DoUpdate ()
		{
			HandlePosition ();
			HandleViewSize ();
			previousMousePosition = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		}

		public override void HandlePosition ()
		{
			if (InputManager.UsingMouse && Mouse.current.scroll.y.ReadValue() != 0)
				// trs.position = (Vector3) (previousViewRect.min + viewRect.size.Multiply(InputManager.GetWorldMousePosition() - viewRectWhenStartedZoom.center)).SetZ(trs.position.z);
				trs.position += (Vector3) (viewRect.center - (Vector2) camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (previousViewRect.center - previousMousePosition));
			// else
			// {
			// 	viewRectWhenStartedZoom = viewRect;
			// 	mousePositionWhenStartedZoom = camera.ScreenToWorldPoint(Input.mousePosition);
			// }
		}

		public override void HandleViewSize ()
		{
			previousViewRect = viewRect;
			if (InputManager.UsingGamepad)
				sizeMultiplier = Mathf.Clamp(sizeMultiplier + Gamepad.current.rightStick.ReadValue().y * zoomRate * multitplyZoomRateWithGamepad * Time.unscaledDeltaTime, sizeMultiplierRange.min, sizeMultiplierRange.max);
			if (InputManager.UsingMouse)
				sizeMultiplier = Mathf.Clamp(sizeMultiplier + Mouse.current.scroll.y.ReadValue() * zoomRate * Time.unscaledDeltaTime, sizeMultiplierRange.min, sizeMultiplierRange.max);
			viewSize = initViewSize * sizeMultiplier;
			base.HandleViewSize ();
		}
	}
}