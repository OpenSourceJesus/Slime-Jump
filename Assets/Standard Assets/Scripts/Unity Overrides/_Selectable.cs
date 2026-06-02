using Extensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[ExecuteInEditMode]
public class _Selectable : MonoBehaviour
{
	public RectTransform rectTrs;
	public Selectable selectable;
	public Canvas canvas;
	public RectTransform canvasRectTrs;
	public Image image;
	public float priority;
	public _Selectable[] canNavigateTo = new _Selectable[0];
	public UnityEvent onDeslect;
	public UnityEvent onSelect;
#if UNITY_EDITOR
	public bool updateCanvas = true;
#endif
	public static _Selectable[] instances = new _Selectable[0];

	public virtual void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return;
#endif
		instances = instances.Add(this);
	}

	public virtual void OnDisable ()
	{
		instances = instances.Remove(this);
	}
	
#if UNITY_EDITOR
	void OnValidate ()
	{
		if (rectTrs == null)
			rectTrs = GetComponent<RectTransform>();
		if (selectable == null)
			selectable = GetComponent<Selectable>();
		if (image == null)
			image = GetComponent<Image>();
		if (updateCanvas)
		{
			updateCanvas = false;
			UpdateCanvas ();
		}
	}
#endif

	void UpdateCanvas ()
	{
		canvas = GetComponent<Canvas>();
		canvasRectTrs = GetComponent<RectTransform>();
		while (canvas == null)
		{
			canvasRectTrs = canvasRectTrs.parent.GetComponent<RectTransform>();
			canvas = canvasRectTrs.GetComponent<Canvas>();
		}
	}
}
