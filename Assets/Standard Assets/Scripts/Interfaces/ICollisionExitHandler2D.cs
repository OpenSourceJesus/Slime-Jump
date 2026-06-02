using UnityEngine;

namespace SlimeJump
{
	public interface ICollisionExitHandler
	{
        Collider2D Collider { get; }
        
        void OnCollisionExit2D (Collision2D coll);
	}
}