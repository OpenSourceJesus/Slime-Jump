using System;
using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class Cosmetic : MonoBehaviour, IComparable<Cosmetic>
	{
		public byte pointsRequired;
		public Type type;
		public Rigidbody2D rigid;
		public Collider2D collider;
		[HideInInspector]
		public Vector2 initLocalPosition;
		public bool Preview
		{
			get
			{
				return preview;
			}
			set
			{
				preview = value;
				gameObject.SetActive(value);
				if (value)
				{
					if (!CosmeticsMenu.equipped.Contains(this))
						CosmeticsMenu.equipped.Add(this);
				}
				else
					CosmeticsMenu.equipped.Remove(this);
			}
		}
		public bool Unlocked
		{
			get
			{
				return SaveAndLoadManager.GetBool("Unlocked " + name, false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Unlocked " + name, value);
			}
		}
		public bool Equipped
		{
			get
			{
				return SaveAndLoadManager.GetBool("Equipped " + name, false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Equipped " + name, value);
				Preview = value;
			}
		}
		public static Cosmetic[] instances = new Cosmetic[0];
		bool preview;

		void Awake ()
		{
			if (transform.parent == GameManager.instance.cosmeticNotificationPreviewParent)
				return;
			Equipped = Equipped;
			initLocalPosition = transform.localPosition;
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (Application.isPlaying)
				return;
			if (rigid == null)
				rigid = GetComponent<Rigidbody2D>();
			if (collider == null)
				collider = GetComponent<Collider2D>();
		}
#endif

		public int CompareTo (Cosmetic cosmetic)
		{
			if (transform.GetSiblingIndex() > cosmetic.transform.GetSiblingIndex())
				return 1;
			else
				return -1;
		}

		public void _Preview (bool preview)
		{
			if (preview)
			{
				for (int i = 0; i < CosmeticsMenu.equipped.Count; i ++)
				{
					Cosmetic cosmetic = CosmeticsMenu.equipped[i];
					if (type == cosmetic.type)
					{
						cosmetic.Preview = false;
						CosmeticsMenu.instance.toggles[instances.IndexOf(cosmetic)].isOn = false;
					}
				}
			}
			Preview = preview;
		}

		public void ShowUnlockNotification ()
		{
			Cosmetic cosmetic = Instantiate(this, GameManager.instance.cosmeticNotificationPreviewParent);
			cosmetic.transform.localPosition = Vector3.zero;
			Renderer[] renderers = cosmetic.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; i ++)
			{
				Renderer renderer = renderers[i];
				renderer.sortingLayerName = "UI";
				renderer.sortingOrder = 200;
			}
			GameManager.instance.cosmeticNotificationText.text = "Cosmetic unlocked!\n" + name;
			GameManager.instance.cosmeticNotificationGo.SetActive(true);
			EventManager.AddEvent(() => { cosmetic.gameObject.SetActive(true); }, Time.time + .1f);
			Destroy(cosmetic.gameObject, GameManager.instance.cosmeticNotificationDur);
		}

		public enum Type
		{
			Head,
			Body
		}
	}
}