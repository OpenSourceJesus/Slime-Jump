using TMPro;
using UnityEngine;

namespace SlimeJump
{
	public class Sign : MonoBehaviour
	{
		[Multiline(5)]
		public string textStrIfUsingKeyboardAndMouse;
		[Multiline(5)]
		public string textStrIfUsingGamepad;
		[Multiline(5)]
		public string textStrIfUsingPhone;
		public GameObject textPanel;
		public TMP_Text textIfUsingKeyboardAndMouse;
		public TMP_Text textIfUsingGamepad;
		public TMP_Text textIfUsingPhone;

		void OnTriggerEnter2D (Collider2D other)
		{
			textIfUsingGamepad.text = textStrIfUsingGamepad;
			textIfUsingKeyboardAndMouse.text = textStrIfUsingKeyboardAndMouse;
			textIfUsingPhone.text = textStrIfUsingPhone;
			textPanel.SetActive(true);
		}

		void OnTriggerExit2D (Collider2D other)
		{
			textPanel.SetActive(false);
		}
	}
}