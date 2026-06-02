using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlimeJump
{
	public class GameManager : SingletonMonoBehaviour<GameManager>
	{
		// public GameObject[] registeredGos = new GameObject[0];
		// [SaveAndLoadValue]
		// static string EnabledGoNamesString
		// {
		// 	get
		// 	{
		// 		return SaveAndLoadManager.GetString("Enabled GameObject names");
		// 	}
		// 	set
		// 	{
		// 		SaveAndLoadManager.SetString ("Enabled GameObject names", value);
		// 	}
		// }
		// [SaveAndLoadValue]
		// static string DisabledGoNamesString
		// {
		// 	get
		// 	{
		// 		return SaveAndLoadManager.GetString("Disabled GameObject names");
		// 	}
		// 	set
		// 	{
		// 		SaveAndLoadManager.SetString ("Disabled GameObject names", value);
		// 	}
		// }
		// [SaveAndLoadValue]
		// public GameModifier[] gameModifiers = new GameModifier[0];
		public float timeSpeed;
		public TemporaryActiveText notificationTemporaryActiveText;
		public GameObject achieveNotificationGo;
		public Image achieveNotificationImage;
		public TMP_Text achieveNotificationText;
		public float achieveNotificationDur;
		public GameObject cosmeticNotificationGo;
		public Transform cosmeticNotificationPreviewParent;
		public TMP_Text cosmeticNotificationText;
		public float cosmeticNotificationDur;
		public TMP_Text speedrunTimerText;
		public TemporaryActiveText saveNotificationTempActiveText;
		public float mouseHideDelay;
		public bool isDemo;
		public TMP_Text deathCounterText;
		public TMP_Text timerText;
		// public static Dictionary<string, GameModifier> gameModifierDict = new Dictionary<string, GameModifier>();
		public static bool paused;
		public static IUpdatable[] updatables = new IUpdatable[0];
		public static uint framesSinceLevelLoaded;
		public static bool isQuittingGame;
		public static float pausedTime;
		public static float TimeSinceLevelLoad
		{
			get
			{
				return Time.timeSinceLevelLoad - pausedTime;
			}
		}
		public static int DeathCount
		{
			get
			{
				return SaveAndLoadManager.GetInt("Death count " + _SceneManager.CurrentScene.name, 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Death count " + _SceneManager.CurrentScene.name, value);
				SaveAndLoadManager.Save ();
			}
		}
		public static float Timer
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Timer " + _SceneManager.CurrentScene.name, 0);
			}
			set
			{
				SaveAndLoadManager.SetFloat ("Timer " + _SceneManager.CurrentScene.name, value);
				SaveAndLoadManager.Save ();
			}
		}
		public const int LAGGY_FRAMES_ON_LOAD_SCENE = 2;
		// static bool previousPreviousSceneInput;
		// static bool previousNextSceneInput;
		// static bool nextSceneInput;
		// static bool previousSceneInput;
		// const string STRING_SEPERATOR = "|";
		Vector2 prevMousePos;
		float mouseHideTimer;
#if !UNITY_EDITOR
		bool initialized;
#endif

		public override void Awake ()
		{
			base.Awake ();
#if !UNITY_WEBGL
			SaveAndLoadManager.Init ();
#endif
#if !UNITY_EDITOR
			if (!initialized)
			{
				initialized = true;
				OneLifeAchievement.openedMainMenuCnt = 0;
				PlayCountAchievement.PlayCount ++;
				SaveAndLoadManager.Save ();
				PlayCountAchievement.Instance.HandleAchieve ();
			}
#endif
			// SetGameObjectsActive ();
			if (instance != this)
				return;
			// gameModifierDict.Clear();
			// for (int i = 0; i < gameModifiers.Length; i ++)
			// {
			// 	GameModifier gameModifier = gameModifiers[i];
			// 	gameModifierDict.Add(gameModifier.name, gameModifier);
			// }
			SceneManager.sceneLoaded += OnSceneLoaded;
			QualitySettings.globalTextureMipmapLimit = 0;
			timeSpeed = SettingsMenu.TimeSpeed;
			if (World.Instance != null)
			{
				deathCounterText.text = "" + DeathCount;
				deathCounterText.gameObject.SetActive(SettingsMenu.DeathCounter);
				timerText.text = Timer.ToString("F1");
				timerText.gameObject.SetActive(SettingsMenu.ShowTimer);
			}
		}

		void Update ()
		{
			if (framesSinceLevelLoaded == 1 && achieveNotificationGo != null)
				achieveNotificationGo.SetActive(false);
			Physics2D.SyncTransforms();
			for (int i = 0; i < updatables.Length; i ++)
			{
				IUpdatable updatable = updatables[i];
				updatable.DoUpdate ();
			}
			if (ObjectPool.Instance != null && ObjectPool.instance.enabled)
				ObjectPool.instance.DoUpdate ();
			if ((InputManager.UsingKeyboard && Keyboard.current.escapeKey.wasPressedThisFrame) || (InputManager.UsingGamepad && Gamepad.current.startButton.wasPressedThisFrame))
				_SceneManager.instance.LoadScene ("Main Menu");
			InputSystem.Update ();
			// nextSceneInput = (InputManager.UsingKeyboard && Keyboard.current.nKey.isPressed) || (InputManager.UsingGamepad && Gamepad.current.rightShoulder.isPressed);
			// previousSceneInput = (InputManager.UsingKeyboard && Keyboard.current.bKey.isPressed) || (InputManager.UsingGamepad && Gamepad.current.leftShoulder.isPressed);
			// else if (nextSceneInput && !previousNextSceneInput)
			// 	_SceneManager.instance.NextScene ();
			// else if (previousSceneInput && !previousPreviousSceneInput)
			// 	_SceneManager.instance.PreviousScene ();
			if (paused)
				pausedTime += Time.unscaledDeltaTime;
			else if (SpeedAchievement.current != null)
			{
				float timeSinceStart = Time.time - SpeedAchievement.startTime;
				if (timeSinceStart > SpeedAchievement.current.duration / timeSpeed)
					speedrunTimerText.color = Color.red;
				else
					speedrunTimerText.color = Color.white;
				speedrunTimerText.text = timeSinceStart.ToString("F1");
			}
			Vector2 mousePos = Mouse.current.position.ReadValue();
			if (mousePos != prevMousePos)
			{
				Cursor.visible = true;
				mouseHideTimer = mouseHideDelay;
			}
			mouseHideTimer -= Time.deltaTime;
			if (mouseHideTimer <= 0)
				Cursor.visible = false;
			prevMousePos = mousePos;
			// previousNextSceneInput = nextSceneInput;
			// previousPreviousSceneInput = previousSceneInput;
			framesSinceLevelLoaded ++;
		}

		void OnDestroy ()
		{
			if (instance == this)
				SceneManager.sceneLoaded -= OnSceneLoaded;
		}
		
		void OnSceneLoaded (Scene scene = new Scene(), LoadSceneMode loadMode = LoadSceneMode.Single)
		{
			Instance.StopAllCoroutines();
			framesSinceLevelLoaded = 0;
			pausedTime = 0;
		}

		public void DisplayNotification (string text)
		{
			notificationTemporaryActiveText.text.text = text;
			notificationTemporaryActiveText.Do ();
		}

		public void BeginGame (string sceneName)
		{
			Intro.LoadSceneNameAtEnd = sceneName;
			if (Intro.AlreadyDone)
				_SceneManager.Instance.LoadScene (sceneName);
			else
				_SceneManager.Instance.LoadScene ("Intro");
		}

		public void OpenMainMenu ()
		{
			_SceneManager.Instance.LoadScene ("Main Menu");
			OneLifeAchievement.openedMainMenuCnt ++;
		}

		// public static void SetGameObjectsActive ()
		// {
		// 	string[] stringSeperators = { STRING_SEPERATOR };
		// 	string[] enabledGoNames = EnabledGoNamesString.Split(stringSeperators, StringSplitOptions.None);
		// 	List<GameObject> registeredGosRemaining = new List<GameObject>(Instance.registeredGos);
		// 	for (int i = 0; i < enabledGoNames.Length; i ++)
		// 	{
		// 		string goName = enabledGoNames[i];
		// 		for (int i2 = 0; i2 < registeredGosRemaining.Count; i2 ++)
		// 		{
		// 			GameObject registeredGo = registeredGosRemaining[i2];
		// 			if (goName == registeredGo.name)
		// 			{
		// 				registeredGo.SetActive(true);
		// 				registeredGosRemaining.RemoveAt(i2);
		// 				break;
		// 			}
		// 		}
		// 	}
		// 	string[] disabledGoNames = DisabledGoNamesString.Split(stringSeperators, StringSplitOptions.None);
		// 	for (int i = 0; i < disabledGoNames.Length; i ++)
		// 	{
		// 		string goName = disabledGoNames[i];
		// 		GameObject go = GameObject.Find(goName);
		// 		if (go != null)
		// 			go.SetActive(false);
		// 	}
		// }
		
		// public static void ActivateGameObjectForever (GameObject go)
		// {
		// 	go.SetActive(true);
		// 	ActivateGameObjectForever (go.name);
		// }
		
		// public static void DeactivateGameObjectForever (GameObject go)
		// {
		// 	go.SetActive(false);
		// 	DeactivateGameObjectForever (go.name);
		// }
		
		// public static void ActivateGameObjectForever (string goName)
		// {
		// 	DisabledGoNamesString = DisabledGoNamesString.Replace(STRING_SEPERATOR + goName, "");
		// 	if (!EnabledGoNamesString.Contains(goName))
		// 		EnabledGoNamesString += STRING_SEPERATOR + goName;
		// }
		
		// public static void DeactivateGameObjectForever (string goName)
		// {
		// 	EnabledGoNamesString = EnabledGoNamesString.Replace(STRING_SEPERATOR + goName, "");
		// 	if (!DisabledGoNamesString.Contains(goName))
		// 		DisabledGoNamesString += STRING_SEPERATOR + goName;
		// }

		public static void SetPaused (bool pause)
		{
			paused = pause;
			Time.timeScale = instance.timeSpeed * (1 - pause.GetHashCode());
		}

		public void Quit ()
		{
			Application.Quit();
		}

		void OnApplicationQuit ()
		{
			isQuittingGame = true;
			SaveAndLoadManager.Save ();
		}

		public static void Log (object obj)
		{
			print(obj);
		}

		public static void DestroyImmediate (Object obj)
		{
			Object.DestroyImmediate(obj);
		}
		
#if UNITY_EDITOR
		public static void DestroyOnNextEditorUpdate (Object obj, Action action = null)
		{
			EditorApplication.update += () => { if (obj == null) return; DestroyObject (obj, action); };
		}

		static void DestroyObject (Object obj, Action action = null)
		{
			if (obj == null)
				return;
			EditorApplication.update -= () => { DestroyObject (obj, action); };
			DestroyImmediate(obj);
			if (action != null)
				action ();
		}
#endif
		
		// public static bool ModifierExistsAndIsActive (string name)
		// {
		// 	GameModifier gameModifier;
		// 	if (gameModifierDict.TryGetValue(name, out gameModifier))
		// 		return gameModifier.isActive;
		// 	else
		// 		return false;
		// }

		// public static bool ModifierIsActive (string name)
		// {
		// 	return gameModifierDict[name].isActive;
		// }

		// public static bool ModifierExists (string name)
		// {
		// 	return gameModifierDict.ContainsKey(name);
		// }

		// [Serializable]
		// public class GameModifier
		// {
		// 	public string name;
		// 	public bool isActive;
		// }
	}
}