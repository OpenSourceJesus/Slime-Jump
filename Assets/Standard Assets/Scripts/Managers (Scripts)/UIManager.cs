using SlimeJump;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIManager : SingletonUpdateWhileEnabled<UIManager>
{
	public _Selectable currentSelected;
	public ComplexTimer multiplyBrightness;
	public float angleEffectiveness;
	public float distanceEffectiveness;
	public Timer repeatTimer;
	public bool useAngleAndDistanceEffectiveness;
	public EventSystem eventSystem;
	Vector2 inputVector;
	Vector2 previousInputVector;
	bool inControlMode;
	bool controllingWithJoystick;
	bool leftClickInput;
	bool previousLeftClickInput;
	InputField currentInputField;
	bool previousSubmitInput;
	bool isSubmitting;
	float initRepeatInterval;
	float scrollbarValueInterval = 0.01f;

	public override void Awake ()
	{
		base.Awake ();
		initRepeatInterval = repeatTimer.duration;
		repeatTimer.onFinished += () => { HandleChangeSelected (true); ControlSelected (); };
	}

	void OnDestroy ()
	{
		repeatTimer.onFinished -= () => { HandleChangeSelected (true); ControlSelected (); };
	}

	public override void DoUpdate ()
	{
		if (!InputManager.UsingGamepad)
		{
			eventSystem.enabled = true;
			ColorSelected (currentSelected, 1);
			previousLeftClickInput = leftClickInput;
			return;
		}
		eventSystem.enabled = false;
		leftClickInput = InputManager.LeftClickInput || (InputManager.UsingPhone && Touchscreen.current.touches.Count > 0);
		if (currentSelected != null)
		{
			if (!CanSelectSelectable(currentSelected))
			{
				ColorSelected (currentSelected, 1);
				HandleChangeSelected (false);
			}
			ColorSelected (currentSelected, multiplyBrightness.GetValue());
			HandleMouseInput ();
			HandleMovementInput ();
			HandleSubmitSelected ();
		}
		else
			HandleChangeSelected (false);
		previousLeftClickInput = leftClickInput;
	}

	bool CanSelectSelectable (_Selectable selectable)
	{
		return _Selectable.instances.Contains(selectable) && selectable.selectable.IsInteractable() && selectable.canvas.enabled;
	}

	public bool IsMousedOverSelectable (_Selectable selectable)
	{
		return IsMousedOverRectTransform(selectable.rectTrs, selectable.canvas, selectable.canvasRectTrs);
	}

	bool IsMousedOverRectTransform (RectTransform rectTrs, Canvas canvas, RectTransform canvasRectTrs)
	{
		if (!InputManager.UsingMouse)
			return false;
		Vector2 mousePosition = (Vector2) InputManager.MousePosition;
		if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
			return rectTrs.GetRectInCanvasNormalized(canvasRectTrs).Contains(canvasRectTrs.GetWorldRect().ToNormalizedPosition(mousePosition));
		else
			return rectTrs.GetRectInCanvasNormalized(canvasRectTrs).Contains(canvasRectTrs.GetWorldRect().ToNormalizedPosition(canvas.worldCamera.ScreenToWorldPoint(mousePosition)));
	}

	void HandleMouseInput ()
	{
		if (InputManager.UsingGamepad)
			return;
		if (!leftClickInput && previousLeftClickInput && !controllingWithJoystick)
			inControlMode = false;
		foreach (_Selectable selectable in _Selectable.instances)
		{
			if (currentSelected != selectable && IsMousedOverSelectable(selectable) && CanSelectSelectable(selectable))
			{
				ChangeSelected (selectable);
				return;
			}
		}
		if (leftClickInput)
		{
			if (currentInputField != null)
				currentInputField.readOnly = true;
			_Slider slider = currentSelected.GetComponent<_Slider>();
			if (slider != null)
			{
				Vector2 mousePosition;
				if (InputManager.LeftClickInput)
					mousePosition = (Vector2) InputManager.MousePosition;
				else
					mousePosition = Touchscreen.current.primaryTouch.position.ReadValue();
				Vector2 closestPointToMouseCanvasNormalized = new Vector2();
				if (slider.selectable.canvas.renderMode == RenderMode.ScreenSpaceOverlay || (slider.selectable.canvas.renderMode == RenderMode.ScreenSpaceCamera && slider.selectable.canvas.worldCamera == null))
					closestPointToMouseCanvasNormalized = slider.slidingAreaRectTrs.GetRectInCanvasNormalized(slider.selectable.canvasRectTrs).ClosestPoint(slider.selectable.canvasRectTrs.GetWorldRect().ToNormalizedPosition(mousePosition));
				else
					closestPointToMouseCanvasNormalized = slider.slidingAreaRectTrs.GetRectInCanvasNormalized(slider.selectable.canvasRectTrs).ClosestPoint(slider.selectable.canvasRectTrs.GetWorldRect().ToNormalizedPosition(slider.selectable.canvas.worldCamera.ScreenToWorldPoint(mousePosition)));
				float normalizedValue = slider.slidingAreaRectTrs.GetRectInCanvasNormalized(slider.selectable.canvasRectTrs).ToNormalizedPosition(closestPointToMouseCanvasNormalized).x;
				slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, normalizedValue);
				if (slider.snapValues.Length > 0)
					slider.value = MathfExtensions.GetClosestNumber(slider.value, slider.snapValues);
			}
			else
			{
				InputField inputField = currentSelected.GetComponent<InputField>();
				if (inputField != null)
				{
					currentInputField = inputField;
					currentInputField.readOnly = false;
				}
			}
		}
	}

	void HandleMovementInput ()
	{
		inputVector = InputManager.UIMovementInput;
		if (inputVector.magnitude > InputManager.Settings.defaultDeadzoneMin)
		{
			if (previousInputVector.magnitude <= InputManager.Settings.defaultDeadzoneMin)
			{
				HandleChangeSelected (true);
				ControlSelected ();
				repeatTimer.Reset ();
				repeatTimer.Start ();
			}
		}
		else
			repeatTimer.Stop ();
		previousInputVector = inputVector;
	}

	void HandleChangeSelected (bool useInputVector = true)
	{
		if (inControlMode || (currentInputField != null && !currentInputField.readOnly))
			return;
		_Selectable nextSelected = null;
		if (useInputVector)
		{
			List<_Selectable> otherSelectables = new List<_Selectable>(currentSelected.canNavigateTo);
			if (useAngleAndDistanceEffectiveness)
				otherSelectables = new List<_Selectable>(_Selectable.instances);
			otherSelectables.Remove(currentSelected);
			if (otherSelectables.Count == 0)
				return;
			float maxSelectableAttractiveness = -Mathf.Infinity;
			for (int i = 0; i < otherSelectables.Count; i ++)
			{
				_Selectable selectable = otherSelectables[i];
				float selectableAttractiveness = GetAttractivenessOfSelectable(selectable, useInputVector);
				if (selectableAttractiveness > maxSelectableAttractiveness)
				{
					maxSelectableAttractiveness = selectableAttractiveness;
					nextSelected = selectable;
				}
			}
		}
		else
		{
			float maxSelectableAttractiveness = -Mathf.Infinity;
			for (int i = 0; i < _Selectable.instances.Length; i ++)
			{
				_Selectable selectable = _Selectable.instances[i];
				float selectableAttractiveness = GetAttractivenessOfSelectable(selectable, useInputVector);
				if (selectableAttractiveness > maxSelectableAttractiveness)
				{
					maxSelectableAttractiveness = selectableAttractiveness;
					nextSelected = selectable;
				}
			}
		}
		ChangeSelected (nextSelected);
	}

	public void ChangeSelected (_Selectable selectable)
	{
		if (inControlMode)
			return;
		if (currentSelected != null)
		{
			ColorSelected (currentSelected, 1);
			UIButton uiButton = currentSelected.GetComponent<UIButton>();
			if (uiButton != null)
				uiButton.EndPress ();
			if (currentSelected.onDeslect != null)
				currentSelected.onDeslect.Invoke();
		}
		if (selectable == null)
			return;
		currentSelected = selectable;
		currentSelected.selectable.Select();
		multiplyBrightness.JumpToStart ();
		isSubmitting = false;
		_Slider slider = selectable.GetComponent<_Slider>();
		_Scrollbar scrollbar = selectable as _Scrollbar;
		if ((slider == null || slider.snapValues.Length > 0) && scrollbar == null)
			repeatTimer.duration = initRepeatInterval;
		ScrollRect scrollRect = selectable.rectTrs.GetComponentInParent<ScrollRect>();
		if (scrollRect != null && selectable != scrollRect.verticalScrollbar.GetComponentInParent<_Selectable>())
		{
			Rect viewportRect = scrollRect.viewport.GetWorldRect();
			Rect rect = viewportRect;
			Vector2 center = rect.center;
			rect.height -= scrollRect.content.GetWorldRect().size.y;
			rect.center = center;
			while (selectable.rectTrs.GetWorldRect().yMin < scrollRect.viewport.GetWorldRect().yMin)
			{
				scrollRect.verticalScrollbar.value -= scrollbarValueInterval;
				Canvas.ForceUpdateCanvases();
			}
			while (selectable.rectTrs.GetWorldRect().yMax > scrollRect.viewport.GetWorldRect().yMax)
			{
				scrollRect.verticalScrollbar.value += scrollbarValueInterval;
				Canvas.ForceUpdateCanvases();
			}
		}
		if (currentSelected.onSelect != null)
			currentSelected.onSelect.Invoke();
	}

	void HandleSubmitSelected ()
	{
		bool submitInput = InputManager.SubmitInput;
		if (CanSelectSelectable(currentSelected))
		{
			if ((submitInput && !previousSubmitInput) || (IsMousedOverSelectable(currentSelected) && leftClickInput && !previousLeftClickInput))
			{
				UIButton uiButton = currentSelected.GetComponent<UIButton>();
				if (uiButton != null)
					uiButton.StartPress ();
				isSubmitting = true;
			}
			else if (isSubmitting && ((!submitInput && previousSubmitInput) || (IsMousedOverSelectable(currentSelected) && !leftClickInput && previousLeftClickInput)))
			{
				_Slider slider = currentSelected.GetComponent<_Slider>();
				if (slider != null)
				{
					controllingWithJoystick = previousSubmitInput;
					inControlMode = !inControlMode;
				}
				else
				{
					_Scrollbar scrollbar = currentSelected as _Scrollbar;
					if (scrollbar != null)
					{
						controllingWithJoystick = previousSubmitInput;
						inControlMode = !inControlMode;
					}
					else
					{
						Button button = currentSelected.GetComponent<Button>();
						if (button != null)
						{
							UIButton uiButton = button as UIButton;
							if (uiButton != null)
								uiButton.EndPress ();
							button.onClick.Invoke();
						}
						else
						{
							Toggle toggle = currentSelected.GetComponent<Toggle>();
							if (toggle != null)
								toggle.isOn = !toggle.isOn;
						}
					}
				}
			}
		}
		previousSubmitInput = submitInput;
	}

	void ControlSelected ()
	{
		if (!inControlMode)
			return;
		_Slider slider = currentSelected.GetComponent<_Slider>();
		if (slider != null)
		{
			if (slider.snapValues.Length > 0)
			{
				slider.indexOfCurrentSnapValue = Mathf.Clamp(slider.indexOfCurrentSnapValue + MathfExtensions.Sign(inputVector.x), 0, slider.snapValues.Length - 1);
				slider.value = slider.snapValues[slider.indexOfCurrentSnapValue];
			}
			else
			{
				repeatTimer.duration = 0;
				slider.normalizedValue += inputVector.x * Time.unscaledDeltaTime;
			}
		}
		else
		{
			_Scrollbar scrollbar = currentSelected as _Scrollbar;
			if (scrollbar != null)
			{
				repeatTimer.duration = 0;
				scrollbar.scrollbar.value += inputVector.y * Time.unscaledDeltaTime;
			}
		}
	}

	float GetAttractivenessOfSelectable (_Selectable selectable, bool useInputVector = true)
	{
		if (!CanSelectSelectable(selectable))
			return -Mathf.Infinity;
		float attractiveness = selectable.priority;
		if (useInputVector)
		{
			Vector2 vectorToSelectable = GetVectorToSelectable(selectable);
			float angleAttractiveness = 180f - Vector2.Angle(inputVector, vectorToSelectable);
			float distanceAttractiveness = 0;
			if (useAngleAndDistanceEffectiveness)
			{
				angleAttractiveness *= angleEffectiveness;
				distanceAttractiveness = vectorToSelectable.magnitude * distanceEffectiveness;
			}
			attractiveness += angleAttractiveness - distanceAttractiveness;
		}
		return attractiveness;
	}

	Vector2 GetVectorToSelectable (_Selectable selectable)
	{
		return selectable.rectTrs.GetCenterInCanvasNormalized(selectable.canvasRectTrs) - currentSelected.rectTrs.GetCenterInCanvasNormalized(currentSelected.canvasRectTrs);
	}

	void ColorSelected (_Selectable selectable, float multiplyBrightness)
	{
		if (selectable == null || selectable.image.color.a == 0)
			return;
		if (isSubmitting)
			selectable.image.color = selectable.selectable.colors.pressedColor.Multiply(multiplyBrightness);
		else
			selectable.image.color = selectable.selectable.colors.normalColor.Multiply(multiplyBrightness);
	}
}