using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shaders.Editor
{
	/// <summary>
	/// Editor script for the SimpleLit material inspector.
	/// </summary>
	public static class ToonLitGUI
	{
		/// <summary>
		/// Options for specular source.
		/// </summary>
		public enum SpecularSource
		{
			/// <summary>
			/// Use this to use specular texture and color.
			/// </summary>
			SpecularTextureAndColor,

			/// <summary>
			/// Use this when not using specular.
			/// </summary>
			NoSpecular
		}

		/// <summary>
		/// Options to select the texture channel where the smoothness value is stored.
		/// </summary>
		public enum SmoothnessMapChannel
		{
			/// <summary>
			/// Use this when smoothness is stored in the alpha channel of the Specular Map.
			/// </summary>
			SpecularAlpha,

			/// <summary>
			/// Use this when smoothness is stored in the alpha channel of the Albedo Map.
			/// </summary>
			AlbedoAlpha,
		}

		/// <summary>
		/// Container for the text and tooltips used to display the shader.
		/// </summary>
		public static class Styles
		{
			/// <summary>
			/// The text and tooltip for the specular map GUI.
			/// </summary>
			public static GUIContent specularMapText = EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");
		}

		/// <summary>
		/// Container for the properties used in the <c>SimpleLitGUI</c> editor script.
		/// </summary>
		public struct ToonLitProperties
		{
			public MaterialProperty specColor;
			public MaterialProperty specGlossMap;
			public MaterialProperty specHighlights;
			public MaterialProperty smoothnessMapChannel;
			public MaterialProperty smoothness;
			public MaterialProperty steps;
			public MaterialProperty slopes;
			public MaterialProperty wrap;
			public MaterialProperty specSteps;
			public MaterialProperty specSlopes;
			public MaterialProperty clouds;

			public MaterialProperty depthThreshold;
			public MaterialProperty normalThreshold;
			public MaterialProperty normalEdgeBias;
			public MaterialProperty depthEdgeStrength;
			public MaterialProperty normalEdgeStrength;
			public MaterialProperty outlineColor;
			public MaterialProperty outline;

			public ToonLitProperties(MaterialProperty[] properties)
			{
				// Surface Input Props
				specColor = BaseShaderGUI.FindProperty("_SpecColor", properties);
				specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
				specHighlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
				smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessSource", properties, false);
				smoothness = BaseShaderGUI.FindProperty("_Smoothness", properties, false);
				steps = BaseShaderGUI.FindProperty("_Steps", properties, false);
				slopes = BaseShaderGUI.FindProperty("_Slopes", properties, false);
				wrap = BaseShaderGUI.FindProperty("_Wrap", properties, false);
				specSteps = BaseShaderGUI.FindProperty("_SpecSteps", properties, false);
				specSlopes = BaseShaderGUI.FindProperty("_SpecSlopes", properties, false);
				clouds = BaseShaderGUI.FindProperty("_Clouds", properties, false);

				// Surface Input Props
				depthThreshold = BaseShaderGUI.FindProperty("_DepthThreshold", properties);
				normalThreshold = BaseShaderGUI.FindProperty("_NormalThreshold", properties, false);
				normalEdgeBias = BaseShaderGUI.FindProperty("_NormalEdgeBias", properties, false);
				depthEdgeStrength = BaseShaderGUI.FindProperty("_DepthEdgeStrength", properties, false);
				normalEdgeStrength = BaseShaderGUI.FindProperty("_NormalEdgeStrength", properties, false);
				outlineColor = BaseShaderGUI.FindProperty("_OutlineColor", properties, false);
				outline = BaseShaderGUI.FindProperty("_Outline", properties, false);
			}
		}

		public static void Inputs(ToonLitProperties properties, MaterialEditor materialEditor, Material material)
		{
			DoToonShadingArea(properties, materialEditor, material);
			DoSpecularArea(properties, materialEditor, material);
			DoOutlineArea(properties, materialEditor, material);
		}

		public static void Advanced(ToonLitProperties properties)
		{
			SpecularSource specularSource = (SpecularSource)properties.specHighlights.floatValue;
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = properties.specHighlights.hasMixedValue;
			bool enabled = EditorGUILayout.Toggle(LitGUI.Styles.highlightsText, specularSource == SpecularSource.SpecularTextureAndColor);
			if (EditorGUI.EndChangeCheck())
				properties.specHighlights.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;
			EditorGUI.showMixedValue = false;
		}

		public static void DoSpecularArea(ToonLitProperties properties, MaterialEditor materialEditor, Material material)
		{
			SpecularSource specSource = (SpecularSource)properties.specHighlights.floatValue;
			EditorGUI.BeginDisabledGroup(specSource == SpecularSource.NoSpecular);
			BaseShaderGUI.TextureColorProps(materialEditor, Styles.specularMapText, properties.specGlossMap, properties.specColor, true);
			LitGUI.DoSmoothness(materialEditor, material, properties.smoothness, properties.smoothnessMapChannel, LitGUI.Styles.specularSmoothnessChannelNames);
			EditorGUI.EndDisabledGroup();
		}

		public static void DoToonShadingArea(ToonLitProperties properties, MaterialEditor materialEditor, Material material)
		{
			materialEditor.ShaderProperty(properties.steps, "Steps");
			materialEditor.ShaderProperty(properties.slopes, "Slopes");
			materialEditor.ShaderProperty(properties.wrap, "Light Wrap");
			materialEditor.ShaderProperty(properties.specSteps, "Specular Steps");
			materialEditor.ShaderProperty(properties.specSlopes, "Specular Slopes");
			materialEditor.ShaderProperty(properties.clouds, "Clouds Shadow");
		}

		public static void DoOutlineArea(ToonLitProperties properties, MaterialEditor materialEditor, Material material)
		{
			materialEditor.ShaderProperty(properties.outline, "Outline On");
			materialEditor.ShaderProperty(properties.depthThreshold, "Depth Threshold");
			materialEditor.ShaderProperty(properties.normalThreshold, "Normal Threshold");
			materialEditor.ShaderProperty(properties.normalEdgeBias, "Normal Edge Bias");
			materialEditor.ShaderProperty(properties.depthEdgeStrength, "Depth Edge Strength");
			materialEditor.ShaderProperty(properties.normalEdgeStrength, "Normal Edge Strength");
			materialEditor.ShaderProperty(properties.outlineColor, "Outline Color");
		}

		public static void SetMaterialKeywords(Material material)
		{
			UpdateMaterialSpecularSource(material);
		}

		private static void UpdateMaterialSpecularSource(Material material)
		{
			if (material.GetFloat("_Clouds") > 0.5)
			{
				CoreUtils.SetKeyword(material, "_CLOUDS", true);
			}
			else
			{
				CoreUtils.SetKeyword(material, "_CLOUDS", false);
			}

			if (material.GetFloat("_Outline") > 0.5)
			{
				CoreUtils.SetKeyword(material, "_OUTLINE", true);
			}
			else
			{
				CoreUtils.SetKeyword(material, "_OUTLINE", false);
			}

			var opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat("_Surface") ==
			              BaseShaderGUI.SurfaceType.Opaque);
			SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecularHighlights");
			if (specSource == SpecularSource.NoSpecular)
			{
				CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
				CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
				CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
			}
			else
			{
				var smoothnessSource = (SmoothnessMapChannel)material.GetFloat("_SmoothnessSource");
				bool hasMap = material.GetTexture("_SpecGlossMap");
				CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasMap);
				CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasMap);
				if (opaque)
					CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", smoothnessSource == SmoothnessMapChannel.AlbedoAlpha);
				else
					CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);

				string color;
				if (smoothnessSource != SmoothnessMapChannel.AlbedoAlpha || !opaque)
					color = "_SpecColor";
				else
					color = "_BaseColor";

				var col = material.GetColor(color);
				float smoothness = material.GetFloat("_Smoothness");
				if (smoothness != col.a)
				{
					col.a = smoothness;
					material.SetColor(color, col);
				}
			}
		}
	}
}