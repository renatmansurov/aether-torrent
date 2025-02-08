//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

void curvedWorld_float(float3 worldPos, float depthCurvature, float sidesCurvature, float depthOffset, out float3 Out)
{
    Out = worldPos;
    float3 center = worldPos - _WorldSpaceCameraPos;
    float y = -dot(center.xz, center.xz) * depthCurvature;
    float3 curvedPos = worldPos;
    curvedPos.y += y;
    Out = TransformWorldToObject(curvedPos);;
}