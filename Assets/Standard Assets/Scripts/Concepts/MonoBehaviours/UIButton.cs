using Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SlimeJump
{
	public class UIButton : Button, IUpdatable
	{
		public Image image;
		public GameObject goWhenPressed;
		public GameObject goWhenNotPressed;
		public AudioClip mouseOverSound;
		public float mouseOverSoundVolume;
		public AudioClip clickSound;
		public float clickSoundVolume;
		bool pointerInside;

		protected override void OnEnable ()
		{
			base.OnEnable ();
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		public virtual void DoUpdate ()
		{
			if (!interactable || InputManager.UsingGamepad)
				return;
			if (IsPressed())
				StartPress ();
			else
				EndPress ();
		}

		public override void OnPointerEnter (PointerEventData eventData)
		{
			pointerInside = true;
			base.OnPointerEnter (eventData);
			OnMouseOver ();
		}

		public override void OnPointerExit (PointerEventData eventData)
		{
			pointerInside = false;
			base.OnPointerExit (eventData);
		}

		protected override void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(this);
			pointerInside = false;
			EndPress ();
			base.OnDisable ();
		}

		protected override void DoStateTransition (SelectionState state, bool instant)
		{
			if (state == SelectionState.Selected && !pointerInside)
				state = SelectionState.Normal;
			base.DoStateTransition (state, instant);
		}

		public void StartPress ()
		{
			if (goWhenPressed != null)
				goWhenPressed.SetActive(true);
			if (goWhenNotPressed != null)
				goWhenNotPressed.SetActive(false);
			if (image != null)
				image.color = image.color.SetAlpha(0);
			if (InputManager.UsingGamepad)
				AudioManager.instance.MakeSoundEffect (clickSound, GameCamera.instance.trs.position, clickSoundVolume);
		}

		public void EndPress ()
		{
			if (goWhenPressed != null)
				goWhenPressed.SetActive(false);
			if (goWhenNotPressed != null)
				goWhenNotPressed.SetActive(true);
			if (image != null)
				image.color = image.color.SetAlpha(1);
		}

		public void OnMouseOver ()
		{
			if (!IsPressed())
				AudioManager.instance.MakeSoundEffect (mouseOverSound, GameCamera.instance.trs.position, mouseOverSoundVolume);
		}
	}
}