#if UNITY_EDITOR
namespace SlimeJump
{
	public class ClearWorldMapData : EditorScript
	{
		public override void Do ()
		{
#if !UNITY_WEBGL
			SaveAndLoadManager.Init ();
#endif
			WorldMap.ExploredCellPositions = new bool[0];
#if !UNITY_WEBGL
			SaveAndLoadManager.Save ();
#endif
		}
	}
}
#else
namespace SlimeJump
{
	public class ClearWorldMapData : EditorScript
	{
	}
}
#endif