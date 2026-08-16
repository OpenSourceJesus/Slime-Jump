using System;
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
#endif

namespace SlimeJump
{
	public class BuildManager : SingletonMonoBehaviour<BuildManager>
	{
#if UNITY_EDITOR
		public BuildAction[] buildActions;
		public Text versionNumberText;
		static BuildPlayerOptions buildOptions;
#endif
		public DevelopmentStage developmentStage;
		public int versionIndex;
		public string versionNumberPrefix;
		public bool clearDataOnFirstStartup;
		public static bool isBuilding;
		public static bool IsFirstStartup
		{
			get
			{
				return PlayerPrefsExtensions.GetBool("1st startup", true);
			}
			set
			{
				PlayerPrefsExtensions.SetBool ("1st startup", value);
			}
		}
		
#if UNITY_EDITOR
		public static string[] GetScenePathsInBuild ()
		{
			List<string> scenePathsInBuild = new List<string>();
			for (int i = 0; i < EditorBuildSettings.scenes.Length; i ++)
			{
				EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
				if (scene.enabled)
					scenePathsInBuild.Add(scene.path);
			}
			return scenePathsInBuild.ToArray();
		}

		public static string[] GetAllScenePaths ()
		{
			List<string> scenePaths = new List<string>();
			for (int i = 0; i < EditorBuildSettings.scenes.Length; i ++)
				scenePaths.Add(EditorBuildSettings.scenes[i].path);
			return scenePaths.ToArray();
		}
		
		[MenuItem("Build/Make Builds %&b")]
		public static void Build ()
		{
			Instance._Build ();
		}

		public void _Build ()
		{
			isBuilding = true;
			Instance.versionIndex ++;
			for (int i = 0; i < buildActions.Length; i ++)
			{
				BuildAction buildAction = buildActions[i];
				if (buildAction.enabled)
				{
					EditorPrefs.SetInt("Current build action index", i);
					buildAction.Do ();
				}
			}
			EditorPrefs.SetInt("Current build action index", -1);
			isBuilding = false;
		}

		// [UnityEditor.Callbacks.DidReloadScripts]
		public static void OnScriptsReload ()
		{
			int currentBuildActionIndex = EditorPrefs.GetInt("Current build action index", -1);
			if (currentBuildActionIndex != -1)
			{
				for (int i = currentBuildActionIndex; i < Instance.buildActions.Length; i ++)
				{
					BuildAction buildAction = instance.buildActions[i];
					if (buildAction.enabled)
					{
						EditorPrefs.SetInt("Current build action index", i);
						buildAction.Do ();
					}
				}
			}
			EditorPrefs.SetInt("Current build action index", -1);
		}
		
		[Serializable]
		public class BuildAction
		{
			public string name;
			public bool enabled;
			public BuildTarget target;
			public string locationPath;
			public BuildOptions[] options;
			public bool removeExtraDirs;
			public bool makeZip;
			public string directoryToZip;
			public string zipLocationPath;
			public bool clearDataOnFirstStartup;
			public bool isDemo;
			
			public void Do ()
			{
				Resources.Load<GameManager>("Game Manager").isDemo = isDemo;
				if (target == BuildTarget.StandaloneOSX || target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
					PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
				else
					PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
				Instance.clearDataOnFirstStartup = clearDataOnFirstStartup;
				if (instance.versionNumberText != null)
					instance.versionNumberText.text = BuildManager.Instance.versionNumberPrefix + DateTime.Now.Date.ToString("MMdd");
				EditorSceneManager.MarkAllScenesDirty();
				EditorSceneManager.SaveOpenScenes();
				buildOptions = new BuildPlayerOptions();
				buildOptions.scenes = GetScenePathsInBuild();
				buildOptions.target = target;
				buildOptions.locationPathName = locationPath;
				foreach (BuildOptions option in options)
					buildOptions.options |= option;
				BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
				if (report.summary.result == BuildResult.Cancelled)
				{
					EditorPrefs.SetInt("Current build action index", -1);
					return;
				}
				AssetDatabase.Refresh();
				if (removeExtraDirs)
				{
					string dirPathPrefix = locationPath.Remove(locationPath.LastIndexOf("/") + 1) + Application.productName;
					string dirPath = dirPathPrefix + "_BackUpThisFolder_ButDontShipItWithYourGame";
					if (Directory.Exists(dirPath))
						Directory.Delete(dirPath, true);
					dirPath = dirPathPrefix + "_BurstDebugInformation_DoNotShip";
					if (Directory.Exists(dirPath))
						Directory.Delete(dirPath, true);
				}
				if (makeZip)
				{
					if (File.Exists(zipLocationPath))
						File.Delete(zipLocationPath);
					SystemExtensions.CompressDirectory (directoryToZip, zipLocationPath);
				}
			}
		}
#endif

		public enum DevelopmentStage
		{
			Alpha,
			Beta,
			Release
		}
	}
}
