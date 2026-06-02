using UnityEngine;

namespace SlimeJump
{
	public class EdgeTraveler : UpdateWhileEnabled
	{
		public Transform trs;
        public float moveSpeed;
		Transform[] corners = new Transform[0];
		Transform currentCorner;
		int currentCornerIdx;
		bool isBacktracking;

		public override void DoUpdate ()
		{
            Vector2 newPosition = Vector3.Lerp(trs.position, currentCorner.position, moveSpeed * Time.deltaTime * (1f / Vector2.Distance(trs.position, currentCorner.position)));
			if (!float.IsNaN(newPosition.x))
			{
				trs.position = newPosition;
				if (trs.position == currentCorner.position)
				{
					if (!isBacktracking)
					{
						currentCornerIdx ++;
						if (currentCornerIdx == corners.Length)
							currentCornerIdx = 0;
					}
					else
					{
						currentCornerIdx --;
						if (currentCornerIdx == -1)
							currentCornerIdx = corners.Length - 1;
					}
					currentCorner = corners[currentCornerIdx];
				}
			}
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			Platform platform = other.GetComponent<Platform>();
			corners = platform.corners;
			float closestDistSqrToCornerIAmFacing = Mathf.Infinity;
			for (int i = 0; i < corners.Length; i ++)
			{
				Transform corner = corners[i];
				Vector2 toCorner = corner.position - trs.position;
				float cornerDistSqr = toCorner.sqrMagnitude;
				if (Vector2.Angle(toCorner, trs.up) < 1 && closestDistSqrToCornerIAmFacing > cornerDistSqr)
				{
					closestDistSqrToCornerIAmFacing = cornerDistSqr;
					currentCornerIdx = i;
				}
			}
			currentCorner = corners[currentCornerIdx];
			isBacktracking = trs.up.x > 0;
			enabled = true;
		}
	}
}