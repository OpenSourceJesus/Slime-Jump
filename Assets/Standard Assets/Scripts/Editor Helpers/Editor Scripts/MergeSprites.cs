#if UNITY_EDITOR
// using System.IO;
using Extensions;
using UnityEngine;
using UnityEditor;
// using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

namespace SlimeJump
{
	public class MergeSprites : EditorScript
	{
		// public float pixelsPerUnit;
		// public Camera camera;
		// public Transform cameraTrs;
		public bool stop;
		public CameraScript cameraScript;
		public BoxCollider2D boundsCollider;
		// public SpriteRenderer spriteRendererPrefab;
		public string outputPath;
		// public List<SpriteRenderer> previousSpriteRenderers = new List<SpriteRenderer>();
		EditorCoroutine editorCoroutine;

		public override void OnValidate ()
		{
			base.OnValidate ();
			if (stop)
			{
				stop = false;
				EditorCoroutineUtility.StopCoroutine(editorCoroutine);
				OnDone ();
			}
		}

		public override void Do ()
		{
			enabled = true;
		}

		void OnApplicationQuit ()
		{
			EditorCoroutineUtility.StopCoroutine(editorCoroutine);
		}

		void Update ()
		{
			if (!Application.isPlaying)
				return;
			enabled = false;
			editorCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(UpdateRoutine ());
		}

		void OnDone ()
		{
			FollowWaypoints[] waypointFollowers = FindObjectsOfType<FollowWaypoints>(true);
			for (int i = 0; i < waypointFollowers.Length; i ++)
			{
				FollowWaypoints waypointFollower = waypointFollowers[i];
				waypointFollower.gameObject.SetActive(true);
			}
			Time.timeScale = 1;
		}

		IEnumerator UpdateRoutine ()
		{
			Time.timeScale = 0;
			FollowWaypoints[] waypointFollowers = FindObjectsOfType<FollowWaypoints>();
			for (int i = 0; i < waypointFollowers.Length; i ++)
			{
				FollowWaypoints waypointFollower = waypointFollowers[i];
				// waypointFollower.gameObject.SetActive(false);
			}
			List<Collider2D> colliders = new List<Collider2D>(FindObjectsOfType<Collider2D>());
			colliders.Remove((Collider2D) boundsCollider);
			List<Rect> rects = new List<Rect>();
			for (int i = 0; i < colliders.Count; i ++)
			{
				Collider2D collider = colliders[i];
				Rect rect = collider.bounds.ToRect();
				// if (boundsCollider.bounds.Contains(rect.center) && ((LayerMask) camera.cullingMask).Contains(collider.gameObject.layer) && collider.GetComponentInParent<FollowWaypoints>() == null)
				if (boundsCollider.bounds.Contains(rect.center) && ((LayerMask) cameraScript.camera.cullingMask).Contains(collider.gameObject.layer) && collider.GetComponentInParent<FollowWaypoints>() == null)
					rects.Add(rect);
			}
			Rect combinedRect = rects.ToArray().Combine();
			// float renderTextureWorldSizeComponents = camera.targetTexture.width / pixelsPerUnit;
			// for (int i = 0; i < previousSpriteRenderers.Count; i ++)
			// {
			// 	SpriteRenderer previousSpriteRenderer = previousSpriteRenderers[i];
			// 	DestroyImmediate(previousSpriteRenderer.gameObject);
			// }
			// previousSpriteRenderers.Clear();
			int _x = 0;
			int _y = 0;
			// Dictionary<Vector2Int, Texture2D> texturesDict = new Dictionary<Vector2Int, Texture2D>();
			// for (float x = combinedRect.xMin; x <= combinedRect.xMax - renderTextureWorldSizeComponents; x += renderTextureWorldSizeComponents)
			cameraScript.HandleViewSize ();
			for (float x = combinedRect.xMin; x < combinedRect.xMax; x += cameraScript.viewSize.x)
			{
				_y = 0;
				for (float y = combinedRect.yMin; y < combinedRect.yMax; y += cameraScript.viewSize.y)
				{
					Rect rect = Rect.MinMaxRect(x, y, x + cameraScript.viewSize.x, y + cameraScript.viewSize.y);
					cameraScript.trs.position = rect.center.SetZ(cameraScript.trs.position.z);
					int lastIndexOfSlash = this.outputPath.LastIndexOf('/');
					string outputPath = this.outputPath.Remove(lastIndexOfSlash + 1) + "World (" + _x + ", " + _y + ").png";
					cameraScript.camera.Render();
					ScreenCapture.CaptureScreenshot(outputPath, 1);
					yield return null;
					// cameraTrs.position = rect.center.SetZ(cameraTrs.position.z);
					// camera.orthographicSize = rect.size.y / 2;
					// camera.Render();
					// SpriteRenderer spriteRenderer = Instantiate(spriteRendererPrefab, rect.center, Quaternion.identity);
					// Texture2D texture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, camera.targetTexture.mipmapCount, true);
					// Graphics.CopyTexture(camera.targetTexture, texture);
					// spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2, pixelsPerUnit);
					// previousSpriteRenderers.Add(spriteRenderer);
					// spriteRenderer.name += "[" + _x + ", " + _y + ']';
					// texturesDict.Add(new Vector2Int(_x, _y), texture);
					// int lastIndexOfSlash = this.outputPath.LastIndexOf('/');
					// // string outputPath = this.outputPath.Remove(lastIndexOfSlash + 1) + "World (" + _x + ", " + _y + ").asset";
					// string outputPath = this.outputPath.Remove(lastIndexOfSlash + 1) + "World (" + _x + ", " + _y + ").png";
					// NativeArray<byte> imageBytes = new NativeArray<byte>(texture.GetRawTextureData(), Allocator.Temp);
					// NativeArray<byte> bytes = ImageConversion.EncodeNativeArrayToPNG(imageBytes, texture.graphicsFormat, (uint) texture.width, (uint) texture.height);
					// File.WriteAllBytes(outputPath, bytes.ToArray());
					// AssetDatabase.DeleteAsset(outputPath);
					// AssetDatabase.CreateAsset(spriteRenderer.sprite, outputPath);
					// byte[] bytes = ImageConversion.EncodeToPNG(spriteRenderer.sprite.texture);
					// string assetsIndicator = "/Assets";
					// outputPath = Application.dataPath.Remove(Application.dataPath.Length - assetsIndicator.Length) + '/' + outputPath.Replace(".asset", ".png");
					// File.WriteAllBytes(outputPath, bytes);
					// // AssetDatabase.Refresh();
					// // Texture2D textureAsset = (Texture2D) AssetDatabase.LoadMainAssetAtPath(outputPath);
					// // textureAsset.SetPixels(texture.GetPixels());
					// // textureAsset.Apply();
					_y ++;
				}
				_x ++;
			}
			print("Done");
			OnDone ();
			// Texture2D outputTexture = new Texture2D(camera.targetTexture.width * _x, camera.targetTexture.height * _y, TextureFormat.RGBA32, camera.targetTexture.mipmapCount, true);
			// foreach (KeyValuePair<Vector2Int, Texture2D> keyValuePair in texturesDict)
			// 	Graphics.CopyTexture(keyValuePair.Value, 0, 0, 0, 0, keyValuePair.Value.width, keyValuePair.Value.height, outputTexture, 0, 0, keyValuePair.Key.x * keyValuePair.Value.width, keyValuePair.Key.y * keyValuePair.Value.height);
			// byte[] outputBytes = ImageConversion.EncodeToPNG(outputTexture);
			// File.WriteAllBytes(outputPath, outputBytes);
		}

		// [MenuItem("Tools/MergeSprites")]
		// static void DoForSelected ()
		// {
		// 	Transform[] selectedTransforms = Selection.transforms;
		// 	for (int i = 0; i < selectedTransforms.Length; i ++)
		// 	{
		// 		Transform selectedTrs = selectedTransforms[i];
				
		// 	}
		// }
	}
}
#else
namespace SlimeJump
{
	public class MergeSprites : EditorScript
	{
	}
}
#endif