using TMPro;
using SlimeJump;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class _Slider : Slider
{
	public TMP_Text displayValueText;
	public _Selectable selectable;
	public RectTransform slidingAreaRectTrs;
	public float[] snapValues = new float[0];
	[HideInInspector]
	public int indexOfCurrentSnapValue;
	string initDisplayValueTextString;
	
	public void Start ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return;
#endif
		if (displayValueText != null)
		{
			if (string.IsNullOrEmpty(initDisplayValueTextString))
				initDisplayValueTextString = displayValueText.text;
			SetDisplayValue ();
		}
		OnValueChanged ();
		if (snapValues.Length > 0)
			indexOfCurrentSnapValue = MathfExtensions.GetIndexOfClosestNumber(value, snapValues);
	}

	public void OnValueChanged ()
	{
		if (snapValues.Length > 0)
			value = MathfExtensions.GetClosestNumber(value, snapValues);
	}
	
	public void SetDisplayValue ()
	{
		if (displayValueText != null)
			displayValueText.text = initDisplayValueTextString + value;
	}

	public void OnMouseOver (AudioClip audioClip)
	{
		if (!IsPressed())
			AudioManager.instance.MakeSoundEffect (audioClip);
	}
}