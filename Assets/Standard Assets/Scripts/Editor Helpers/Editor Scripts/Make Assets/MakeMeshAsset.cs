#if UNITY_EDITOR
using UnityEngine;

namespace SlimeJump
{
	public class MakeMeshAsset : MakeAsset
	{
		public MeshFilter meshFilter;
        public bool useSharedMesh;

		public override void Do ()
		{
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            Mesh mesh;
            if (useSharedMesh)
                mesh = meshFilter.sharedMesh;
			else
				mesh = meshFilter.mesh;
			mesh.Optimize();
			_Do (mesh, assetPath);
		}
	}
}
#else
namespace SlimeJump
{
	public class MakeMeshAsset : EditorScript
	{
	}
}
#endif