using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace SlimeJump
{
	public class AchievementButton : UIButton
	{
		public Image imageWhenPressed;
		public Achievement achievement;

		public override void DoUpdate ()
		{
			base.DoUpdate ();
			if (!IsPressed() && image.color.a > 0)
			{
				if (achievement.Achieved)
				{
					image.color = image.color.SetAlpha(1);
					imageWhenPressed.color = imageWhenPressed.color.SetAlpha(1);
				}
				else
				{
					image.color = image.color.SetAlpha(0.5f);
					imageWhenPressed.color = imageWhenPressed.color.SetAlpha(0.5f);
				}
			}
		}

		public void OnPressed ()
		{
			AchievementsMenu.instance.previewImage.sprite = achievement.icon;
			AchievementsMenu.instance.previewImage.enabled = true;
			AchievementsMenu.instance.descriptionText.text = achievement.description;
		}
	}
}