using UnityEngine;

namespace SlimeJump
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(LineRenderer))]
	public class Laser : UpdateWhileEnabled
	{
		public LineRenderer line;
		public LayerMask whatBlocksMe;
		public Transform trs;
		public RaycastHit hit;
		public Color seesPlayerColor;
		public Color doesNotSeePlayerColor;
		const int MAX_LENGTH = int.MaxValue;

		public override void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (line == null)
					line = GetComponent<LineRenderer>();
				if (trs == null)
					trs = GetComponent<Transform>();
				line.SetPosition(0, trs.position);
				return;
			}
#endif
			base.OnEnable ();
		}

		public override void DoUpdate ()
		{
			RaycastHit2D hit = Physics2D.Raycast(trs.position, trs.up, MAX_LENGTH, whatBlocksMe);
			if (hit.collider != null)
			{
				if (hit.collider.gameObject == Player.instance.gameObject)
					line.SetColors(seesPlayerColor, seesPlayerColor);
				else
					line.SetColors(doesNotSeePlayerColor, doesNotSeePlayerColor);
				line.SetPosition(1, hit.point);
			}
			else
			{
				line.SetColors(doesNotSeePlayerColor, doesNotSeePlayerColor);
				line.SetPosition(1, trs.position + trs.up * MAX_LENGTH);
			}
		}
	}
}