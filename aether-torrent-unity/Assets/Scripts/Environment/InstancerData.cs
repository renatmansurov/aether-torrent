using UnityEngine;

namespace Environment
{
	[CreateAssetMenu(menuName = "Skydive/Instancer Data")]
	public class InstancerData : ScriptableObject
	{
		public GrassPatch[] grassPatches;
	}
}