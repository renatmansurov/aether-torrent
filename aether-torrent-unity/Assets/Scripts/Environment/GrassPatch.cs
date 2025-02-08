using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Environment
{
	[Serializable]
	public class GrassPatch
	{
		public Matrix4x4[] transforms;
		public Vector4[] colors;
	}
}