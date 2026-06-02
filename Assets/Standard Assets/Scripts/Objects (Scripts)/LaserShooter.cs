using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class LaserShooter : UpdateWhileEnabled
	{
		public AnimationEntry shootAnimationEntry;
		public Transform trs;
		public LayerMask whatBlocksMyShots;
		public LaserShooterLaser laserPrefab;
		public FacePlayer facePlayer;
		public static LaserShooter[] instances = new LaserShooter[0];
		float timeAtLastShot;

		public override void OnEnable ()
		{
			base.OnEnable ();
			timeAtLastShot = -Mathf.Infinity;
		}

		public override void DoUpdate ()
		{
			if (this == null)
			{
				GameManager.updatables = GameManager.updatables.Remove(this);
				return;
			}
			if (Time.time - timeAtLastShot >= laserPrefab.activateDelay)
				facePlayer.enabled = true;
			RaycastHit2D hit = Physics2D.Raycast(trs.position, trs.up, Mathf.Infinity, whatBlocksMyShots);
			if (hit.collider != null && hit.collider.GetComponentInParent<Player>() != null)
			{
				if (Time.time - timeAtLastShot >= shootAnimationEntry.length)
					shootAnimationEntry.Play (1);
				else
					shootAnimationEntry.Play ();
			}
			else
				shootAnimationEntry.animator.Play("None");
		}

		public void Shoot ()
		{
			timeAtLastShot = Time.time;
			RaycastHit2D hit = Physics2D.Raycast(trs.position, trs.up, Mathf.Infinity, whatBlocksMyShots.Remove("Player"));
			Vector2 toHitPoint = hit.point - (Vector2) trs.position;
			LaserShooterLaser laser = Instantiate(laserPrefab, trs.position, trs.rotation);
			laser.trs.localScale = new Vector3(1, toHitPoint.magnitude);
			facePlayer.enabled = false;
		}
	}
}