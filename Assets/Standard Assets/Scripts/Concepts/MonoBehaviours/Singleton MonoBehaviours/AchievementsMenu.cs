using TMPro;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SlimeJump
{
	public class AchievementsMenu : SingletonMonoBehaviour<AchievementsMenu>
	{
		public Image previewImage;
		public TMP_Text descriptionText;
#if UNITY_EDITOR
		public bool update;
		public Transform achievementsParent;
		public _Selectable scrollbarSelectable;
		public _Selectable backButton;
		public _Selectable cosmeticsButton;

		void OnValidate ()
		{
			if (!update)
				return;
			update = false;
			_Selectable[] selectables = achievementsParent.GetComponentsInChildren<_Selectable>();
			List<_Selectable> scrollbarCanNavigateTo = new List<_Selectable>(new _Selectable[] { cosmeticsButton });
			for (int i = 0; i < selectables.Length; i ++)
			{
				_Selectable selectable = selectables[i];
				selectable.canNavigateTo = new _Selectable[0];
				int[] selectableIdxs = new int[0];
				if (selectable.rectTrs.GetSiblingIndex() % 3 == 0)
					selectableIdxs = new int[] { -3, -2, 1, 3, 4 };
				else if (selectable.rectTrs.GetSiblingIndex() % 3 == 1)
					selectableIdxs = new int[] { -4, -3, -2, -1, 1, 2, 3, 4 };
				else
				{
					selectableIdxs = new int[] { -4, -3, -1, 2, 3 };
					selectable.canNavigateTo = new _Selectable[] { scrollbarSelectable };
					scrollbarCanNavigateTo.Add(selectable);
				}
				if (i < 3)
					selectable.canNavigateTo = selectable.canNavigateTo.AddRange(new _Selectable[] { cosmeticsButton });
				for (int i2 = 0; i2 < selectableIdxs.Length; i2 ++)
				{
					int selectableIdx = selectable.rectTrs.GetSiblingIndex() + selectableIdxs[i2];
					if (selectableIdx > -1 && selectableIdx < selectables.Length)
						selectable.canNavigateTo = selectable.canNavigateTo.Add(selectables[selectableIdx]);
				}
				selectable.rectTrs.GetChild(0).GetComponent<Image>().sprite = selectable.GetComponent<Image>().sprite;
			}
			backButton.canNavigateTo = new _Selectable[] { cosmeticsButton, selectables[0] };
			cosmeticsButton.canNavigateTo = new _Selectable[] { backButton, selectables[0], selectables[1], selectables[2] };
			scrollbarSelectable.canNavigateTo = scrollbarCanNavigateTo.ToArray();
		}
#endif
	}
}