#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SlimeJump
{
	public class ChangeButtonToUIButton : EditorScript
	{
		public override void Do ()
		{
			_Do (GetComponent<Button>());
		}

		static void _Do (Button button)
		{
			GameObject go = button.gameObject;
			ColorBlock colors = button.colors;
			Button.ButtonClickedEvent onClick = button.onClick;
			GameManager.DestroyOnNextEditorUpdate (button, () => { 
				UIButton uiButton = go.AddComponent<UIButton>();
				uiButton.colors = colors;
				uiButton.onClick = onClick; });
		}

		[MenuItem("Tools/Change selected Buttons to UIButton")]
		static void DoToSelected ()
		{
			for (int i = 0; i < Selection.transforms.Length; i ++)
				_Do (Selection.transforms[i].GetComponent<Button>());
		}
	}
}
#else
namespace SlimeJump
{
	public class ChangeButtonToUIButton : EditorScript
	{
	}
}
#endif