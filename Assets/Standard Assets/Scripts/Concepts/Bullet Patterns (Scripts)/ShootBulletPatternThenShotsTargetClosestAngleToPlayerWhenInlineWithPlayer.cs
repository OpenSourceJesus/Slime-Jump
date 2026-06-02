using System.Collections;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class ShootBulletPatternThenShotsTargetClosestAngleToPlayerWhenInlineWithPlayer : BulletPattern
	{
		public BulletPattern bulletPattern;
		public float[] retargetAngles;
		[HideInInspector]
		[SerializeField]
		Vector2[] retargetDirections = new Vector2[0];
		public float lineWidth;
		public bool lastRetarget;

		public override void Init (Transform spawner)
		{
			retargetDirections = new Vector2[retargetAngles.Length];
			for (int i = 0; i < retargetAngles.Length; i ++)
				retargetDirections[i] = VectorExtensions.FromFacingAngle(retargetAngles[i]);
		}

		public override Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
		{
			Bullet[] output = bulletPattern.Shoot (spawner, bulletPrefab);
			GameManager.Instance.StartCoroutine(RetargetShotsWhenInlineWithPlayerRoutine (output));
			return output;
		}

		public override Bullet[] Shoot (Vector2 spawnPos, Vector2 direction, Bullet bulletPrefab)
		{
			Bullet[] output = bulletPattern.Shoot (spawnPos, direction, bulletPrefab);
			GameManager.Instance.StartCoroutine(RetargetShotsWhenInlineWithPlayerRoutine (output));
			return output;
		}

		public virtual IEnumerator RetargetShotsWhenInlineWithPlayerRoutine (Bullet[] bullets)
		{
			Bullet bullet;
			Vector2 retargetDirection;
			do
			{
				for (int i = 0; i < bullets.Length; i ++)
				{
					bullet = bullets[i];
					if (bullet.timesRetargeted == 0 && bullet.gameObject.activeSelf)
					{
						for (int i2 = 0; i2 < retargetDirections.Length; i2 ++)
						{
							retargetDirection = retargetDirections[i2];
							if (Physics2DExtensions.LinecastWithWidth(bullet.trs.position, (Vector2) bullet.trs.position + retargetDirection * Vector2.Distance(Player.instance.trs.position, bullet.trs.position), lineWidth, LayerMask.GetMask("Player")))
							{
								bullet.Retarget (retargetDirection, lastRetarget);
								break;
							}
						}
					}
					else
					{
						bullets = bullets.Remove(bullet);
						i --;
						if (bullets.Length == 0)
							yield break;
					}
				}
				yield return new WaitForEndOfFrame();
			} while (true);
		}
	}
}