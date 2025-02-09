using System;
using UnityEngine;

namespace Environment
{
	[Serializable]
	public class GrassInstancesPatch
	{
		public Matrix4x4[] transforms;
		public Color[] colors;
	}
}