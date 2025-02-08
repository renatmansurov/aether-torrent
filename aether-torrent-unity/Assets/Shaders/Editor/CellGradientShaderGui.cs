using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shaders.Editor
{
	public class CellGradientShaderGui : ShaderGUI
	{
		//Gradient Properties
		private static readonly int C0 = Shader.PropertyToID("_c0");
		private static readonly int C1 = Shader.PropertyToID("_c1");
		private static readonly int C2 = Shader.PropertyToID("_c2");
		private static readonly int C3 = Shader.PropertyToID("_c3");
		private static readonly int PointsId = Shader.PropertyToID("_points");
		private static readonly int SlopesId = Shader.PropertyToID("_slopes");
		private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
		private static readonly int MainTexScaleOffsetId = Shader.PropertyToID("_MainTexScaleOffset");
		private static readonly int TexBlendId = Shader.PropertyToID("_TexBlend");
		private static readonly int FXPropertiesId = Shader.PropertyToID("_FXProperties");
		private static readonly int FxTexId = Shader.PropertyToID("_FXTex");
		private static readonly int FXScrollSpeedId = Shader.PropertyToID("_FxScrollSpeed");
		private static readonly int FXScaleId = Shader.PropertyToID("_FXScale");
		private static readonly int XhTexId = Shader.PropertyToID("_XhTex");
		private static readonly int XhNoiseTexId = Shader.PropertyToID("_XhNoiseTex");
		private static readonly int XhMinmaxNoiseId = Shader.PropertyToID("_XhMinMaxNoise");
		private static readonly int XhScaleId = Shader.PropertyToID("_XhScale");
		private static readonly int XhColorId = Shader.PropertyToID("_XhColor");
		private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
		private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
		private static readonly int OutlineScaleId = Shader.PropertyToID("_OutlineScale");
		private static readonly int RimShadowId = Shader.PropertyToID("_RimShadow");
		private const float SlopesPower = 7;
		private bool fxEnabled;
		private bool crossHatchEnabled;
		private bool fresnelEnabled;

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)

		{
			var t = materialEditor.target as Material;
			if (t == null)
			{
				return;
			}

			//Read Gradient Properties
			var slopes = t.GetVector(SlopesId);
			var slope0 = Mathf.Pow(slopes.x, 1f / SlopesPower);
			var slope1 = Mathf.Pow(slopes.y, 1f / SlopesPower);
			var slope2 = Mathf.Pow(slopes.z, 1f / SlopesPower);

			var mainTex = t.GetTexture(MainTexId);
			var mainScaleOffset = t.GetVector(MainTexScaleOffsetId);
			var texBlend = t.GetFloat(TexBlendId);

			//Read FX Properties
			var fxTex = t.GetTexture(FxTexId);
			var fxScrollSpeed = t.GetVector(FXScrollSpeedId);
			var fxScale = t.GetVector(FXScaleId);

			var fxProperties = t.GetVector(FXPropertiesId);
			var fxFade = fxProperties.x;
			var fxBlend = fxProperties.y;
			var fxZero = fxProperties.z;
			var fxOne = fxProperties.w;

			var rimShadow = t.GetVector(RimShadowId);
			var rimShadeMult = rimShadow.x;
			var rimXhMult = rimShadow.y;
			var rimMin = rimShadow.z;
			var rimMax = rimShadow.w;

			//Read Cross Hatch Properties
			var xhTex = t.GetTexture(XhTexId);
			var xhNoiseTex = t.GetTexture(XhNoiseTexId);
			var xhMinMaxNoise = t.GetVector(XhMinmaxNoiseId);
			var xhMin = xhMinMaxNoise.x;
			var xhMax = xhMinMaxNoise.y;
			var xhNoise = xhMinMaxNoise.z;
			var xhSNoise = xhMinMaxNoise.w;

			var xhScale = t.GetVector(XhScaleId);
			var xhColor = t.GetVector(XhColorId);

			var outlineColor = t.GetColor(OutlineColorId);
			var outlineWidth = t.GetFloat(OutlineWidthId);
			var outlineScale = t.GetFloat(OutlineScaleId);


			if (t.mainTexture)
			{
				t.EnableKeyword("_MAIN_TEXTURE");
			}
			else
			{
				t.DisableKeyword("_MAIN_TEXTURE");
			}

			fxEnabled = t.IsKeywordEnabled("_FX");
			crossHatchEnabled = t.IsKeywordEnabled("_CROSS_HATCH");
			fresnelEnabled = t.IsKeywordEnabled("_FRESNEL");

			DrawGradientControl(materialEditor);

			var styleCentered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter };
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("Gradient Segments Contrast", GUILayout.MinWidth(50));
			var newSlope0 = EditorGUILayout.Slider("Slope 0", slope0 - 1, 0, 1) + 1;
			var newSlope1 = EditorGUILayout.Slider("Slope 1", slope1 - 1, 0, 1) + 1;
			var newSlope2 = EditorGUILayout.Slider("Slope 2", slope2 - 1, 0, 1) + 1;
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(t, "Shading Slopes Change");
				var newSlopes = new Vector4(Mathf.Pow(newSlope0, SlopesPower), Mathf.Pow(newSlope1, SlopesPower), Mathf.Pow(newSlope2, SlopesPower), 0);
				t.SetVector(SlopesId, newSlopes);
			}

			EditorGUILayout.Separator();
			EditorGUI.BeginChangeCheck();
			//DRAW MAIN TEXTURE CONTROLS
			EditorGUILayout.Separator();
			EditorGUI.BeginChangeCheck();
			if (mainTex)
			{
				EditorGUILayout.BeginHorizontal();
				var newMainTex = DrawTextureField("MainTex", mainTex);
				mainScaleOffset = DrawTextureScaleOffset(t.mainTexture, mainScaleOffset);
				GUILayout.EndHorizontal();
				EditorGUILayout.Separator();
				var texBlendLabel = "Medium";
				if (texBlend > 0.25)
				{
					texBlendLabel = "Additive";
				}

				if (texBlend < 0.75)
				{
					texBlendLabel = "Multiplicative";
				}

				if (texBlend >= 0.25 && texBlend <= 0.75)
				{
					texBlendLabel = "Medium";
				}

				EditorGUILayout.LabelField("Texture Blend:");
				texBlend = EditorGUILayout.Slider(texBlendLabel, texBlend, 0, 1);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Main Texture Change");
					t.SetVector(MainTexScaleOffsetId, mainScaleOffset);
					t.SetFloat(TexBlendId, texBlend);
					t.SetTexture(MainTexId, newMainTex);
				}
			}
			else
			{
				var newMainTex = (Texture2D)EditorGUILayout.ObjectField(mainTex, typeof(Texture2D), false);
				GUILayout.Label("[No Texture]", styleCentered);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Main Texture Switch");
					t.SetTexture(MainTexId, newMainTex);
				}
			}

			EditorGUILayout.Separator();
			var fresnelRect = EditorGUILayout.BeginVertical();
			EditorGUILayout.Space(0);
			if (GUILayout.Button("Rim Shadow"))
			{
				fresnelEnabled = !fresnelEnabled;
			}

			if (fresnelEnabled)
			{
				t.EnableKeyword("_FRESNEL");
				EditorGUI.BeginChangeCheck();
				rimShadeMult = EditorGUILayout.Slider("Affect Shading Mult", rimShadeMult, 0, 2f);
				rimXhMult = EditorGUILayout.Slider("Affect Cross Hatch", rimXhMult, 0, 2f);
				GUILayout.Label("Remap Rim Shadow Shading", styleCentered);
				var newFresnelMinMax = DrawVector2Field(new Vector2(rimMin, rimMax));
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Clip Changed");
					rimMin = Mathf.Clamp(newFresnelMinMax.x, 0, rimMax);
					rimMax = Mathf.Clamp(newFresnelMinMax.y, rimMin, 1);
				}

				EditorGUILayout.MinMaxSlider("", ref rimMin, ref rimMax, 0, 1);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Rim Settings Change");
					var newRimMinMax = new Vector4(rimShadeMult, rimXhMult, rimMin, rimMax);
					t.SetVector(RimShadowId, newRimMinMax);
				}

				DrawPanel(fresnelRect);
			}
			else
			{
				t.DisableKeyword("_FRESNEL");
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();
			//Draw Cross Hatch Controls
			var xhRect = EditorGUILayout.BeginVertical();
			if (GUILayout.Button("Cross Hatch"))
			{
				crossHatchEnabled = !crossHatchEnabled;
			}

			if (crossHatchEnabled)
			{
				t.EnableKeyword("_CROSS_HATCH");
				EditorGUI.BeginChangeCheck();
				xhColor = EditorGUILayout.ColorField("Cross Hatch Color", xhColor);
				GUILayout.BeginHorizontal();
				xhTex = DrawTextureField("Strokes Tex", xhTex);
				xhNoiseTex = DrawTextureField("Noise Tex", xhNoiseTex);
				GUILayout.EndHorizontal();
				xhScale = DrawTextureScaleOffset(xhTex, xhScale, "Strokes Scale", "Noise Scale", xhNoiseTex);
				if (xhNoiseTex)
				{
					xhNoise = EditorGUILayout.Slider("Cross Hatch Noise", xhNoise, 0, 1);
					xhSNoise = EditorGUILayout.Slider("Shading Noise", xhSNoise, 0, 1);
				}

				GUILayout.Label("Remap Cross Hatch Shading", styleCentered);
				var newMinMax = DrawVector2Field(new Vector2(xhMinMaxNoise.x, xhMinMaxNoise.y));
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Clip Changed");
					xhMin = Mathf.Clamp(newMinMax.x, 0, fxOne);
					xhMax = Mathf.Clamp(newMinMax.y, fxZero, 1);
				}

				EditorGUILayout.MinMaxSlider("", ref xhMin, ref xhMax, 0, 1);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "FX Settings Change");
					var newMinMaxNoise = new Vector4(xhMin, xhMax, xhNoise, xhSNoise);
					t.SetTexture(XhTexId, xhTex);
					t.SetTexture(XhNoiseTexId, xhNoiseTex);
					t.SetVector(XhMinmaxNoiseId, newMinMaxNoise);
					t.SetVector(XhScaleId, xhScale);
					t.SetColor(XhColorId, xhColor);
				}

				DrawPanel(xhRect, true);
			}
			else
			{
				t.DisableKeyword("_CROSS_HATCH");
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();
			var fxRect = EditorGUILayout.BeginVertical();
			//Draw FX Controls
			if (GUILayout.Button("FX"))
			{
				fxEnabled = !fxEnabled;
			}

			if (fxEnabled)
			{
				t.EnableKeyword("_FX");
				EditorGUI.BeginChangeCheck();
				fxFade = EditorGUILayout.Slider("FX Fade", fxFade, 0, 1);
				GUILayout.BeginHorizontal();
				fxTex = DrawTextureField("FX Texture", fxTex);
				EditorGUI.BeginDisabledGroup(fxFade == 0 || !fxTex);
				fxScale = DrawTextureScaleOffset(fxTex, fxScale, "Scale 1", "Scale 2");
				GUILayout.EndHorizontal();
				GUILayout.BeginVertical();
				var blendLabel = "Medium";
				if (fxBlend > 0.25)
				{
					blendLabel = "Maximum";
				}

				if (fxBlend < 0.75)
				{
					blendLabel = "Multiplicative";
				}

				if (fxBlend >= 0.25 && fxBlend <= 0.75)
				{
					blendLabel = "Medium";
				}

				EditorGUILayout.Separator();
				EditorGUILayout.LabelField("FX Layers Blend:");
				fxBlend = EditorGUILayout.Slider(blendLabel, fxBlend, 0, 1);
				GUILayout.Label("Clip FX Texture Values", styleCentered);
				var newClip = DrawVector2Field(new Vector2(fxZero, fxOne));
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "Clip Changed");
					fxZero = Mathf.Clamp(newClip.x, 0, fxOne);
					fxOne = Mathf.Clamp(newClip.y, fxZero, 1);
				}

				EditorGUILayout.MinMaxSlider("", ref fxZero, ref fxOne, 0, 1);
				EditorGUILayout.LabelField("FX Texture Scroll");
				var newFxScrollSpeed = DrawVector4Field("Layer 1", "Layer 2", fxScrollSpeed);
				EditorGUILayout.Separator();
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(t, "FX Settings Change");
					var newFXProperties = new Vector4(fxFade, fxBlend, 0, 1);
					t.SetVector(FXPropertiesId, newFXProperties);
					t.SetTexture(FxTexId, fxTex);
					t.SetVector(FXScaleId, fxScale);
					t.SetVector(FXScrollSpeedId, newFxScrollSpeed);
				}

				EditorGUI.EndDisabledGroup();
				GUILayout.EndHorizontal();
				DrawPanel(fxRect);
			}
			else
			{
				t.DisableKeyword("_FX");
			}

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(t, "FX Switch");
				var newFXProperties = new Vector4(fxFade, fxBlend, fxZero, fxOne);
				t.SetVector(FXPropertiesId, newFXProperties);
			}

			EditorGUILayout.Separator();

			EditorGUI.BeginChangeCheck();
			var newOutlineColor = EditorGUILayout.ColorField("Outline Color", outlineColor);
			var newOutlineWidth = EditorGUILayout.FloatField("Width", outlineWidth);
			var newOutlineScale = EditorGUILayout.FloatField("Scale", outlineScale);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(t, "FX Settings Change");
				t.SetColor(OutlineColorId, newOutlineColor);
				t.SetFloat(OutlineWidthId, newOutlineWidth);
				t.SetFloat(OutlineScaleId, newOutlineScale);
			}

			EditorGUILayout.EndVertical();
			//Render the shader properties using the default GUI
			base.OnGUI(materialEditor, materialProperties);
		}

		private static void DrawGradientControl(UnityEditor.Editor materialEditor)
		{
			var t = materialEditor.target as Material;
			if (t == null)
			{
				return;
			}

			var points = t.GetVector(PointsId);
			var shadingGradient = new Gradient();
			var colorKeys = new GradientColorKey[4];
			colorKeys[0].color = t.GetColor(C0);
			colorKeys[0].time = points.x;
			colorKeys[1].color = t.GetColor(C1);
			colorKeys[1].time = points.y;
			colorKeys[2].color = t.GetColor(C2);
			colorKeys[2].time = points.z;
			colorKeys[3].color = t.GetColor(C3);
			colorKeys[3].time = points.w;
			var alphaKeys = Array.Empty<GradientAlphaKey>();
			shadingGradient.SetKeys(colorKeys, alphaKeys);
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Shading Gradient", GUILayout.MinWidth(50));
			GUILayout.EndHorizontal();
			var newShadingGradient = EditorGUILayout.GradientField(shadingGradient, GUILayout.MinWidth(100));
			if (!EditorGUI.EndChangeCheck())
			{
				return;
			}

			Undo.RecordObject(t, "Shading Gradient Change");
			if (newShadingGradient.colorKeys.Length != 4)
			{
				newShadingGradient.colorKeys = shadingGradient.colorKeys;
			}
			else
			{
				shadingGradient.colorKeys = newShadingGradient.colorKeys;
				points = new Vector4(shadingGradient.colorKeys[0].time, shadingGradient.colorKeys[1].time, shadingGradient.colorKeys[2].time, shadingGradient.colorKeys[3].time);
				t.SetColor(C0, shadingGradient.colorKeys[0].color);
				t.SetColor(C1, shadingGradient.colorKeys[1].color);
				t.SetColor(C2, shadingGradient.colorKeys[2].color);
				t.SetColor(C3, shadingGradient.colorKeys[3].color);
				t.SetVector(PointsId, points);
			}
		}

		private static void DrawPanel(Rect r, bool even = false)
		{
			var alpha = even ? 0.05f : 0.1f;
			r.size += new Vector2(4f, 4f);
			r.center -= new Vector2(2f, 2f);
			var o = new Rect(r);
			o.size += new Vector2(4f, 4f);
			o.center -= new Vector2(2f, 2f);
			EditorGUI.DrawRect(r, new Color(1, 1, 1, alpha / 2f));
			EditorGUI.DrawRect(o, new Color(1, 1, 1, alpha));
		}

		private static Texture2D DrawTextureField(string name, Object texture)
		{
			GUILayout.BeginVertical();
			var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fixedWidth = 70 };
			GUILayout.Label(name, style);
			var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
			GUILayout.EndVertical();
			return result;
		}

		private static Vector4 DrawVector4Field(string name1, string name2, Vector4 vector, bool zwEnabled = true)
		{
			var xy = new Vector2(vector.x, vector.y);
			var zw = new Vector2(vector.z, vector.w);
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			EditorGUILayout.LabelField(name1);
			xy = EditorGUILayout.Vector2Field("", xy);
			EditorGUI.BeginDisabledGroup(!zwEnabled);
			EditorGUILayout.LabelField(name2);
			zw = EditorGUILayout.Vector2Field("", zw);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			return new Vector4(xy.x, xy.y, zw.x, zw.y);
		}

		private static Vector2 DrawVector2Field(Vector2 vector)
		{
			var xy = new Vector2(vector.x, vector.y);
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			xy = EditorGUILayout.Vector2Field("", xy);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			return xy;
		}

		private static Vector4 DrawTextureScaleOffset(bool enabled, Vector4 scaleOffset, string name1 = "Scale", string name2 = "Offset", bool zwEnabled = true)
		{
			EditorGUI.BeginDisabledGroup(!enabled);
			GUILayout.BeginVertical();
			EditorGUILayout.Space(5);
			var result = DrawVector4Field(name1, name2, scaleOffset, zwEnabled);
			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
			return result;
		}
	}
}