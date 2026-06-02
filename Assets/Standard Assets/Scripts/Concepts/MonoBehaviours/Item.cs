using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Item : MonoBehaviour
	{
		[Multiline]
		public string description;
		public uint value;
		public Transform trs;
		public byte maxCount;
		public Item[] combinedWith = new Item[0];

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (trs == null)
				trs = GetComponent<Transform>();
			if (combinedWith.Length == 0)
				combinedWith = GetComponentsInChildren<Item>().Remove(this);
		}
#endif

		public virtual void OnGain (Player player)
		{
			for (int i = 0; i < combinedWith.Length; i ++)
			{
				Item item = combinedWith[i];
				if (item.gameObject.activeSelf)
					item.OnGain (player);
			}
		}

		public virtual void OnDisable ()
		{
		}

		public override string ToString ()
		{
			string output = description;
			if (!string.IsNullOrEmpty(output) && !output.EndsWith("\n"))
				output += "\n";
			for (int i = 0; i < combinedWith.Length; i ++)
			{
				Item item = combinedWith[i];
				output += item.ToString(); 
			}
			if (maxCount > 0)
			{
				if (output.Contains("\n"))
					output += "\n";
				else
				{
					if (!string.IsNullOrEmpty(description) && description.EndsWith("."))
						output += ".";
					output += " ";
				}
				output += "You can only buy this " + maxCount + " times";
				if (!string.IsNullOrEmpty(description))
					output += ".";
			}
			if (output.EndsWith("\n"))
				output = output.Remove(output.Length - 1);
			return output;
		}
	}
}