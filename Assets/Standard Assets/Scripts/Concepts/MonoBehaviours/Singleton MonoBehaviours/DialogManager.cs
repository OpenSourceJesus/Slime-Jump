using SlimeJump;
using UnityEngine;

namespace DialogAndStory
{
	public class DialogManager : SingletonMonoBehaviour<DialogManager>
	{
		[HideInInspector]
		public Conversation lastBeganConversation;

		public void StartDialog (Dialog dialog)
		{
			lastBeganConversation = dialog.conversation;
			dialog.onBeganEvent.Invoke();
			dialog.IsActive = true;
		}
		
		public void EndDialog (Dialog dialog)
		{
			if (!dialog.isDone)
				dialog.onLeftWhileNotDoneEvent.Invoke();
			dialog.IsActive = false;
		}
		
		public void StartConversation (Conversation conversation)
		{
			lastBeganConversation = conversation;
			conversation.gameObject.SetActive(true);
			conversation.updateRoutine = conversation.StartCoroutine(conversation.UpdateRoutine ());
		}

		public void EndConversation (Conversation conversation)
		{
			EndDialog (conversation.lastBeganDialog);
			// for (int i = 0; i < conversation.dialogs.Length; i ++)
			// {
			// 	Dialog dialog = conversation.dialogs[i];
			// 	EndDialog (dialog);
			// }
			if (conversation.updateRoutine != null)
				conversation.StopCoroutine(conversation.updateRoutine);
			conversation.updateRoutine = null;
			conversation.gameObject.SetActive(false);
		}
	}
}