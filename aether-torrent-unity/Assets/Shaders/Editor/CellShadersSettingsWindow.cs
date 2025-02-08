using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CellShadersSettingsWindow : EditorWindow
{
	private static readonly int DirectionId = Shader.PropertyToID("_LightDirection");
	private static readonly int C0 = Shader.PropertyToID("_c0");
	private static readonly int C1 = Shader.PropertyToID("_c1");
	private static readonly int C2 = Shader.PropertyToID("_c2");
	private static readonly int C3 = Shader.PropertyToID("_c3");
	private static readonly int PointsId = Shader.PropertyToID("_points");
	private static readonly int SlopesId = Shader.PropertyToID("_slopes");
	private static readonly int XhMinmaxNoiseId = Shader.PropertyToID("_XhMinMaxNoise");

	private const float SlopesPower = 7f;
	private const float HandleSize = 0.02f;
	private Shader cellShader;
	private List<Material> cellShaderMaterials = new();
	private List<Renderer> selectedRenderers = new();
	private Quaternion lightAngle = Quaternion.identity;
	private Vector3[] directions;
	private bool anglesAreEqual;
	private bool gradientsAreEqual;
	private bool bigRotateHandle;
	private bool showLightAngle;
	private bool showRotationGizmos;
	private bool showGradient;
	private bool applyGradientToAll;
	private Vector3 key0Pos;
	private Vector3 key1Pos;
	private Vector3 key2Pos;
	private Vector3 key3Pos;
	private Vector4 points;
	private Vector3 slope0Pos;
	private Vector3 slope1Pos;
	private Vector3 slope2Pos;
	private float slope0;
	private float slope1;
	private float slope2;
	private float xMin;
	private float xMax;
	private Vector3 xMinPos;
	private Vector3 xMaxPos;

	[MenuItem("Cell Gradient Shader/Open Gizmos GUI")]
	public static void ShowIsoBrowser()
	{
		EditorWindow window = GetWindow<CellShadersSettingsWindow>();
		window.titleContent = new GUIContent("Cell Gradient Gizmos");
		window.minSize = new Vector2(100, 50);
	}

	private void OnEnable()
	{
		cellShader = Shader.Find("Cell Gradient");
		SceneView.duringSceneGui += OnSceneGui;
		directions = new[]
		{
			Vector3.down,
			Vector3.up,
			Vector3.forward,
			Vector3.left,
			Vector3.back,
			Vector3.right
		};
	}

	public void OnDestroy()
	{
		SceneView.duringSceneGui -= OnSceneGui;
	}

	private void OnSelectionChange()
	{
		cellShaderMaterials.Clear();
		selectedRenderers.Clear();
		if (Selection.count <= 0)
		{
			return;
		}

		foreach (var transform in Selection.transforms)
		{
			selectedRenderers = transform.GetComponentsInChildren<Renderer>().ToList();
			if (selectedRenderers.Count > 0 && TryAddMaterial(selectedRenderers, out var materials))
			{
				cellShaderMaterials.AddRange(materials);
			}
		}

		anglesAreEqual = gradientsAreEqual = (cellShaderMaterials.Count == 1);

		cellShaderMaterials = cellShaderMaterials.Distinct().ToList();
		if (cellShaderMaterials.Count <= 0)
		{
			return;
		}

		points = cellShaderMaterials[0].GetVector(PointsId);
		var listToSort = new[]
		{
			points.x,
			points.y,
			points.z,
			points.w
		};
		Array.Sort(listToSort, (a, b) => a.CompareTo(b));
		var sortedList = listToSort.OrderBy(a => a).ToArray();
		points = new Vector4(sortedList[0], sortedList[1], sortedList[2], sortedList[3]);
		var slopes = cellShaderMaterials[0].GetVector(SlopesId);
		slope0 = Mathf.Pow(slopes.x * 0.007874016f - 0.007874016f, 1f / SlopesPower);
		slope1 = Mathf.Pow(slopes.y * 0.007874016f - 0.007874016f, 1f / SlopesPower);
		slope2 = Mathf.Pow(slopes.z * 0.007874016f - 0.007874016f, 1f / SlopesPower);
		var lightDirection = cellShaderMaterials[0].GetVector(DirectionId);
		lightAngle = Quaternion.LookRotation(lightDirection, Vector3.forward);
		anglesAreEqual = cellShaderMaterials.Count == 1;
	}

	private bool TryAddMaterial(IEnumerable<Renderer> renderers, out Material[] result)
	{
		result = (from renderer in renderers from material in renderer.sharedMaterials where material.shader == cellShader select material).ToArray();
		return result.Length > 0;
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		showLightAngle = GUILayout.Toggle(showLightAngle, "Light Angle");
		if (showLightAngle)
		{
			EditorGUILayout.BeginHorizontal();
			showRotationGizmos = GUILayout.Toggle(showRotationGizmos, "Rotation Gizmos");
			EditorGUI.BeginDisabledGroup(!showRotationGizmos);
			bigRotateHandle = GUILayout.Toggle(bigRotateHandle, "Big Gizmo");
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		showGradient = GUILayout.Toggle(showGradient, "Light Gradient Controls");
		if (showGradient)
		{
			applyGradientToAll = GUILayout.Toggle(applyGradientToAll, "Apply Gradient To All");
		}

		EditorGUILayout.EndHorizontal();
	}

	private void OnSceneGui(SceneView obj)
	{
		if (cellShaderMaterials == null || cellShaderMaterials.Count == 0)
		{
			return;
		}

		if (showLightAngle)
		{
			var lightDirection = DrawLightAngleGui();
			foreach (var cellShaderMaterial in cellShaderMaterials)
			{
				cellShaderMaterial.SetVector(DirectionId, lightDirection);
			}
		}

		if (!showGradient)
		{
			return;
		}

		if (cellShaderMaterials.Count > 1 && !applyGradientToAll)
		{
			return;
		}

		DrawRemapHandles(cellShaderMaterials[0]);
	}

	private Vector3 DrawLightAngleGui()
	{
		Handles.lighting = false;
		var cam = Camera.current;
		var center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1));
		var h = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, 1));
		var v = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 1));
		var size = Mathf.Min(Vector3.Distance(center, h), Vector3.Distance(center, v)) * 0.75f;
		var freeRotateHandleSize = size;
		if (!bigRotateHandle)
		{
			freeRotateHandleSize *= 0.25f;
		}

		Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(1, 1, 0f, 0.85f);
		EditorGUI.BeginChangeCheck();
		var newTarget = Handles.FreeMoveHandle(center - lightAngle * Vector3.forward * size * 1.2f, size * 0.01f, Vector3.one, Handles.DotHandleCap);
		if (EditorGUI.EndChangeCheck())
		{
			anglesAreEqual = true;
			lightAngle = Quaternion.LookRotation(center - newTarget);
		}

		var lightDirection = lightAngle * Vector3.forward;
		Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(1, 1, 0f, 0.85f);
		Handles.DrawLine(center, center - lightDirection * size * 1.2f);
		Handles.DrawWireDisc(center - lightDirection * size * 1.2f, lightDirection, size * 0.05f);
		Handles.DrawWireDisc(center - lightDirection * size, lightDirection, size * 0.15f);
		lightAngle = Quaternion.LookRotation(lightDirection, Vector3.forward);
		if (showRotationGizmos)
		{
			EditorGUI.BeginChangeCheck();
			Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(0, 0, 0.75f, 0.25f);
			lightAngle = Handles.Disc(lightAngle, center, Vector3.forward, size, false, 0.1f);
			Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(0, 0.75f, 0, 0.25f);
			lightAngle = Handles.Disc(lightAngle, center, Vector3.up, size, false, 0.1f);
			Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(0.75f, 0, 0, 0.25f);
			lightAngle = Handles.Disc(lightAngle, center, Vector3.left, size, false, 0.1f);
			Handles.color = !anglesAreEqual ? new Color(0.75f, 0.75f, 0.75f, 0.4f) : new Color(1, 1, .4f, 0.25f);
			lightAngle = Handles.FreeRotateHandle(lightAngle, center, freeRotateHandleSize * 0.8f);
			Handles.color = new Color(1, 1, 1f, 0.1f);
			foreach (var direction in directions)
			{
				if (Handles.Button(center - direction * size * 1.2f, Quaternion.LookRotation(direction, Vector3.forward), size * 0.15f, size * 0.15f, Handles.ArrowHandleCap))
				{
					anglesAreEqual = true;
					lightDirection = direction;
				}
			}
		}

		if (!anglesAreEqual && EditorGUI.EndChangeCheck())
		{
			anglesAreEqual = true;
		}

		return lightDirection;
	}

	private void DrawRemapHandles(Material t)
	{
		var cam = Camera.current;

		var color0 = t.GetColor(C0);
		var color1 = t.GetColor(C1);
		var color2 = t.GetColor(C2);
		var color3 = t.GetColor(C3);
		var crossHatch = cellShaderMaterials[0].GetVector(XhMinmaxNoiseId);

		var center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1));
		var rt = cam.ViewportToWorldPoint(new Vector3(1f, 0.8f, 1));
		var rb = cam.ViewportToWorldPoint(new Vector3(1f, 0, 1));
		var rtt = Vector3.Lerp(rt, center, 0.05f);
		var rtb = Vector3.Lerp(rb, center, 0.05f);
		var rxt = Vector3.Lerp(rt, center, 0.025f);
		var rxb = Vector3.Lerp(rb, center, 0.025f);
		var transform = cam.transform;
		var forward = transform.forward;
		var left = -transform.right * 0.1f;
		key0Pos = Vector3.Lerp(rtt, rtb, points.x);
		key1Pos = Vector3.Lerp(rtt, rtb, points.y);
		key2Pos = Vector3.Lerp(rtt, rtb, points.z);
		key3Pos = Vector3.Lerp(rtt, rtb, points.w);
		var slope0Start = Vector3.Lerp(key2Pos, key3Pos, .5f);
		var slope1Start = Vector3.Lerp(key1Pos, key2Pos, .5f);
		var slope2Start = Vector3.Lerp(key0Pos, key1Pos, .5f);
		var slope0End = slope0Start + left;
		var slope1End = slope1Start + left;
		var slope2End = slope2Start + left;
		slope0Pos = Vector3.Lerp(slope0Start, slope0End, slope0);
		slope1Pos = Vector3.Lerp(slope1Start, slope1End, slope1);
		slope2Pos = Vector3.Lerp(slope2Start, slope2End, slope2);

		xMinPos = Vector3.Lerp(rxt, rxb, crossHatch.x);
		xMaxPos = Vector3.Lerp(rxt, rxb, crossHatch.y);

		EditorGUI.BeginChangeCheck();
		Handles.color = Color.clear;
		var newSlope0Pos = DrawKey(slope0Pos);
		var newSlope1Pos = DrawKey(slope1Pos);
		var newSlope2Pos = DrawKey(slope2Pos);


		if (EditorGUI.EndChangeCheck())
		{
			slope0Pos = GetClosestPoint(slope0Start, slope0End, newSlope0Pos, out var magnitude0);
			slope1Pos = GetClosestPoint(slope1Start, slope1End, newSlope1Pos, out var magnitude1);
			slope2Pos = GetClosestPoint(slope2Start, slope2End, newSlope2Pos, out var magnitude2);
			slope0 = magnitude0;
			slope1 = magnitude1;
			slope2 = magnitude2;
		}

		Handles.color = Color.white;
		Handles.DrawDottedLine(slope0Start, slope0Pos, 2f);
		Handles.DrawDottedLine(slope1Start, slope1Pos, 2f);
		Handles.DrawDottedLine(slope2Start, slope2Pos, 2f);
		Handles.DrawSolidDisc(slope0Pos, forward, HandleSize * 0.5f);
		Handles.DrawSolidDisc(slope1Pos, forward, HandleSize * 0.5f);
		Handles.DrawSolidDisc(slope2Pos, forward, HandleSize * 0.5f);
		Handles.color = gradientsAreEqual ? Color.yellow : Color.gray;
		Handles.DrawLine(rtt, rtb);
		Handles.color = gradientsAreEqual ? Color.white : Color.gray;
		Handles.DrawLine(xMinPos, xMaxPos);
		Handles.DrawSolidDisc(xMinPos, forward, HandleSize * 0.5f);
		Handles.DrawSolidDisc(xMaxPos, forward, HandleSize * 0.5f);
		Handles.color = color0;
		Handles.DrawSolidDisc(key0Pos, forward, HandleSize);
		Handles.color = color1;
		Handles.DrawSolidDisc(key1Pos, forward, HandleSize);
		Handles.color = color2;
		Handles.DrawSolidDisc(key2Pos, forward, HandleSize);
		Handles.color = color3;
		Handles.DrawSolidDisc(key3Pos, forward, HandleSize);
		Handles.color = Color.clear;

		EditorGUI.BeginChangeCheck();
		var newXMinPos = DrawKey(xMinPos);
		if (EditorGUI.EndChangeCheck())
		{
			xMinPos = GetClosestPoint(rxt, rxb, newXMinPos, out var result);
			if (result < crossHatch.y)
			{
				gradientsAreEqual = true;
				crossHatch.x = result;
			}
		}

		EditorGUI.BeginChangeCheck();
		var newXMaxPos = DrawKey(xMaxPos);
		if (EditorGUI.EndChangeCheck())
		{
			xMaxPos = GetClosestPoint(rxt, rxb, newXMaxPos, out var result);
			if (result > crossHatch.x)
			{
				gradientsAreEqual = true;
				crossHatch.y = result;
			}
		}

		EditorGUI.BeginChangeCheck();
		var newKey0 = DrawKey(key0Pos);
		if (EditorGUI.EndChangeCheck())
		{
			key0Pos = GetClosestPoint(rtt, rtb, newKey0, out var result);
			if (result < points.y)
			{
				gradientsAreEqual = true;
				points.x = result;
			}
		}

		EditorGUI.BeginChangeCheck();
		var newKey1 = DrawKey(key1Pos);
		if (EditorGUI.EndChangeCheck())
		{
			key1Pos = GetClosestPoint(rtt, rtb, newKey1, out var result);
			if (result < points.z && result > points.x)
			{
				gradientsAreEqual = true;
				points.y = result;
			}
		}

		EditorGUI.BeginChangeCheck();
		var newKey2 = DrawKey(key2Pos);
		if (EditorGUI.EndChangeCheck())
		{
			key2Pos = GetClosestPoint(rtt, rtb, newKey2, out var result);
			if (result < points.w && result > points.y)
			{
				points.z = result;
			}
		}

		EditorGUI.BeginChangeCheck();
		var newKey3 = DrawKey(key3Pos);
		if (EditorGUI.EndChangeCheck())
		{
			key3Pos = GetClosestPoint(rtt, rtb, newKey3, out var result);
			if (result > points.z)
			{
				gradientsAreEqual = true;
				points.w = result;
			}
		}


		var slopes = new Vector4(Mathf.Pow(slope0, 7f), Mathf.Pow(slope1, 7f), Mathf.Pow(slope2, 7f), 0) * 127f + Vector4.one;

		if (!applyGradientToAll)
		{
			t.SetVector(SlopesId, slopes);
			t.SetVector(PointsId, points);
			t.SetVector(XhMinmaxNoiseId, crossHatch);
		}
		else if (gradientsAreEqual)
		{
			foreach (var material in cellShaderMaterials)
			{
				material.SetVector(SlopesId, slopes);
				material.SetVector(PointsId, points);
				var newCrossHatch = material.GetVector(XhMinmaxNoiseId);
				newCrossHatch.x = crossHatch.x;
				newCrossHatch.y = crossHatch.y;
				material.SetVector(XhMinmaxNoiseId, newCrossHatch);
			}
		}
	}

	private static Vector3 DrawKey(Vector3 pos)
	{
	   return Handles.FreeMoveHandle(pos, HandleSize, Vector3.one, Handles.DotHandleCap);
	}

	private static Vector3 GetClosestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point, out float magnitude)
	{
		var lineDirection = lineEnd - lineStart;
		var lineLength = lineDirection.magnitude;
		lineDirection.Normalize();
		var projectLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0f, lineLength);
		magnitude = projectLength / lineLength;
		return lineStart + lineDirection * projectLength;
	}
}