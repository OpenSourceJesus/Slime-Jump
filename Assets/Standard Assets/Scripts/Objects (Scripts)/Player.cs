using Extensions;
using UnityEngine;
using UnityEngine.UI;
using Destructible2D;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace SlimeJump
{
	public class Player : SingletonUpdateWhileEnabled<Player>, IDestructable
	{
		public uint maxHp;
		public float Hp
		{
			get
			{
				return hp;
			}
			set
			{
			}
		}
		public uint MaxHp
		{
			get
			{
				return maxHp;
			}
			set
			{
			}
		}
		public Transform trs;
		public Transform graphicsTrs;
		public Transform colliderTrs;
		public Rigidbody2D rigid;
		public SpriteRenderer spriteRenderer;
		public Collider2D collider;
		public ShadowCaster2D shadowCaster;
		public Collider2D climbableSensor;
		public Collider2D wallSensor;
		[HideInInspector]
		public bool isGrounded;
		[HideInInspector]
		public bool isJumping;
		public float moveSpeed;
		public float jumpSpeed;
		public float climbSpeed;
		public float climbFallSpeed;
		[HideInInspector]
		public float multSpeed;
		public Transform acidBarTrs;
		[HideInInspector]
		public LayerMask whatICollideWith;
		public AffectedByVortex affectedByVortex;
		[HideInInspector]
		public Vector2 multiplySize = Vector2.one;
		[HideInInspector]
		public Vector2 addToPosition = Vector2.zero;
		public AnimationEntry jumpAnimationEntry;
		public AnimationEntry landAnimationEntry;
		public AnimationEntry hitWallAnimationEntry;
		public AnimationEntry idleAnimationEntry;
		public AnimationEntry inAcidFogAnimationEntry;
		[HideInInspector]
		public Vector2 prevAddToPosition;
		public DustCloud dustCloudPrefab;
		public float dustCloudsPerDistance;
		[HideInInspector]
		public float swingAngularVelocity;
		[HideInInspector]
		public float swingAngle;
		[HideInInspector]
		public Vector2 lastMovement;
		public Transform fracturerTrs;
		public D2dFracturer fracturer;
		public float explodeSpeed;
		public float respawnDelay;
		[HideInInspector]
		public bool inNoGravityZone;
		[HideInInspector]
		public bool lassoHasSlack;
		public float minCamMoveDistIfLassoHas0Slack;
		[HideInInspector]
		public bool invulnerable;
		public Animator animator;
		public Transform itemsParent;
		[HideInInspector]
		public Item[] items = new Item[0];
		[HideInInspector]
		public Weapon[] weapons = new Weapon[0];
		[HideInInspector]
		public UseableItem[] useableItems = new UseableItem[0];
		public SortedList<string, BulletPatternEntry> bulletPatternEntriesSortedList = new SortedList<string, BulletPatternEntry>();
		[HideInInspector]
		public Vector2 blasterLaunchVel;
		[HideInInspector]
		public float multiplyBlasterLaunchSpeedWithJumpIfJumpFirst;
		public float maxTimeAfterBlasterLaunchToSubtractJumpVel;
		[HideInInspector]
		public float timerTillBlasterLaunchNotSubtractJumpVel;
		public Transform hpBarTrs;
		public Image toggleShootLassoImage;
		public RectTransform changeLassoLengthSliderRectTrs;
		// public Slider changeLassoLengthSlider;
		public Sprite cancelShootLassoSprite;
		public Image toggleShootBlasterImage;
		public Sprite cancelShootBlasterSprite;
		[HideInInspector]
		public bool shootingLasso;
		[HideInInspector]
		public bool shootingBlaster;
		public Vector2 SavedPosition
		{
			get
			{
				return SaveAndLoadManager.GetVector2("Position " + _SceneManager.CurrentScene.name, new _Vector2(0, 0)).ToVec2();
			}
			set
			{
				SaveAndLoadManager.SetVector2 ("Position " + _SceneManager.CurrentScene.name, _Vector2.FromVec2(value));
			}
		}
		public RectTransform[] controlsRectTransforms = new RectTransform[0];
		public AudioClip shootSound;
		public float shootSoundVolume;
		public FloatRange shootSoundPitchRange;
		public AudioClip connectToStickyWallSound;
		public float connectToStickyWallSoundVolume;
		public FloatRange connectToStickyWallSoundPitchRange;
		public AudioClip disconnectToStickyWallSound;
		public float disconnectToStickyWallSoundVolume;
		public FloatRange disconnectToStickyWallSoundPitchRange;
		public AudioClip deathSound;
		public float deathSoundVolume;
		public AudioClip respawnSound;
		public float respawnSoundVolume;
		public AudioClip hitWallSound;
		public float hitWallSoundVolume;
		public FloatRange hitWallSoundPitchRange;
		public AudioClip[] jumpSounds = new AudioClip[0];
		public float jumpSoundsVolume;
		public FloatRange jumpSoundsPitchRange;
		[HideInInspector]
		public float respawnTimer;
#if UNITY_ANDROID || UNITY_IOS
		public static Vector2[] initControlsPositions = new Vector2[0];
		public static _Vector2[] ControlsPositions
		{
			get
			{
				_Vector2[] output = new _Vector2[Instance.controlsRectTransforms.Length];
				for (int i = 0; i < output.Length; i ++)
					output[i] = _Vector2.FromVec2(instance.controlsRectTransforms[i].anchoredPosition);
				return SaveAndLoadManager.GetVector2Array("Controls positions", output);
			}
			set
			{
				for (int i = 0; i < value.Length; i ++)
					Instance.controlsRectTransforms[i].anchoredPosition = value[i].ToVec2();
				SaveAndLoadManager.SetVector2Array ("Controls positions", value);
				SaveAndLoadManager.Save ();
			}
		}
#endif
		public static int Gems
		{
			get
			{
				return SaveAndLoadManager.GetInt("Gems " + _SceneManager.CurrentScene.name, 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Gems " + _SceneManager.CurrentScene.name, value);
			}
		}
		public static uint respawnedOnFrame;
		D2dFracturer[] fracturers = new D2dFracturer[0];
		float hp;
		float maxJumpDuration;
		float timeSinceJump;
		float affectedByVortexXVelocity;
		bool isClimbing;
		float xSize = 1;
		bool wasGrounded;
		bool isHittingWall;
		bool wasHittingWall;
		Vector2 prevPosition;
		float distanceTillMakeDustCloud;
		Vector2 prevDustCloudPosition;
		bool climbedSinceJumped;
		float jumpVel;
		float blasterLaunchDuration;
		float multiplyBlasterLaunchSpeedWithJumpIfJumpSecond;
		float moveInput;
		Sprite shootLassoSprite;
		Sprite shootBlasterSprite;
		bool jumpInput;
		bool isHittingNonStickyWall;
		LayerMask whatIsNotClimbable;
		bool prevWasGrounded;
		bool prevWasHittingWall;
		Vector2 prevSpriteRendererSize;

		public override void Awake ()
		{
			base.Awake ();
			if (!enabled)
				return;
			Vortex.instances = FindObjectsOfType<Vortex>(true);
			ShooterTrap.instances = FindObjectsOfType<ShooterTrap>(true);
			RecorderBox.instances = FindObjectsOfType<RecorderBox>(true);
			PlaybackBox.instances = FindObjectsOfType<PlaybackBox>(true);
			FallerObject.instances = FindObjectsOfType<FallerObject>(true);
			ChaserObject.instances = FindObjectsOfType<ChaserObject>(true);
			LaserShooter.instances = FindObjectsOfType<LaserShooter>(true);
			DissolveOnHit.instances = FindObjectsOfType<DissolveOnHit>(true);
			ToggleOverTime.instances = FindObjectsOfType<ToggleOverTime>(true);
			SavePoint.instances = FindObjectsOfType<SavePoint>(true);
			Gem.instances = FindObjectsOfType<Gem>(true);
			TouchSavePointAchievement.instances = FindObjectsOfType<TouchSavePointAchievement>();
			OneLifeAchievement.instances = FindObjectsOfType<OneLifeAchievement>();
			SpeedAchievement.instances = FindObjectsOfType<SpeedAchievement>();
			WinAchievement.instances = FindObjectsOfType<WinAchievement>();
			multSpeed = 1;
			whatICollideWith = Physics2D.GetLayerCollisionMask(gameObject.layer);
			whatIsNotClimbable = whatICollideWith.Remove("Climbable");
			// Don't treat kill-on-contact objects as walls — otherwise the move linecast stops the
			// player just short of touching them when chasing from behind (same direction).
			whatICollideWith = whatICollideWith.Remove("Arrow", "Hazard", "Homing Missile", "Saw", "Enemy", "Bouncy Bullet");
			items = itemsParent.GetComponentsInChildren<Item>();
			useableItems = itemsParent.GetComponentsInChildren<UseableItem>();
			weapons = itemsParent.GetComponentsInChildren<Weapon>();
			shootLassoSprite = toggleShootLassoImage.sprite;
			shootBlasterSprite = toggleShootBlasterImage.sprite;
			float yVelocity = jumpSpeed;
			while (yVelocity > 0)
			{
				yVelocity += Physics2D.gravity.y * Time.fixedDeltaTime;
				yVelocity *= 1f - rigid.linearDamping * Time.fixedDeltaTime;
				maxJumpDuration += Time.fixedDeltaTime;
			}
			for (int i = 0; i < items.Length; i ++)
			{
				Item item = items[i];
				if (item.gameObject.activeSelf)
					item.OnGain (this);
			}
			for (int i = 0; i < FallerObject.instances.Length; i ++)
			{
				FallerObject fallerObject = FallerObject.instances[i];
				fallerObject.Awake ();
			}
#if UNITY_ANDROID || UNITY_IOS
			for (int i = 0; i < controlsRectTransforms.Length; i ++)
				controlsRectTransforms[i].localScale = Vector3.one * SettingsMenu.ControlsScale;
			changeLassoLengthSliderRectTrs.eulerAngles = Vector3.forward * SettingsMenu.ChangeLassoLengthControlRot;
			ControlsPositions = ControlsPositions;
#endif
			hp = maxHp;
		}

		void Start ()
		{
			if (World.instance != null)
			{
				trs.position = SavedPosition;
				World.instance.SetPieces ();
				World.instance.Init ();
#if UNITY_EDITOR
				WorldMakerWindow.SetWorldActive (false);
#endif
			}
			GameCamera.Instance.HandlePosition ();
			Physics2D.SyncTransforms();
			CloudSpawner.instance.Init ();
			prevPosition = trs.position;
			Respawn ();
		}

		public override void DoUpdate ()
		{
			if (GameManager.paused)
				return;
			GameManager.Timer += Time.deltaTime * GameManager.instance.timeSpeed;
			if (GameManager.instance.timerText.gameObject.activeSelf)
				GameManager.instance.timerText.text = GameManager.Timer.ToString("F1");
			if (respawnTimer > 0)
			{
				respawnTimer -= Time.deltaTime;
				if (respawnTimer <= 0)
					Respawn ();
				else
					return;
			}
			prevAddToPosition = addToPosition;
			HandleMoving ();
			HandleJumping ();
			HandleClimbing ();
			Aim ();
			HandleAttacking ();
			timerTillBlasterLaunchNotSubtractJumpVel -= Time.deltaTime;
			if (isClimbing || inNoGravityZone)
				rigid.gravityScale = 0;
			else
				rigid.gravityScale = 1;
			if (!lassoHasSlack && Lasso.instance.isAttached && Lasso.instance.changeLengthInput == 0)
			{
				GameCamera.instance.followPlayer = false;
				if (((Vector2) (GameCamera.instance.trs.position - trs.position)).sqrMagnitude >= minCamMoveDistIfLassoHas0Slack * minCamMoveDistIfLassoHas0Slack)
				{
					GameCamera.instance.followPlayer = true;
					GameCamera.instance.HandlePosition ();
				}
			}
			else
				GameCamera.instance.followPlayer = true;
			affectedByVortexXVelocity += affectedByVortex.velocity.x * multSpeed;
			affectedByVortexXVelocity *= 1f - rigid.linearDamping * Time.deltaTime;
			float gravitySpeed = Physics2D.gravity.y * Time.deltaTime;
			if (blasterLaunchVel.y >= -gravitySpeed)
				blasterLaunchVel.y += gravitySpeed;
			else
				blasterLaunchVel.y = 0;
			if (affectedByVortexXVelocity == 0)
				blasterLaunchVel *= 1f - rigid.linearDamping * Time.deltaTime;
			else
				blasterLaunchVel.y *= 1f - rigid.linearDamping * Time.deltaTime;
			rigid.linearVelocity = rigid.linearVelocity.SetY((rigid.linearVelocity.y + affectedByVortex.velocity.y) * multSpeed);
			jumpVel += gravitySpeed;
			jumpVel *= 1f - rigid.linearDamping * Time.deltaTime;
			if (!jumpAnimationEntry.IsPlaying() && !landAnimationEntry.IsPlaying() && !hitWallAnimationEntry.IsPlaying())
			{
				multiplySize = Vector2.one;
				graphicsTrs.localPosition = Vector3.zero;
			}
			graphicsTrs.SetWorldScale (multiplySize.SetX(multiplySize.x * xSize).SetZ(1));
			colliderTrs.SetWorldScale (Vector3.one.SetX(xSize));
			Lasso.instance.trs.SetWorldScale (Vector3.one);
			if (isGrounded)
			{
				if (!prevWasGrounded && !wasGrounded && !isJumping)
					landAnimationEntry.Play ();
				addToPosition.y = -spriteRenderer.bounds.extents.y * (1f - multiplySize.y);
			}
			graphicsTrs.position += (Vector3) (addToPosition - prevAddToPosition);
			lastMovement = (Vector2) trs.position - prevPosition;
			MoveAchievement.MovedDistance += lastMovement.magnitude;
			MoveAchievement.Instance.HandleAchieve ();
			TimeAchievement.TimePlayed += Time.deltaTime;
			TimeAchievement.Instance.HandleAchieve ();
			prevWasGrounded = wasGrounded;
			wasGrounded = isGrounded;
			prevWasHittingWall = wasHittingWall;
			wasHittingWall = isHittingWall;
			prevPosition = trs.position;
			Material mat = new Material(spriteRenderer.sharedMaterial);
			mat.SetVector("_vel", lastMovement / Time.deltaTime);
			mat.SetInt("_grounded", isGrounded.GetHashCode());
			mat.SetInt("_hittingWall", isHittingWall.GetHashCode());
			spriteRenderer.sharedMaterial = mat;
			prevSpriteRendererSize = spriteRenderer.bounds.size;
		}

		public void SetMoveInput (float amt)
		{
			instance.moveInput = amt;
		}

		public void SetJumpInput (bool jump)
		{
			instance.jumpInput = jump;
		}

		public void ToggleShootLasso ()
		{
			if (instance != this)
			{
				instance.ToggleShootLasso ();
				return;
			}
			shootingLasso = !shootingLasso;
			if (shootingLasso)
				toggleShootLassoImage.sprite = cancelShootLassoSprite;
			else
				toggleShootLassoImage.sprite = shootLassoSprite;
		}

		public void SetChangeLengthInput (float amt)
		{
			Lasso.instance.changeLengthInput = (int) amt;
		}

		public void ToggleShootBlaster ()
		{
			if (instance != this)
			{
				instance.ToggleShootBlaster ();
				return;
			}
			shootingBlaster = !shootingBlaster;
			if (shootingBlaster)
				toggleShootBlasterImage.sprite = cancelShootBlasterSprite;
			else
				toggleShootBlasterImage.sprite = shootBlasterSprite;
		}

		public void HandleMoving ()
		{
#if !UNITY_ANDROID && !UNITY_IOS
			moveInput = InputManager.MoveInput;
#endif
			if (moveInput != 0)
			{
				Vector2 topPoint = new Vector2(collider.bounds.center.x + (collider.bounds.extents.x + Physics2D.defaultContactOffset * 2) * Mathf.Sign(moveInput), collider.bounds.max.y);
				Vector2 bottomPoint = new Vector2(collider.bounds.center.x + (collider.bounds.extents.x + Physics2D.defaultContactOffset * 2) * Mathf.Sign(moveInput), collider.bounds.min.y);
				isHittingWall = Physics2D.Linecast(topPoint, bottomPoint, whatICollideWith).collider != null;
				if (isHittingWall)
				{
					if (!prevWasHittingWall && !wasHittingWall)
						hitWallAnimationEntry.Play ();
					addToPosition.x = spriteRenderer.bounds.extents.x * (1f - multiplySize.x) * Mathf.Sign(moveInput);
				}
				xSize = Mathf.Sign(moveInput);
			}
			else
				isHittingWall = false;
			if (!isHittingWall)
				Move (moveInput);
			else
				Move (0);
		}

		public void Move (float speed)
		{
			lassoHasSlack = true;
			if (Lasso.instance.isAttached)
			{
				Vector2 toHitPoint = Lasso.instance.hookTrs.position - trs.position;
				float lengthRemaining = Lasso.instance.currentLength - toHitPoint.magnitude;
				if (lengthRemaining <= 0)
				{
					lassoHasSlack = false;
					trs.position += (Vector3) toHitPoint.normalized * -lengthRemaining;
					if (!inNoGravityZone)
					{
						float swingAngularAcceleration = Lasso.instance.swingSpeed * Mathf.Cos(swingAngle * Mathf.Deg2Rad);
						swingAngularVelocity += swingAngularAcceleration * Time.deltaTime;
						swingAngularVelocity *= 1f - rigid.linearDamping * Time.deltaTime;
						swingAngle += swingAngularVelocity / Lasso.instance.currentLength * Time.deltaTime;
						Lasso.instance.hookTrs.eulerAngles += Vector3.forward * swingAngularVelocity / Lasso.instance.currentLength * Time.deltaTime;
						trs.eulerAngles = Vector3.zero;
					}
				}
			}
			rigid.linearVelocity = rigid.linearVelocity.SetX(speed * moveSpeed + affectedByVortexXVelocity + blasterLaunchVel.x * multSpeed);
			float extraVelSign = MathfExtensions.Sign(affectedByVortexXVelocity + blasterLaunchVel.x);
			if (MathfExtensions.Sign(speed) != extraVelSign)
			{
				affectedByVortexXVelocity += speed * moveSpeed * multSpeed;
				if (MathfExtensions.Sign(affectedByVortexXVelocity + blasterLaunchVel.x) != extraVelSign)
				{
					float blasterVelSign = MathfExtensions.Sign(blasterLaunchVel.x);
					if (MathfExtensions.Sign(affectedByVortexXVelocity) != blasterVelSign)
					{
						blasterLaunchVel.x += affectedByVortexXVelocity;
						if (MathfExtensions.Sign(blasterLaunchVel.x) != blasterVelSign)
							blasterLaunchVel.x = 0;
					}
					affectedByVortexXVelocity = 0;
				}
			}
		}

		public void HandleJumping ()
		{
#if !UNITY_ANDROID && !UNITY_IOS
			jumpInput = InputManager.JumpInput;
#endif 
			if (isClimbing)
				return;
			if (jumpInput && isGrounded && !isJumping)
				StartJump ();
			else if (isJumping)
			{
				timeSinceJump += Time.deltaTime;
				if (!climbedSinceJumped)
				{
					Vector2 toCurrentPosition = (Vector2) trs.position - prevPosition;
					distanceTillMakeDustCloud -= toCurrentPosition.magnitude;
					Vector2 dustCloudPosition = prevPosition;
					while (distanceTillMakeDustCloud <= 0)
					{
						distanceTillMakeDustCloud += 1f / dustCloudsPerDistance;
						Instantiate(dustCloudPrefab, prevDustCloudPosition, Quaternion.identity);
						prevDustCloudPosition += toCurrentPosition.normalized / dustCloudsPerDistance;
					}
				}
				if (!jumpInput && timeSinceJump < maxJumpDuration && !inNoGravityZone)
					StopJump ();
				else if (rigid.linearVelocity.y <= 0)
				{
					isJumping = false;
					climbedSinceJumped = false;
				}
			}
		}

		void HandleClimbing ()
		{
			bool wasClimbing = isClimbing;
			isClimbing = climbableSensor.IsTouchingLayers(LayerMask.GetMask("Climbable"));
			if (isClimbing)
			{
				if (!wasClimbing)
				{
					SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(connectToStickyWallSound, Vector3.zero, connectToStickyWallSoundVolume);
					soundEffect.audioSource.pitch = connectToStickyWallSoundPitchRange.Get(Random.value);
					soundEffect.audioSource.spatialBlend = 0;
				}
				climbedSinceJumped = true;
				if (jumpInput)
				{
					isJumping = true;
					rigid.linearVelocity = rigid.linearVelocity.SetY((climbSpeed + affectedByVortex.velocity.y + blasterLaunchVel.y) * multSpeed);
					timeSinceJump = 0;
				}
				else
					rigid.linearVelocity = rigid.linearVelocity.SetY((-climbFallSpeed + affectedByVortex.velocity.y + blasterLaunchVel.y) * multSpeed);
			}
			else if (wasClimbing)
			{
				SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(disconnectToStickyWallSound, Vector3.zero, disconnectToStickyWallSoundVolume);
				soundEffect.audioSource.pitch = disconnectToStickyWallSoundPitchRange.Get(Random.value);
				soundEffect.audioSource.spatialBlend = 0;
				if (jumpInput)
					rigid.linearVelocity = rigid.linearVelocity.SetY(climbSpeed);
			}
		}

		public void StartJump ()
		{
			jumpAnimationEntry.Play ();
			isJumping = true;
			float multiplyJumpSpeed = 1;
			if (timerTillBlasterLaunchNotSubtractJumpVel > 0)
				multiplyJumpSpeed = multiplyBlasterLaunchSpeedWithJumpIfJumpSecond;
			rigid.linearVelocity += Vector2.up * jumpSpeed * multiplyJumpSpeed * multSpeed;
			jumpVel = jumpSpeed;
			timeSinceJump = 0;
			distanceTillMakeDustCloud = 0;
			prevDustCloudPosition = (Vector2) collider.bounds.center + Vector2.down * collider.bounds.extents.y;
			JumpAchievement.JumpCount ++;
			JumpAchievement.Instance.HandleAchieve ();
			AudioClip jumpSound = jumpSounds[Random.Range(0, jumpSounds.Length)];
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(jumpSound, Vector3.zero, jumpSoundsVolume);
			soundEffect.audioSource.pitch = jumpSoundsPitchRange.Get(Random.value);
			soundEffect.audioSource.spatialBlend = 0;
		}

		public void StopJump ()
		{
			if (climbedSinceJumped)
				rigid.linearVelocity = rigid.linearVelocity.SetY(0);
			else
				rigid.linearVelocity -= Vector2.up * jumpVel;
			isJumping = false;
			climbedSinceJumped = false;
			jumpVel = 0;
		}

		void Aim ()
		{
			if (InputManager.UsingGamepad)
			{
				Vector2 aimInput = Gamepad.current.rightStick.ReadValue();
				if (aimInput != Vector2.zero)
					itemsParent.up = aimInput;
			}
			else if (InputManager.UsingMouse)
				itemsParent.rotation = Quaternion.LookRotation(Vector3.forward, Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - trs.position);
			else
				itemsParent.rotation = Quaternion.LookRotation(Vector3.forward, Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue()) - trs.position);
		}

		public void HandleAttacking ()
		{
			if (InputManager.AttackInput)
			{
				for (int i = 0; i < weapons.Length; i ++)
				{
					Weapon weapon = weapons[i];
					if (weapon.gameObject.activeSelf && weapon.animationEntry.animator != null)
					{
						Blaster blaster = weapon as Blaster;
						if (blaster == null || trs.position.x < blaster.maxXPosToAllowUse)
#if UNITY_ANDROID || UNITY_IOS
							if (shootingBlaster)
#endif
								weapon.animationEntry.Play ();

					}
				}
			}
		}
		
		public void ShootBulletPatternEntry (string name)
		{
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(shootSound, Vector3.zero, shootSoundVolume);
			soundEffect.audioSource.pitch = shootSoundPitchRange.Get(Random.value);
			soundEffect.audioSource.spatialBlend = 0;
			bulletPatternEntriesSortedList[name].Shoot();
		}
		
		public void TakeDamage (float amount, Vector2 direction, Player attacker)
		{
			float prevHp = hp;
			hp = Mathf.Clamp(hp - amount, 0, maxHp);
			if (hpBarTrs != null)
			{
				if (prevHp > hp && hp > 0)
					for (int i = 0; i < prevHp - hp; i ++)
					{
						Image image = hpBarTrs.GetChild((int) hp - i).GetComponent<Image>();
						image.color = image.color.SetAlpha(0.25f);
					}
				else if (prevHp < hp)
					for (int i = 0; i < hp - prevHp; i ++)
					{
						Image image = hpBarTrs.GetChild((int) prevHp + i).GetComponent<Image>();
						image.color = image.color.SetAlpha(1);
					}
			}
			if (hp == 0)
				Death (attacker);
		}
		
		public void Death (Player killer)
		{
			if (respawnTimer > 0)
				return;
			shadowCaster.enabled = false;
			CloudSpawner.instance.enabled = false;
			respawnTimer = respawnDelay;
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(deathSound, Vector3.zero, deathSoundVolume);
			soundEffect.audioSource.spatialBlend = 0;
			acidBarTrs.parent.parent.gameObject.SetActive(false);
			GameCamera.instance.followPlayer = false;
			spriteRenderer.enabled = false;
			animator.speed = 0;
			EventManager.events.Clear();
			EventManager.instance.enabled = false;
			if (Lasso.instance.lineRenderer.enabled)
				Lasso.instance.Release ();
			Lasso.instance.enabled = false;
			for (int i = 0; i < CosmeticsMenu.equipped.Count; i ++)
			{
				Cosmetic cosmetic = CosmeticsMenu.equipped[i];
				if (cosmetic != null)
				{
					cosmetic.collider.isTrigger = false;
					cosmetic.rigid.simulated = true;
					cosmetic.transform.SetParent(null);
				}
			}
			GameObject newFracturerGo = Instantiate(fracturer.gameObject, fracturerTrs.parent);
			fracturerTrs.gameObject.SetActive(true);
			fracturerTrs.SetParent(null);
			fracturer.Fracture();
			fracturers = FindObjectsOfType<D2dFracturer>();
			for (int i = 0; i < fracturers.Length; i ++)
			{
				D2dFracturer fracturer = fracturers[i];
				Rigidbody2D rigid = fracturer.GetComponent<Rigidbody2D>();
				Vector2 toCenter = rigid.worldCenterOfMass - (Vector2) trs.position;
				rigid.linearVelocity = toCenter.normalized * explodeSpeed;
			}
			fracturer = newFracturerGo.GetComponent<D2dFracturer>();
			fracturerTrs = fracturer.transform;
			trs.SetParent(null);
			isGrounded = false;
			StopJump ();
			affectedByVortex.enabled = false;
			affectedByVortexXVelocity = 0;
			affectedByVortex.velocity = Vector2.zero;
			blasterLaunchVel = Vector2.zero;
			rigid.linearVelocity = Vector2.zero;
			rigid.simulated = false;
			OneLifeAchievement.savePointsNamesTouchedInOneLife.Clear();
			DieAchievement.DieCount ++;
			SaveAndLoadManager.Save ();
			DieAchievement.Instance.HandleAchieve ();
			if (Survival.instance != null)
				Survival.instance.OnPlayerDied ();
			GameManager.DeathCount ++;
			GameManager.instance.deathCounterText.text = "" + GameManager.DeathCount;
		}

		void Respawn ()
		{
			shadowCaster.enabled = true;
			EventManager.events.Clear();
			EventManager.instance.enabled = false;
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(respawnSound, Vector3.zero, respawnSoundVolume);
			soundEffect.audioSource.spatialBlend = 0;
			hp = maxHp;
			acidBarTrs.parent.parent.gameObject.SetActive(true);
			if (hpBarTrs != null)
			{
				if (hpBarTrs.childCount > 1)
					for (int i = 1; i < maxHp; i ++)
						DestroyImmediate(hpBarTrs.GetChild(0).gameObject);
				for (int i = 0; i < hp; i ++)
				{
					if (i > hpBarTrs.childCount - 1)
						Instantiate(hpBarTrs.GetChild(0).gameObject, hpBarTrs);
					else
					{
						Image image = hpBarTrs.GetChild(i).GetComponent<Image>();
						image.color = image.color.SetAlpha(1);
					}
				}
			}
			if (World.instance != null)
			{
				trs.position = SavedPosition;
				World.instance.Init ();
			}
			else
				trs.position = Vector3.zero;
			lastMovement = Vector2.zero;
			GameCamera.instance.followPlayer = true;
			GameCamera.instance.HandlePosition ();
			Physics2D.SyncTransforms();
			CloudSpawner.instance.enabled = true;
			prevPosition = trs.position;
			Lasso.instance.enabled = true;
			for (int i = 0; i < CosmeticsMenu.equipped.Count; i ++)
			{
				Cosmetic cosmetic = CosmeticsMenu.equipped[i];
				if (cosmetic != null)
				{
					cosmetic.collider.isTrigger = true;
					cosmetic.rigid.simulated = false;
					cosmetic.transform.SetParent(graphicsTrs);
					cosmetic.transform.localPosition = cosmetic.initLocalPosition;
					cosmetic.transform.localEulerAngles = Vector3.zero;
				}
			}
			animator.speed = 1;
			spriteRenderer.enabled = true;
			rigid.simulated = true;
			affectedByVortex.enabled = true;
			for (int i = 0; i < FallerObject.instances.Length; i ++)
			{
				FallerObject fallerObject = FallerObject.instances[i];
				fallerObject.Reset ();
			}
			for (int i = 0; i < ChaserObject.instances.Length; i ++)
			{
				ChaserObject chaserObject = ChaserObject.instances[i];
				chaserObject.trs.position = chaserObject.initPos;
			}
			for (int i = 0; i < LaserShooter.instances.Length; i ++)
			{
				LaserShooter laserShooter = LaserShooter.instances[i];
				laserShooter.OnEnable ();
			}
			for (int i = 0; i < ToggleOverTime.instances.Length; i ++)
			{
				ToggleOverTime toggleOverTime = ToggleOverTime.instances[i];
				toggleOverTime.Awake ();
			}
			for (int i = 0; i < Vortex.instances.Length; i ++)
			{
				Vortex vortex = Vortex.instances[i];
				vortex.affectedByVortexVelocitiesDict.Clear();
			}
			for (int i = 0; i < Gem.instances.Length; i ++)
			{
				Gem gem = Gem.instances[i];
				gem.gameObject.SetActive(true);
			}
			for (int i = 0; i < PlayerMimic.instances.Count; i ++)
			{
				PlayerMimic playerMimic = PlayerMimic.instances[i];
				Destroy(playerMimic.gameObject);
			}
			RecorderBox.areRecording = new RecorderBox[0];
			for (int i = 0; i < RecorderBox.instances.Length; i ++)
			{
				RecorderBox recorderBox = RecorderBox.instances[i];
				if (recorderBox.hasBeenUsed)
				{
					recorderBox.spriteRenderer.color = recorderBox.spriteRenderer.color.Multiply(2);
					recorderBox.hasBeenUsed = false;
				}
			}
			PlaybackBox.recordings = new PlayerRecording[0];
			for (int i = 0; i < PlaybackBox.instances.Length; i ++)
			{
				PlaybackBox playbackBox = PlaybackBox.instances[i];
				if (playbackBox.hasBeenUsed)
				{
					playbackBox.spriteRenderer.color = playbackBox.spriteRenderer.color.Multiply(2);
					playbackBox.playerMimics.Clear();
					playbackBox.hasBeenUsed = false;
				}
			}
			for (int i = 0; i < DissolveOnHit.instances.Length; i ++)
			{
				DissolveOnHit dissolveOnHit = DissolveOnHit.instances[i];
				dissolveOnHit.wasHit = false;
				dissolveOnHit.spriteRenderer.color = dissolveOnHit.spriteRenderer.color.SetAlpha(1);
				dissolveOnHit.gameObject.SetActive(true);
			}
			for (int i = 0; i < FollowWaypoints.instances.Length; i ++)
			{
				FollowWaypoints followWaypoints = FollowWaypoints.instances[i];
				if (followWaypoints != null)
				{
					followWaypoints.trs.position = followWaypoints.initPos;
					followWaypoints.trs.eulerAngles = Vector3.forward * followWaypoints.initRot;
					followWaypoints.currentWaypointIndex = followWaypoints.initCurrentWaypointIndex;
					followWaypoints.isBacktracking = followWaypoints.initIsBacktracking;
					followWaypoints.currWaypointTrs = followWaypoints.waypoints[followWaypoints.currentWaypointIndex].trs;
				}
			}
			for (int i = 0; i < Bullet.instances.Count; i ++)
			{
				Bullet bullet = Bullet.instances[i];
				if (bullet != null)
				{
					bullet.Despawn ();
					i --;
				}
			}
			Bullet.instances.Clear();
			for (int i = 0; i < LaserShooterLaser.instances.Count; i ++)
			{
				LaserShooterLaser laserShooterLaser = LaserShooterLaser.instances[i];
				Destroy(laserShooterLaser.gameObject);
				i --;
			}
			for (int i = 0; i < ShooterTrap.instances.Length; i ++)
			{
				ShooterTrap shooterTrap = ShooterTrap.instances[i];
				if (shooterTrap.gameObject.activeInHierarchy)
					shooterTrap.Awake ();
			}
			for (int i = 0; i < fracturers.Length; i ++)
			{
				D2dFracturer fracturer = fracturers[i];
				DestroyImmediate(fracturer.gameObject);
			}
			if (World.instance != null)
				World.instance.SetPieces ();
			respawnedOnFrame = GameManager.framesSinceLevelLoaded;
		}

		public void SetMultiplyBlasterLaunchSpeedWithJump ()
		{
			BlasterBullet blasterBullet = (BlasterBullet) bulletPatternEntriesSortedList["Blaster Shoot"].bulletPrefab;
			float yVelocity = blasterBullet.launchSpeed;
			while (yVelocity > 0)
			{
				yVelocity += Physics2D.gravity.y * Time.fixedDeltaTime;
				yVelocity *= 1f - rigid.linearDamping * Time.fixedDeltaTime;
				blasterLaunchDuration += Time.fixedDeltaTime;
			}
			multiplyBlasterLaunchSpeedWithJumpIfJumpFirst = (blasterBullet.launchSpeed / blasterLaunchDuration) / ((jumpSpeed + blasterBullet.launchSpeed) / (maxJumpDuration + blasterLaunchDuration));
			multiplyBlasterLaunchSpeedWithJumpIfJumpSecond = (jumpSpeed / maxJumpDuration) / ((jumpSpeed + blasterBullet.launchSpeed) / (maxJumpDuration + blasterLaunchDuration));
			multiplyBlasterLaunchSpeedWithJumpIfJumpFirst /= 2;
			multiplyBlasterLaunchSpeedWithJumpIfJumpSecond /= 2;
		}

		public virtual void OnCollisionEnter2D (Collision2D coll)
		{
			ContactPoint2D contactPnt = coll.GetContact(0);
			if (contactPnt.normal.y > Vector2.one.normalized.x * .75f && contactPnt.point.y < collider.bounds.center.y)
			{
				isGrounded = true;
				if (!isJumping && timerTillBlasterLaunchNotSubtractJumpVel <= 0 && rigid.linearVelocity.y > 0)
					rigid.linearVelocity = rigid.linearVelocity.SetY(-rigid.linearVelocity.y);
			}
			if (!isHittingNonStickyWall)
			{
				SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(hitWallSound, Vector3.zero, hitWallSoundVolume);
				soundEffect.audioSource.pitch = hitWallSoundPitchRange.Get(Random.value);
				soundEffect.audioSource.spatialBlend = 0;
			}
			isHittingNonStickyWall = true;
		}

		void OnCollisionStay2D (Collision2D coll)
		{
			OnCollisionEnter2D (coll);
		}

		public virtual void OnCollisionExit2D (Collision2D coll)
		{
			if (isGrounded && !isJumping && timerTillBlasterLaunchNotSubtractJumpVel <= 0 && rigid.linearVelocity.y > 0)
				rigid.linearVelocity = rigid.linearVelocity.SetY(-rigid.linearVelocity.y);
			isGrounded = false;
			if (!isClimbing)
				isHittingNonStickyWall = wallSensor.IsTouchingLayers(whatIsNotClimbable);
		}
	}
}