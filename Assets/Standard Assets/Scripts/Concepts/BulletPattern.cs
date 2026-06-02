using SlimeJump;
using UnityEngine;
using System.Collections;

public class BulletPattern : ScriptableObject, IConfigurable
{
	public virtual string Name
	{
		get
		{
			return name;
		}
	}
	public virtual string Category
	{
		get
		{
			return "Bullet Patterns";
		}
	}

	public virtual void Init (Transform spawner)
	{
	}

	public virtual Vector2 GetShootDirection (Transform spawner)
	{
		return spawner.up;
	}
	
	public virtual Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
	{
		// spawner.up = GetShootDirection(spawner);
		return new Bullet[] { ObjectPool.instance.SpawnComponent<Bullet>(bulletPrefab.prefabIndex, spawner.position, Quaternion.LookRotation(Vector3.forward, GetShootDirection(spawner))) };
	}
	
	public virtual Bullet[] Shoot (Vector2 spawnPos, Vector2 direction, Bullet bulletPrefab)
	{
		return new Bullet[] { ObjectPool.instance.SpawnComponent<Bullet>(bulletPrefab.prefabIndex, spawnPos, Quaternion.LookRotation(Vector3.forward, direction)) };
	}

	public virtual Bullet Retarget (Bullet bullet, bool lastRetarget = false)
	{
		bullet.Retarget (GetRetargetDirection(bullet), lastRetarget);
		return bullet;
	}
	
	public virtual Bullet Retarget (Bullet bullet, Vector2 direction, bool lastRetarget = false)
	{
		bullet.Retarget (direction, lastRetarget);
		return bullet;
	}
	
	public virtual Vector2 GetRetargetDirection (Bullet bullet)
	{
		return bullet.trs.up;
	}

	public virtual Bullet[] Split (Bullet bullet, Bullet splitBulletPrefab)
	{
		return Shoot (bullet.trs.position, GetSplitDirection(bullet), splitBulletPrefab);
	}

	public virtual Bullet[] Split (Bullet bullet, Vector2 direction, Bullet splitBulletPrefab)
	{
		return Shoot (bullet.trs.position, direction, splitBulletPrefab);
	}
	
	public virtual Vector2 GetSplitDirection (Bullet bullet)
	{
		return bullet.trs.up;
	}
}
