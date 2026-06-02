using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class ShooterTrap : UpdateWhileEnabled
	{
		public BulletPatternEntry bulletPatternEntry;
		public AnimationEntry shootAnimationEntry;
		public GameObject[] ignoreShotCollisionWithGos = new GameObject[0];
		public Collider2D[] ignoreShotCollisionWith = new Collider2D[0];
		public Transform trs;
		public float prewarmTime;
		public LayerMask whatBlocksMyShots;
		// public ObjectInWorld worldOb;
		public AudioClip shootSuond;
		public float shootSoundVolume;
		public FloatRange shootSoundPitchRange;
#if UNITY_EDITOR
		public bool setIgnoreShotCollisionsWith;
#endif
		public static ShooterTrap[] instances = new ShooterTrap[0];
		protected float timeAtLastShot;
		float timeAtLastShootSound;

		public virtual void Awake ()
		{
			if (!enabled)
				shootAnimationEntry.animator.Play (0);
			timeAtLastShot = -Mathf.Infinity;
			timeAtLastShootSound = -Mathf.Infinity;
			List<Collider2D> _ignoreShotCollisionWith = new List<Collider2D>();
			for (int i = 0; i < ignoreShotCollisionWithGos.Length; i ++)
			{
				GameObject ignoreShotCollisionWithGo = ignoreShotCollisionWithGos[i];
				_ignoreShotCollisionWith.AddRange(ignoreShotCollisionWithGo.GetComponents<Collider2D>());
			}
			_ignoreShotCollisionWith.Add(Player.Instance.climbableSensor);
			_ignoreShotCollisionWith.Add(Player.Instance.wallSensor);
			ignoreShotCollisionWith = _ignoreShotCollisionWith.ToArray();
			if (WorldMap.isOpen)
				return;
			ObjectPool.instance = ObjectPool.Instance;
			if (prewarmTime == 0)
				return;
			List<Bullet> bullets = new List<Bullet>();
			float animationLength = shootAnimationEntry.length;
			for (float time = 0; time <= prewarmTime; time += animationLength)
			{
				for (int i = 0; i < bullets.Count; i ++)
				{
					Bullet bullet = bullets[i];
					bullet.rigid.position += (Vector2) bullet.trs.up * bullet.moveSpeed * animationLength;
				}
				bullets.AddRange(bulletPatternEntry.Shoot(ignoreShotCollisionWith));
			}
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (setIgnoreShotCollisionsWith)
			{
				setIgnoreShotCollisionsWith = false;
				Bullet bullet = bulletPatternEntry.bulletPrefab;
				Rect bulletRect = bullet.collider.GetRect();
				bulletRect.center = trs.position;
				if (trs.eulerAngles.z == 90 || trs.eulerAngles.z == 270)
					bulletRect = bulletRect.Rotate90();
				Collider2D[] colliders = FindObjectsOfType<Collider2D>();
				List<GameObject> _ignoreShotCollisionWithGos = new List<GameObject>();
				for (int i = 0; i < colliders.Length; i ++)
				{
					Collider2D collider = colliders[i];
					if (!Physics2D.GetIgnoreLayerCollision(bullet.gameObject.layer, collider.gameObject.layer) && collider.GetRect().Overlaps(bulletRect))
						_ignoreShotCollisionWithGos.Add(collider.gameObject);
				}
				ignoreShotCollisionWithGos = _ignoreShotCollisionWithGos.ToArray();
			}
		}
#endif

		public override void DoUpdate ()
		{
			RaycastHit2D hit = Physics2D.Raycast(trs.position, trs.up, Mathf.Infinity, whatBlocksMyShots);
			if (hit.collider != null && hit.collider.GetComponentInParent<Player>() != null)
			{
				if (Time.time - timeAtLastShot >= shootAnimationEntry.length)
				{
					MakeShootSound ();
					shootAnimationEntry.Play (1);
				}
				else
					shootAnimationEntry.Play ();
			}
			else
				shootAnimationEntry.animator.Play("None");
		}

		public void Shoot ()
		{
			timeAtLastShot = Time.time;
			bulletPatternEntry.Shoot(ignoreShotCollisionWith);
			// Bullet[] bullets = bulletPatternEntry.Shoot(ignoreShotCollisionWith);
			// for (int i = 0; i < bullets.Length; i ++)
			// {
			// 	Bullet bullet = bullets[i];
			// 	bullet.trs.SetParent(worldOb.pieceIAmIn.trs);
			// }
		}

		public void MakeShootSound ()
		{
			if (Time.time - timeAtLastShootSound < shootAnimationEntry.length)
				return;
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(shootSuond, trs.position, shootSoundVolume);
			soundEffect.audioSource.pitch = shootSoundPitchRange.Get(Random.value);
			timeAtLastShootSound = Time.time;
		}
	}
}