using UnityEngine;
using UnityEngine.Playables;

namespace Environment
{
	[ExecuteInEditMode]
	public class WeatherGlobals : MonoBehaviour
	{
		[SerializeField] public float timeOfDay = 12;
		[SerializeField] private Vector2 dayTime;
		[SerializeField] private Vector2 nightTime;
		[SerializeField] private float overcast = 1;
		[SerializeField] private float cloudContrast = 0.1f;
		[SerializeField] private Vector4 cloudsScaleSpeed;
		[SerializeField] private Vector4 cloudsSpdPostRemap;
		[SerializeField] private Color globalLightColor;
		[SerializeField] private Color globalShadowColor;
		[SerializeField] private Light sunLight;
		[SerializeField] private PlayableDirector mainLightsTimeline;

		//Helper Properties
		[SerializeField] private Gradient lightGradient;
		[SerializeField] private Gradient shadowGradient;
		public Vector2 cloudsScale;
		public Vector2 cloudsSpeed;
		public float cloudsTurbulenceSpeed;
		public int bands;


		void Start()
		{
		}
#if UNITY_EDITOR
		void Update()
		{
			ApplyEnvironmentGlobals();
		}
#endif

		public void ApplyEnvironmentGlobals()
		{
			CalculateValues();
			SetValues();
		}

		float RemapTo01(float value, float min, float max)
		{
			var remapped = max + (value - min) * (max - min);
			return Mathf.Clamp01(remapped);
		}

		float Remap(float value, float low1, float low2, float high1, float high2)
		{
			return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
		}

		private void CalculateValues()
		{
			cloudsScaleSpeed = new Vector4(cloudsScale.x*0.1f, cloudsScale.y*0.1f, cloudsSpeed.x, cloudsSpeed.y);
			cloudsSpdPostRemap = new Vector4(cloudsTurbulenceSpeed, bands, overcast, cloudContrast);
			LoopKeys(lightGradient);
			LoopKeys(shadowGradient);
			var dayNormalizedTime = (timeOfDay - dayTime.x) / (dayTime.y - dayTime.x);
			var nightNormalizedTime = (timeOfDay - nightTime.x) / (nightTime.y - nightTime.x);
			globalLightColor = lightGradient.Evaluate(dayNormalizedTime);
			globalShadowColor = shadowGradient.Evaluate(dayNormalizedTime);
			mainLightsTimeline.time = dayNormalizedTime * 4f;
			mainLightsTimeline.Evaluate();
		}

		private static void LoopKeys(Gradient colorGradient)
		{
			var keys = colorGradient.colorKeys;
			keys[^1] = new GradientColorKey(keys[0].color, colorGradient.colorKeys[^1].time);
			colorGradient.colorKeys = keys;
		}

		private void SetValues()
		{
			Shader.SetGlobalVector("_GlobalCloudsSS", cloudsScaleSpeed);
			Shader.SetGlobalVector("_GlobalCloudsSpdPostRemap", cloudsSpdPostRemap);
			Shader.SetGlobalColor("_GlobalLightColor", globalLightColor);
			RenderSettings.ambientSkyColor = globalShadowColor;
			RenderSettings.ambientEquatorColor = globalLightColor;
			sunLight.color = globalLightColor;
		}
	}
}