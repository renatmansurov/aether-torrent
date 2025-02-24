using System;
using UnityEngine.Rendering;

namespace Render.Volumetric_Light.Runtime
{
	/// <summary>
	/// A volume parameter that holds a VolumetricFogRenderPassEvent value.
	/// </summary>
	[Serializable]
	public sealed class VolumetricFogRenderPassEventParameter : VolumeParameter<VolumetricFogRenderPassEvent>
	{
		/// <summary>
		/// Creates a new VolumetricFogRenderPassEventParameter instance.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="overrideState"></param>
		public VolumetricFogRenderPassEventParameter(VolumetricFogRenderPassEvent value, bool overrideState = false) : base(value, overrideState)
		{
		}
	}
}