using TMPro;
using SlimeJump;
using Extensions;
using UnityEngine;
using UnityEngine.Events;

public class Tooltip : _Selectable, IUpdatable
{
	public UnityEvent onShow;
	public UnityEvent onHide;
	bool previousShow;

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

	public void DoUpdate ()
	{
		bool show = UIManager.Instance.IsMousedOverSelectable(this);
		if (show && !previousShow)
			onShow.Invoke();
		else if (!show && previousShow)
			onHide.Invoke();
		previousShow = show;
	}
}