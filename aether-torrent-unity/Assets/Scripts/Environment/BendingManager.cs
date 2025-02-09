using UnityEngine;
using UnityEngine.Rendering;

namespace Environment
{
    [ExecuteAlways]
    public class BendingManager : MonoBehaviour
    {
        private const string BendingFeature = "ENABLE_WORLD_BENDING";
        private static readonly int DepthBendGlobal = Shader.PropertyToID("_DepthBendGlobal");
        private static readonly int SidesBendGlobal = Shader.PropertyToID("_SidesBendGlobal");
        private static readonly int DepthBendOffsetGlobal = Shader.PropertyToID("_DepthBendOffsetGlobal");
        private static readonly int DepthBendSlopeGlobal = Shader.PropertyToID("_DepthBendSlopeGlobal");
        private static readonly int CustomCameraTransform = Shader.PropertyToID("_CustomCameraTransform");
        [SerializeField] private bool enablePlanet;
        [SerializeField] [Range(0.0f, 0.01f)] private float bendingAmount;
        [SerializeField] [Range(0.0f, 0.01f)] private float sidesBendingAmount;
        [SerializeField] private float depthBendOffsetAmount;
        [SerializeField] private float depthBendSlopeAmount;
        [SerializeField] private Transform customCameraTransform;

        private float prevAmount;

        private void Awake()
        {
            Shader.EnableKeyword(BendingFeature);
            UpdateBendingAmount();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void Update()
        {
            //if (Math.Abs(prevAmount - bendingAmount) > Mathf.Epsilon)
            UpdateBendingAmount();
        }

        private void OnValidate()
        {
            UpdateBendingAmount();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void UpdateBendingAmount()
        {
            //prevAmount = bendingAmount;
            Shader.SetGlobalFloat(DepthBendGlobal, bendingAmount);
            Shader.SetGlobalFloat(SidesBendGlobal, sidesBendingAmount);
            Shader.SetGlobalFloat(DepthBendOffsetGlobal, depthBendOffsetAmount);
            Shader.SetGlobalFloat(DepthBendSlopeGlobal, depthBendSlopeAmount);
            Shader.SetGlobalVector(CustomCameraTransform, customCameraTransform.position);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            cam.cullingMatrix = Matrix4x4.Ortho(-99, 99, -99, 99, 0.001f, 99) * cam.worldToCameraMatrix;
        }

        private static void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            cam.ResetCullingMatrix();
        }
    }
}