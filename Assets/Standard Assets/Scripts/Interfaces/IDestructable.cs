using UnityEngine;

namespace SlimeJump
{
	public interface IDestructable
	{
		float Hp { get; set; }
		uint MaxHp { get; set; }
		
		void TakeDamage (float amount, Vector2 direction, Player attacker);
		void Death (Player killer);
	}
}