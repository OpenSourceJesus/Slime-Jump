using SlimeJump;
using UnityEngine;

public class Animatable : UpdateWhileEnabled
{
	public Transform trs;
	public AnimationCurve localXPositionCurve;
	public AnimationCurve localYPositionCurve;
	public AnimationCurve localZPositionCurve;
	Vector2 initLocalPosition;
	float time;

	public override void OnEnable ()
	{
		base.OnEnable ();
		initLocalPosition = trs.localPosition;
		time = 0;
	}

	public override void OnDisable ()
	{
		base.OnDisable ();
		trs.localPosition = initLocalPosition;
	}

	public override void DoUpdate ()
	{
		trs.localPosition = new Vector3(localXPositionCurve.Evaluate(time), localYPositionCurve.Evaluate(time), localZPositionCurve.Evaluate(time));
		time += Time.deltaTime;
	}
}