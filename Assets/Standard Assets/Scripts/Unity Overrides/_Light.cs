using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class _Light : MonoBehaviour
{
	float range;
	public Transform trs;
	public new Light light;
	
	void Start ()
	{
		range = light.range;
	}
	
	void Update ()
	{
		light.range = range * trs.localScale.x;
	}
}
