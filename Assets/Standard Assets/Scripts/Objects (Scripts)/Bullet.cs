using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Bullet : Spawnable, IDestructable
	{
		public uint maxHp;
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
		public Rigidbody2D rigid;
		public Collider2D collider;
		public float moveSpeed;
		public float lifetime;
		public float range;
		public SpriteRenderer spriteRenderer;
		public Color canRetargetColor;
		public Color cantRetargetColor;
		public bool hitsDestructables;
		public bool hitsNonDestructables;
		[HideInInspector]
		public int timesRetargeted;
		[HideInInspector]
		public bool hasFinishedRetargeting;
		public float damage = 1;
		[HideInInspector]
		public Collider2D[] dontCollideWith = new Collider2D[0];
		public AutoDespawnMode autoDespawnMode;
		public ObjectPool.RangedDespawn rangedDespawn;
		public ObjectPool.DelayedDespawn delayedDespawn;
		public byte hitsTillDespawn;
		public AudioClip hitWallSound;
		public float hitWallSoundVolume;
		public FloatRange hitWallSoundPitchRange;
		public static List<Bullet> instances = new List<Bullet>();
		byte hitsTillDespawnRemaining;
		float hp;

		public virtual void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (rigid == null)
					rigid = GetComponent<Rigidbody2D>();
				if (collider == null)
					collider = GetComponent<Collider2D>();
				return;
			}
#endif
			hitsTillDespawnRemaining = hitsTillDespawn;
			hp = maxHp;
			ObjectPool.instance = ObjectPool.Instance;
			if (autoDespawnMode == AutoDespawnMode.RangedAutoDespawn)
				rangedDespawn = ObjectPool.instance.RangeDespawn(prefabIndex, gameObject, trs, range);
			else if (autoDespawnMode == AutoDespawnMode.DelayedAutoDespawn)
				delayedDespawn = ObjectPool.instance.DelayDespawn(prefabIndex, gameObject, trs, lifetime);
			rigid.linearVelocity = trs.up * moveSpeed;
			instances.Add(this);
		}

		public virtual void OnTriggerEnter2D (Collider2D other)
		{
			if (other.isTrigger)
				return;
			IDestructable destructable = other.GetComponentInParent<IDestructable>();
			if (destructable != null)
			{
				if (other == Player.instance.collider && Player.instance.invulnerable)
					return;
				destructable.TakeDamage (damage, trs.position - other.transform.position, null);
				if (hitsDestructables)
				{
					hitsTillDespawnRemaining --;
					if (hitsTillDespawnRemaining == 0)
					{
						SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(hitWallSound, trs.position, hitWallSoundVolume);
						soundEffect.audioSource.pitch = hitWallSoundPitchRange.Get(Random.value);
						Despawn ();
					}
				}
			}
			else if (hitsNonDestructables)
			{
				hitsTillDespawnRemaining --;
				if (hitsTillDespawnRemaining == 0)
				{
					SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(hitWallSound, trs.position, hitWallSoundVolume);
					soundEffect.audioSource.pitch = hitWallSoundPitchRange.Get(Random.value);
					Despawn ();
				}
			}
		}

		public virtual void OnCollisionEnter2D (Collision2D coll)
		{
			OnTriggerEnter2D (coll.collider);
		}

		public virtual void Despawn ()
		{
			if (autoDespawnMode == AutoDespawnMode.RangedAutoDespawn)
				ObjectPool.instance.CancelRangedDespawn (rangedDespawn);
			else if (autoDespawnMode == AutoDespawnMode.DelayedAutoDespawn)
				ObjectPool.instance.CancelDelayedDespawn (delayedDespawn);
			ObjectPool.instance.Despawn (prefabIndex, gameObject, trs);
		}

		public void TakeDamage (float amount, Vector2 direction, Player attacker)
		{
			hp -= amount;
			if (hp <= 0)
				Death (attacker);
		}

		public virtual void Death (Player killer)
		{
			Despawn ();
		}

		public virtual void Retarget (Vector2 direction, bool lastRetarget = false)
		{
			if (hasFinishedRetargeting)
				return;
			trs.up = direction;
			rigid.linearVelocity = trs.up * moveSpeed;
			timesRetargeted ++;
			if (lastRetarget)
			{
				hasFinishedRetargeting = true;
				spriteRenderer.color = cantRetargetColor;
				spriteRenderer.sortingOrder --;
			}
		}

		public virtual void OnDisable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			timesRetargeted = 0;
			if (hasFinishedRetargeting)
			{
				hasFinishedRetargeting = false;
				spriteRenderer.color = canRetargetColor;
				spriteRenderer.sortingOrder ++;
			}
			for (int i = 0; i < dontCollideWith.Length; i ++)
			{
				Collider2D _dontCollideWith = dontCollideWith[i];
				if (_dontCollideWith != null)
					Physics2D.IgnoreCollision(collider, _dontCollideWith, false);
			}
			// if (trs.parent != ObjectPool.instance.trs)
			// 	Despawn ();
			instances.Remove(this);
		}

		public enum AutoDespawnMode
		{
			DontAutoDespawn,
			RangedAutoDespawn,
			DelayedAutoDespawn
		}
	}
}