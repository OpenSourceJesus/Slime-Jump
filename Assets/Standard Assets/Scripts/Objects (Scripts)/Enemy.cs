using Extensions;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Enemy : UpdateWhileEnabled, IDestructable
	{
		public uint maxHp;
		public Transform trs;
		public Rigidbody2D rigid;
		public Transform eyesTrs;
		public Collider2D collider;
		public CircleCollider2D visionSensor;
		public bool isFlying;
		public float moveSpeed;
		public float visionRange;
		public float visionAngle;
		public float multiplyPatrolSpeed;
		public float patrolRange;
		public Zone2D patrolZone;
		public float patrolStopDist;
		public float minPatrolDestinationAngleDifference;
		public float lookToHurtDirectionDuration;
		public FloatRange patrolStopTimeRange;
		public FloatRange attackDistRange;
		public FloatRange chaseStopDistRange;
		public LayerMask whatBlocksVision;
		[HideInInspector]
		public Vector2 initPosition;
		public Weapon weapon;
		public UnityEvent<Enemy> onDied;
		public float Hp
		{
			get
			{
				return hp;
			}
			set
			{
			}
		}
		public uint MaxHp
		{
			get
			{
				return maxHp;
			}
			set
			{
			}
		}
		public static Enemy[] instances = new Enemy[0];
		float hp;
		Vector2 initSize;
		bool chasePlayer;
		Vector2 currentPatrolDestination;
		float patrolStopTimeRemaining;
		bool previousAtCurrentPatrolDestination;
		Vector2 toPreviousPatrolDestination;
		float lookToHurtDirectionTimer;
		Vector2 blasterLaunchVel;

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (visionSensor != null)
				visionSensor.radius = visionRange;
			// if (patrolZone != default(Zone2D))
			// 	patrolZone = patrolZone.Gen();
		}
#endif

		public override void OnEnable ()
		{
			base.OnEnable ();
			hp = maxHp;
			instances = instances.Add(this);
			initSize = trs.localScale;
			initPosition = trs.position;
			trs.position += Vector3.up * Physics2D.defaultContactOffset;
			currentPatrolDestination = trs.position;
			if (!isFlying)
				currentPatrolDestination.y -= collider.bounds.extents.y;
		}

		public override void OnDisable ()
		{
			base.OnDisable ();
			instances = instances.Remove(this);
		}

		public override void DoUpdate ()
		{
			HandleMoving ();
			if (chasePlayer)
				HandleAttacking ();
		}

		void HandleMoving ()
		{
			if (chasePlayer)
			{
				Vector2 toPlayer = Player.instance.trs.position - trs.position;
				if (chaseStopDistRange.Contains(toPlayer.magnitude))
					Move (Vector2.zero);
				else if (toPlayer.magnitude < chaseStopDistRange.min)
					Move (-toPlayer);
				else
					Move (toPlayer);
				LayerMask whatBlocksBullets = whatBlocksVision.Remove("Enemy");
				if (Physics2D.Raycast(trs.position, Player.instance.trs.position - trs.position, Mathf.Infinity, whatBlocksBullets).collider != Player.instance.collider &&
					Physics2D.Raycast(trs.position, Player.instance.collider.bounds.min - trs.position, Mathf.Infinity, whatBlocksBullets).collider != Player.instance.collider &&
					Physics2D.Raycast(trs.position, Player.instance.collider.bounds.max - trs.position, Mathf.Infinity, whatBlocksBullets).collider != Player.instance.collider &&
					Physics2D.Raycast(trs.position, new Vector3(Player.instance.collider.bounds.min.x, Player.instance.collider.bounds.max.y) - trs.position, Mathf.Infinity, whatBlocksBullets).collider != Player.instance.collider &&
					Physics2D.Raycast(trs.position, new Vector3(Player.instance.collider.bounds.max.x, Player.instance.collider.bounds.min.y) - trs.position, Mathf.Infinity, whatBlocksBullets).collider != Player.instance.collider)
				{
					chasePlayer = false;
					visionSensor.enabled = true;
				}
			}
			else
			{
				if (lookToHurtDirectionTimer > 0)
				{
					lookToHurtDirectionTimer -= Time.deltaTime;
					Move (Vector2.zero);
					return;
				}
				Vector2 toCurrentPatrolDestination;
				if (isFlying)
					toCurrentPatrolDestination = currentPatrolDestination - (Vector2) trs.position;
				else
					toCurrentPatrolDestination = currentPatrolDestination - ((Vector2) trs.position + Vector2.down * collider.bounds.extents.y);
				bool atCurrentPatrolDestination = toCurrentPatrolDestination.sqrMagnitude <= patrolStopDist * patrolStopDist;
				if (atCurrentPatrolDestination)
				{
					if (!previousAtCurrentPatrolDestination)
						patrolStopTimeRemaining = patrolStopTimeRange.Get(Random.value);
					else
					{
						patrolStopTimeRemaining -= Time.deltaTime;
						if (patrolStopTimeRemaining <= 0)
						{
							while (true)
							{
								if (isFlying)
								{
									if (patrolZone == default(Zone2D))
										currentPatrolDestination = initPosition + Random.insideUnitCircle.normalized * patrolRange;
									else
										currentPatrolDestination = patrolZone.GetRandomPoint();
									toCurrentPatrolDestination = currentPatrolDestination - (Vector2) trs.position;
								}
								else // TODO: Allow patrolling up hills and get patrolling down hills working
								{
									currentPatrolDestination.x = initPosition.x + Random.Range(-1f, 1f) * patrolRange;
									toCurrentPatrolDestination = currentPatrolDestination - ((Vector2) trs.position + Vector2.down * collider.bounds.extents.y);
								}
								if ((toPreviousPatrolDestination == Vector2.zero || Vector2.Angle(toCurrentPatrolDestination, toPreviousPatrolDestination) >= minPatrolDestinationAngleDifference) && rigid.Cast(toCurrentPatrolDestination, new RaycastHit2D[1], toCurrentPatrolDestination.magnitude) == 0)
									break;
							}
							toPreviousPatrolDestination = toCurrentPatrolDestination;
						}
					}
					Move (Vector2.zero);
				}
				else
					Move (toCurrentPatrolDestination);
				previousAtCurrentPatrolDestination = atCurrentPatrolDestination;
			}
		}

		void Move (Vector2 move)
		{
			move = Vector2.ClampMagnitude(move, 1);
			if (isFlying)
			{
				rigid.linearVelocity = move * moveSpeed + blasterLaunchVel;
				if (move != Vector2.zero)
					trs.up = move;
			}
			else
			{
				int moveSign = MathfExtensions.Sign(move.x);
				rigid.linearVelocity = rigid.linearVelocity.SetX(moveSign * moveSpeed + blasterLaunchVel.x);
				if (move.x != 0)
					trs.localScale = trs.localScale.SetX(initSize.x * moveSign);
			}
			blasterLaunchVel *= 1f - rigid.linearDamping * Time.deltaTime;
		}

		void HandleAttacking ()
		{
			Vector2 toPlayerPosition = Player.instance.trs.position - trs.position;
			if (attackDistRange.Contains(toPlayerPosition.magnitude))
				weapon.animationEntry.Play ();
			else
				weapon.animationEntry.animator.Play ("None", weapon.animationEntry.layer);
		}
		
		public void TakeDamage (float amount, Vector2 direction, Player attacker)
		{
			hp = Mathf.Clamp(hp - amount, 0, maxHp);
			if (hp == 0)
				Death (attacker);
			else if (!chasePlayer)
			{
				if (isFlying)
					trs.up = direction;
				else
					trs.localScale = trs.localScale.SetX(initSize.x * Mathf.Sign(direction.x));
				lookToHurtDirectionTimer = lookToHurtDirectionDuration;
			}
		}
		
		public void Death (Player killer)
		{
			gameObject.SetActive(false);
			onDied.Invoke(this);
		}

		public void ChasePlayer ()
		{
			chasePlayer = true;
			visionSensor.enabled = false;
			lookToHurtDirectionTimer = 0;
		}
		
		public void ShootWeapon ()
		{
			if (isFlying)
				trs.up = Player.instance.trs.position - trs.position;
			else
				trs.localScale = trs.localScale.SetX(initSize.x * Mathf.Sign(Player.instance.trs.position.x - trs.position.x));
			weapon.bulletPatternEntry.Shoot ();
		}

		void OnTriggerStay2D (Collider2D other)
		{
			if (Vector2.Angle(eyesTrs.up, Player.instance.trs.position - eyesTrs.position) <= visionAngle && (collider.bounds.Intersects(Player.instance.collider.bounds) ||
				Physics2D.Raycast(eyesTrs.position, Player.instance.trs.position - eyesTrs.position, visionRange, whatBlocksVision).collider == Player.instance.collider ||
				Physics2D.Raycast(eyesTrs.position, Player.instance.collider.bounds.min - eyesTrs.position, visionRange, whatBlocksVision).collider == Player.instance.collider ||
				Physics2D.Raycast(eyesTrs.position, Player.instance.collider.bounds.max - eyesTrs.position, visionRange, whatBlocksVision).collider == Player.instance.collider ||
				Physics2D.Raycast(eyesTrs.position, new Vector3(Player.instance.collider.bounds.min.x, Player.instance.collider.bounds.max.y) - eyesTrs.position, visionRange, whatBlocksVision).collider == Player.instance.collider ||
				Physics2D.Raycast(eyesTrs.position, new Vector3(Player.instance.collider.bounds.max.x, Player.instance.collider.bounds.min.y) - eyesTrs.position, visionRange, whatBlocksVision).collider == Player.instance.collider))
				ChasePlayer ();
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			BlasterBullet blasterBullet = coll.collider.GetComponent<BlasterBullet>();
			if (blasterBullet != null)
			{
				blasterLaunchVel += (Vector2) blasterBullet.trs.up * blasterBullet.launchSpeed;
				if (!isFlying)
					rigid.linearVelocity = rigid.linearVelocity.SetY(rigid.linearVelocity.y + blasterLaunchVel.y);
			}
		}
	}
}