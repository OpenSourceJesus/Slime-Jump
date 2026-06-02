using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class FallerObject : UpdateWhileEnabled
	{
		public Transform trs;
		public float jumpSpeed;
		public Collider2D sensorCollider;
		public Rigidbody2D rigid;
		[HideInInspector]
		public Vector2 initPos;
		public static FallerObject[] instances = new FallerObject[0];
		bool jumpUp = true;
		bool colliding = true;
		bool moving;

		public void Awake ()
		{
			sensorCollider.transform.SetParent(null);
			initPos = trs.position;
		}

		public override void DoUpdate ()
		{
			if (!colliding)
				rigid.linearVelocity += rigid.linearVelocity.normalized * Physics2D.gravity.magnitude * Time.deltaTime;
			else if (!moving)
			{
				ContactFilter2D contactFilter = new ContactFilter2D();
				contactFilter.useLayerMask = true;
				contactFilter.layerMask = LayerMask.GetMask("Player");
				if (sensorCollider.Overlap(contactFilter, new Collider2D[1]) > 0)
				{
					if (jumpSpeed == 0)
						rigid.linearVelocity = trs.up * Physics2D.gravity.magnitude * Time.deltaTime * jumpUp.PositiveOrNegative();
					else
						rigid.linearVelocity = trs.up * jumpSpeed * jumpUp.PositiveOrNegative();
					jumpUp = !jumpUp;
					moving = true;
					colliding = false;
				}
			}
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			if (GameManager.framesSinceLevelLoaded - Player.respawnedOnFrame > 9)
			{
				colliding = true;
				moving = false;
			}
		}

		public void Reset ()
		{
			trs.position = initPos;
			jumpUp = true;
			colliding = true;
			moving = false;
			rigid.linearVelocity = Vector2.zero;
		}
	}
}