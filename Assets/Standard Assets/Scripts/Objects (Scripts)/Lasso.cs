using Extensions;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Lasso : SingletonUpdateWhileEnabled<Lasso>
	{
		public Transform trs;
		public LineRenderer lineRenderer;
		public float maxLength;
		public float shootSpeed;
		public LayerMask whatIHit;
		public float swingSpeed;
		[HideInInspector]
		public float currentLength;
		public float changeLengthSpeed;
		[HideInInspector]
		public float multiplyShootAndChangeLengthSpeed = 1;
		public Transform hookTrs;
		public Transform hookGraphicsTrs;
		public SpriteRenderer hookSpriteRenderer;
		public Sprite closedHookSprite;
		public Transform colliderTrs;
		[HideInInspector]
		public Transform hitTrs;
		[HideInInspector]
		public bool isAttached;
		public short maxXPosToAllowUse;
		[HideInInspector]
		public int changeLengthInput;
		public AudioClip[] shootSounds = new AudioClip[0];
		public float minShootSoundPlayDur;
		public float shootSoundVolume;
		public AudioClip retractSound;
		public FloatRange retractSoundPitchRange;
		public float retractSoundVolume;
		public AudioClip[] hitSounds = new AudioClip[0];
		public FloatRange hittSoundPitchRange;
		public float hittSoundVolume;
		public static bool Collected
		{
			get
			{
				return SaveAndLoadManager.GetBool("Collected lasso", false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Collected lasso", value);
			}
		}
		bool previousLassoInput;
		Vector2 shootVector;
		Vector2 previousHitTrsPosition;
		Sprite openHookSprite;
		SoundEffect shootSoundEffect;
		float timeAtLastShot;
		SoundEffect retractSoundEffect;
		float timeAtLastRetract;

		void Start ()
		{
			if (!Collected)
				gameObject.SetActive(false);
			else
				Player.instance.toggleShootLassoImage.gameObject.SetActive(true);
			hookTrs.SetParent(null);
			openHookSprite = hookSpriteRenderer.sprite;
		}

		public override void DoUpdate ()
		{
			bool lassoInput = InputManager.LassoInput;
#if UNITY_ANDROID || UNITY_IOS
			lassoInput &= InputManager.LassoInput && !EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue()) && Player.instance.shootingLasso;
#endif
			if (Player.instance.trs.position.x > maxXPosToAllowUse)
			{
				previousLassoInput = lassoInput;
				return;
			}
			if (lassoInput && !previousLassoInput)
			{
				lineRenderer.SetPositions(new Vector3[] { Vector2.zero, Vector2.zero });
				lineRenderer.enabled = true;
				if (InputManager.UsingGamepad)
					shootVector = Gamepad.current.rightStick.ReadValue();
				else if (InputManager.UsingMouse)
					shootVector = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - trs.position;
				else
					shootVector = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue()) - trs.position;
				hookTrs.position = trs.position;
				hookTrs.up = shootVector;
				hookTrs.gameObject.SetActive(true);
				colliderTrs.up = shootVector;
				colliderTrs.localScale = colliderTrs.localScale.SetY(0);
				colliderTrs.gameObject.SetActive(true);
				hookSpriteRenderer.gameObject.SetActive(true);
				if (retractSoundEffect != null)
					Destroy(retractSoundEffect.gameObject);
				shootSoundEffect = AudioManager.instance.MakeSoundEffect(shootSounds[Random.Range(0, shootSounds.Length)], Vector3.zero, shootSoundVolume);
				shootSoundEffect.audioSource.spatialBlend = 0;
				timeAtLastShot = Time.time;
			}
			else if (!lassoInput && previousLassoInput && lineRenderer.enabled)
				Release ();
			if (lineRenderer.enabled)
			{
				if (!isAttached)
				{
					Vector2 endPosition = lineRenderer.GetPosition(0);
					Vector2 newEndPosition = endPosition + shootVector.normalized * shootSpeed * multiplyShootAndChangeLengthSpeed * Time.deltaTime;
					Vector2 toNewEndPosition = newEndPosition - endPosition;
					float lengthRemaining = maxLength - endPosition.magnitude;
					lengthRemaining = Mathf.Min(lengthRemaining, toNewEndPosition.magnitude);
					RaycastHit2D hit = Physics2D.Raycast((Vector2) trs.position + endPosition, toNewEndPosition, lengthRemaining, whatIHit);
					if (hit.collider != null)
					{
						if (hit.collider.GetComponent<Slippery>() != null)
						{
							Release ();
							return;
						}
						Vector2 toHitPoint = hit.point - (Vector2) trs.position;
						lineRenderer.SetPosition(0, toHitPoint);
						currentLength = toHitPoint.magnitude;
						colliderTrs.localScale = colliderTrs.localScale.SetY(currentLength);
						hookTrs.position = hit.point;
						hookTrs.up = toHitPoint;
						float toHitPointAngle = toHitPoint.GetFacingAngle() * Mathf.Deg2Rad;
						Player.instance.swingAngularVelocity = (Player.instance.rigid.linearVelocity.x * Mathf.Cos(toHitPointAngle) + Player.instance.rigid.linearVelocity.y * Mathf.Sin(toHitPointAngle)) / currentLength;
						Player.instance.swingAngle = toHitPoint.GetFacingAngle();
						hitTrs = hit.collider.transform;
						previousHitTrsPosition = hitTrs.position;
						hookSpriteRenderer.sprite = closedHookSprite;
						isAttached = true;
						Player.instance.trs.SetParent(hookTrs);
						if (shootSoundEffect != null)
							Destroy(shootSoundEffect.gameObject, minShootSoundPlayDur - (Time.time - timeAtLastShot));
						SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(hitSounds[Random.Range(0, hitSounds.Length)], hit.point, hittSoundVolume);
						soundEffect.audioSource.pitch = hittSoundPitchRange.Get(Random.value);
// #if UNITY_ANDROID || UNITY_IOS
// 						Player.instance.changeLassoLengthSlider.interactable = true;
// #endif
					}
					else
					{
						hookTrs.position = Player.instance.trs.position + (Vector3) newEndPosition;
						lineRenderer.SetPositions(new Vector3[] { newEndPosition, Vector2.zero });
						currentLength = newEndPosition.magnitude;
						colliderTrs.localScale = colliderTrs.localScale.SetY(currentLength);
						if (lengthRemaining <= 0)
							Release ();
					}
				}
				else
				{
					hookTrs.position += (Vector3) ((Vector2) hitTrs.position - previousHitTrsPosition);
					previousHitTrsPosition = hitTrs.position;
					Vector2 toHitPoint = hookTrs.position - trs.position;
					hookGraphicsTrs.up = toHitPoint;
#if !UNITY_ANDROID && !UNITY_IOS
					changeLengthInput = InputManager.ChangeLassoLengthInput;
#endif
					if (changeLengthInput != 0)
					{
						ContactFilter2D contactFilter = new ContactFilter2D();
						contactFilter.useLayerMask = true;
						contactFilter.layerMask = Player.instance.whatICollideWith;
						float changeLengthAmount = changeLengthInput * changeLengthSpeed * multiplyShootAndChangeLengthSpeed * Time.deltaTime;
						if (changeLengthInput > 0 || Player.instance.collider.Cast(toHitPoint, contactFilter, new RaycastHit2D[1], Mathf.Abs(changeLengthAmount) * 6) == 0)
						{
							currentLength = Mathf.Clamp(currentLength + changeLengthAmount, 0, maxLength);
							colliderTrs.localScale = colliderTrs.localScale.SetY(currentLength);
						}
					}
					lineRenderer.SetPosition(0, toHitPoint);
					if (!hitTrs.gameObject.activeInHierarchy)
						Release ();
					GameCamera.instance.HandlePosition ();
					Camera.main.enabled = false;
					GameCamera.instance.camera.Render();
					GameCamera.instance.camera.enabled = true;
				}
			}
			previousLassoInput = lassoInput;
		}

		public void Release ()
		{
			lineRenderer.enabled = false;
			hookTrs.gameObject.SetActive(false);
			if (isAttached)
			{
				Player.instance.trs.SetParent(null);
				hookSpriteRenderer.sprite = openHookSprite;
				hookGraphicsTrs.localEulerAngles = Vector3.zero;
				colliderTrs.gameObject.SetActive(false);
				isAttached = false;
				// #if UNITY_ANDROID || UNITY_IOS
				// 				Player.instance.changeLassoLengthSlider.interactable = false;
				// #endif
				Player.instance.rigid.linearVelocity = Player.instance.lastMovement / Time.deltaTime;
			}
			if (shootSoundEffect != null)
				Destroy(shootSoundEffect.gameObject, minShootSoundPlayDur - (Time.time - timeAtLastShot));
			if (retractSoundEffect != null)
				Destroy(retractSoundEffect.gameObject);
			retractSoundEffect = AudioManager.instance.MakeSoundEffect(retractSound, Vector3.zero, retractSoundVolume);
			retractSoundEffect.audioSource.spatialBlend = 0;
			retractSoundEffect.audioSource.pitch = retractSoundPitchRange.Get(Random.value);
			timeAtLastRetract = Time.time;
		}
	}
}