#if UNITY_EDITOR
namespace SlimeJump
{
	public class ClearData : EditorScript
	{
		public override void Do ()
		{
#if !UNITY_WEBGL
			SaveAndLoadManager.Init ();
			SaveAndLoadManager.DeleteAll ();
#endif
		}
	}
}
#else
namespace SlimeJump
{
	public class ClearData : EditorScript
	{
	}
}
#endif