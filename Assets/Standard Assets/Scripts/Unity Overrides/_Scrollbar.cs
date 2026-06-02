using SlimeJump;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class _Scrollbar : _Selectable, IUpdatable
{
	public Scrollbar scrollbar;
	public Transform handleTrs;
	public RectTransform contentRectTrs;
	DragUpdater dragUpdater;

	void Awake ()
	{
		dragUpdater = new DragUpdater(this);
		RectTransform slidingArea = (RectTransform) handleTrs.parent;
		handleTrs.localPosition = Vector2.up * (slidingArea.rect.height * scrollbar.value - slidingArea.rect.height / 2);
	}

	public override void OnEnable ()
	{
		base.OnEnable ();
		GameManager.updatables = GameManager.updatables.Add(this);
	}

	public override void OnDisable ()
	{
		base.OnDisable ();
		GameManager.updatables = GameManager.updatables.Remove(this);
	}

	public void StartDrag ()
	{
		GameManager.updatables = GameManager.updatables.Add(dragUpdater);
	}

	public void EndDrag ()
	{
		GameManager.updatables = GameManager.updatables.Remove(dragUpdater);
	}
	
	public void DoUpdate ()
	{
		RectTransform viewportRectTrs = (RectTransform) contentRectTrs.parent;
		Rect rect = viewportRectTrs.GetWorldRect();
		Vector2 center = rect.center;
		rect.height -= contentRectTrs.GetWorldRect().size.y;
		rect.center = center;
		float value = Rect.PointToNormalized(rect, contentRectTrs.GetWorldRect().center).y;
		RectTransform slidingArea = (RectTransform) handleTrs.parent;
		handleTrs.localPosition = Vector2.up * (slidingArea.rect.height * value - slidingArea.rect.height / 2);
	}

	class DragUpdater : IUpdatable
	{
		_Scrollbar scrollbar;

		public DragUpdater (_Scrollbar scrollbar)
		{
			this.scrollbar = scrollbar;
		}

		public void DoUpdate ()
		{
			float value = scrollbar.scrollbar.value;
			RectTransform slidingArea = (RectTransform) scrollbar.handleTrs.parent;
			if (InputManager.UsingMouse)
				value = Rect.PointToNormalized(slidingArea.GetWorldRect(), Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())).y;
			scrollbar.handleTrs.localPosition = Vector2.up * (slidingArea.rect.height * value - slidingArea.rect.height / 2);
			scrollbar.scrollbar.value = value;
		}
	}
}