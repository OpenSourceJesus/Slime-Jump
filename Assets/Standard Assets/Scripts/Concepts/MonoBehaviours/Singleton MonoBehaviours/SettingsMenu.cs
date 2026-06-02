using Extensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SlimeJump
{
	public class SettingsMenu : SingletonMonoBehaviour<SettingsMenu>
	{
		public _Slider volumeSlider;
		public Button muteButton;
		public _Slider timeSpeedSlider;
		public _Slider controlsScaleSlider;
		public _Slider changeLassoLengthControlRotSlider;
		public GameObject deathCounterOnGo;
		public GameObject deathCounterOffGo;
		public GameObject showTimerOnGo;
		public GameObject showTimerOffGo;
		public static float Volume
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Volume", 1);
			}
			set
			{
				AudioListener.volume = value;
				SaveAndLoadManager.SetFloat ("Volume", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static bool EnableSound
		{
			get
			{
				return SaveAndLoadManager.GetBool("Enable sound", true);
			}
			set
			{
				AudioListener.pause = !value;
				SaveAndLoadManager.SetBool ("Enable sound", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static float TimeSpeed
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Time speed", GameManager.instance.timeSpeed);
			}
			set
			{
				GameManager.instance.timeSpeed = value;
				SaveAndLoadManager.SetFloat ("Time speed", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static bool DeathCounter
		{
			get
			{
				return SaveAndLoadManager.GetBool("Death counter", false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Death counter", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static bool ShowTimer
		{
			get
			{
				return SaveAndLoadManager.GetBool("Show timer", false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Show timer", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static float ControlsScale
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Controls scale", 1);
			}
			set
			{
				for (int i = 0; i < Player.Instance.controlsRectTransforms.Length; i ++)
					Player.instance.controlsRectTransforms[i].localScale = Vector3.one * value;
				SaveAndLoadManager.SetFloat ("Controls scale", value);
				SaveAndLoadManager.Save ();
			}
		}
		public static float ChangeLassoLengthControlRot
		{
			get
			{
				return SaveAndLoadManager.GetInt("Change lasso length control rot", 90);
			}
			set
			{
				Player.instance.changeLassoLengthSliderRectTrs.eulerAngles = Vector3.forward * value;
				SaveAndLoadManager.SetInt ("Change lasso length control rot", (int) value);
				SaveAndLoadManager.Save ();
			}
		}
#if UNITY_ANDROID || UNITY_IOS
		DragControlUpdater dragControlUpdater;
#endif

		public override void Awake ()
		{
			base.Awake ();
			gameObject.SetActive(false);
#if UNITY_ANDROID || UNITY_IOS
			Player.initControlsPositions = new Vector2[Player.Instance.controlsRectTransforms.Length];
			for (int i = 0; i < Player.instance.controlsRectTransforms.Length; i ++)
				Player.initControlsPositions[i] = Player.instance.controlsRectTransforms[i].anchoredPosition;
			Player.ControlsPositions = Player.ControlsPositions;
			controlsScaleSlider.Start ();
			controlsScaleSlider.value = ControlsScale;
			changeLassoLengthControlRotSlider.Start ();
			changeLassoLengthControlRotSlider.value = ChangeLassoLengthControlRot;
#endif
			SaveAndLoadManager.Init ();
			volumeSlider.Start ();
			volumeSlider.value = Volume;
			if (!EnableSound)
				muteButton.onClick.Invoke();
			timeSpeedSlider.Start ();
			timeSpeedSlider.value = TimeSpeed;
			deathCounterOnGo.SetActive(DeathCounter);
			deathCounterOffGo.SetActive(!DeathCounter);
			showTimerOnGo.SetActive(ShowTimer);
			showTimerOffGo.SetActive(!ShowTimer);
		}

#if UNITY_ANDROID || UNITY_IOS
		public void StartDragControl (RectTransform rectTrs)
		{
			if (dragControlUpdater != null)
				GameManager.updatables = GameManager.updatables.Remove(dragControlUpdater);
			dragControlUpdater = new DragControlUpdater(rectTrs);
			GameManager.updatables = GameManager.updatables.Add(dragControlUpdater);
		}
		
		public void EndDragControl (RectTransform rectTrs)
		{
			GameManager.updatables = GameManager.updatables.Remove(dragControlUpdater);
			dragControlUpdater = null;
		}

		public void ResetControls ()
		{
			_Vector2[] controlsPositions = new _Vector2[Player.initControlsPositions.Length];
			for (int i = 0; i < Player.initControlsPositions.Length; i ++)
				controlsPositions[i] = _Vector2.FromVec2(Player.initControlsPositions[i]);
			Player.ControlsPositions = controlsPositions;
			controlsScaleSlider.value = 1;
			changeLassoLengthControlRotSlider.value = 90;
		}

		public void SaveControls ()
		{
			_Vector2[] controlsPositions = new _Vector2[Player.Instance.controlsRectTransforms.Length];
			for (int i = 0; i < controlsPositions.Length; i ++)
				controlsPositions[i] = _Vector2.FromVec2(Player.instance.controlsRectTransforms[i].anchoredPosition);
			Player.ControlsPositions = controlsPositions;
		}

		class DragControlUpdater : IUpdatable
		{
			public RectTransform rectTrs;
			RectTransform canvasRectTrs;
			Vector2 touchOff;

			public DragControlUpdater (RectTransform rectTrs)
			{
				this.rectTrs = rectTrs;
				canvasRectTrs = rectTrs.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
				touchOff = canvasRectTrs.sizeDelta * (rectTrs.GetCenterInCanvasNormalized(canvasRectTrs) - canvasRectTrs.GetWorldRect().ToNormalizedPosition(Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue())));
			}

			public void DoUpdate ()
			{
				rectTrs.anchoredPosition = canvasRectTrs.sizeDelta * canvasRectTrs.GetWorldRect().ToNormalizedPosition(Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue())) + touchOff;
			}
		}
#endif
	}
}