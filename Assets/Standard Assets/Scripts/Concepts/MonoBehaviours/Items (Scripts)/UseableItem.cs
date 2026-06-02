using Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

namespace SlimeJump
{
	public class UseableItem : Item
	{
		public InputAction useAction;
		public float cooldown;
		[HideInInspector]
		public float cooldownRemaining;
		public float maxUseTime;
		public Image cooldownIndicatorImage;
		float lastUseTime;
		UseUpdater useUpdater;
		CooldownUpdater cooldownUpdater;

		public override void OnGain (Player player)
		{
			useAction.Enable();
			if (cooldown > 0 && cooldownIndicatorImage != null)
				cooldownIndicatorImage.rectTransform.parent.gameObject.SetActive(true);
		}

		public void TryToUse (InputAction.CallbackContext context = default(InputAction.CallbackContext))
		{
			if (useUpdater == null && Time.time - lastUseTime >= cooldown)
				Use ();
		}

		public virtual void Use ()
		{
			lastUseTime = Time.time;
			if (cooldownIndicatorImage != null)
				cooldownIndicatorImage.fillAmount = 0;
			if (maxUseTime > 0)
			{
				if (useUpdater == null)
					OnBeganUsing ();
			}
			else
			{
				cooldownUpdater = new CooldownUpdater(this);
				GameManager.updatables = GameManager.updatables.Add(cooldownUpdater);
			}
		}

		public virtual void OnBeganUsing ()
		{
			useUpdater = new UseUpdater(this);
			GameManager.updatables = GameManager.updatables.Add(useUpdater);	
		}

		public virtual void OnStopUsing ()
		{
			float timeOvershoot = Time.time - lastUseTime - maxUseTime;
			if (timeOvershoot > 0)
				lastUseTime = Time.time - timeOvershoot;
			GameManager.updatables = GameManager.updatables.Remove(useUpdater);
			useUpdater = null;
			cooldownUpdater = new CooldownUpdater(this);
			GameManager.updatables = GameManager.updatables.Add(cooldownUpdater);
			PlayerPrefs.SetInt("Used ability", PlayerPrefs.GetInt("Used ability", 0) + 1);
		}

		public override void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(useUpdater);
			GameManager.updatables = GameManager.updatables.Remove(cooldownUpdater);
			useAction.Disable();
		}

		public override string ToString ()
		{
			string output = base.ToString() + " ";
			if (maxUseTime > 0)
			{
				output += "Max use time: " + maxUseTime;
				if (maxUseTime < Mathf.Infinity)
					output += "s. ";
				else
					output += " ";
			}
			output += "Cooldown: " + cooldown + "s.\n";
			return output;
		}

		class UseUpdater : IUpdatable
		{
			UseableItem useableItem;
			float usedTime;

			public UseUpdater (UseableItem useableItem)
			{
				this.useableItem = useableItem;
			}

			public void DoUpdate ()
			{
				if (useableItem.useAction.enabled && useableItem.useAction.ReadValue<float>() == 1)
				{
					useableItem.Use ();
					usedTime += Time.deltaTime;
					if (usedTime >= useableItem.maxUseTime)
						useableItem.OnStopUsing ();
				}
				else
					useableItem.OnStopUsing ();
			}
		}

		class CooldownUpdater : IUpdatable
		{
			UseableItem useableItem;

			public CooldownUpdater (UseableItem useableItem)
			{
				this.useableItem = useableItem;
			}

			public void DoUpdate ()
			{
				if (useableItem == null || useableItem.cooldownIndicatorImage == null)
				{
					GameManager.updatables = GameManager.updatables.Remove(this);
					return;
				}
				useableItem.cooldownIndicatorImage.fillAmount = (Time.time - useableItem.lastUseTime) / useableItem.cooldown;
				if (Time.time - useableItem.lastUseTime >= useableItem.cooldown)
				{
					useableItem.cooldownIndicatorImage.fillAmount = 1;
					GameManager.updatables = GameManager.updatables.Remove(this);
				}
			}
		}
	}
}