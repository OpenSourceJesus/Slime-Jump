using SlimeJump;
using UnityEngine;
using DialogAndStory;

namespace SlimeJump
{
	public class ConversationTrigger : UpdateWhileEnabled
	{
		public Conversation conversation;
		public int maxRetriggers;
		int RetriggerCnt
		{
			get
			{
				return SaveAndLoadManager.GetInt("Retriggered " + _SceneManager.CurrentScene.name + ' ' + name, -1);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Retriggered " + _SceneManager.CurrentScene.name + ' ' + name, value);
			}
		}

		void Awake ()
		{
			if (RetriggerCnt >= maxRetriggers)
				Destroy(gameObject);
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (DialogManager.instance.lastBeganConversation != null)
				DialogManager.instance.EndConversation (DialogManager.instance.lastBeganConversation);
			DialogManager.instance.StartConversation (conversation);
		}

		public void AddToRetriggerCount ()
		{
			RetriggerCnt ++;
			if (RetriggerCnt >= maxRetriggers)
				Destroy(gameObject);
		}
	}
}