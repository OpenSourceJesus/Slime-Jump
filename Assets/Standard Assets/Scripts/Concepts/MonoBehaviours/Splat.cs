using UnityEngine;
using UnityEngine.Events;

public class Splat : MonoBehaviour
{
	public float lifetime;
	public UnityEvent onDestroy;

	void Awake ()
	{
		Destroy(gameObject, lifetime);
	}

	void OnDestroy ()
	{
		onDestroy.Invoke();
	}
}