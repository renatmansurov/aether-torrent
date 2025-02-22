using UnityEngine;

public class LightMatrixUpdater : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The material using the volumetric fog shader.")]
    public Material volumetricFogMaterial;
    [Tooltip("The directional light that illuminates the fog.")]
    public Light directionalLight;

    [Header("Light Space Settings")]
    [Tooltip("Orthographic size for the light projection.")]
    public float orthographicSize = 10f;
    [Tooltip("Near plane of the light projection.")]
    public float nearPlane = 0.1f;
    [Tooltip("Far plane of the light projection.")]
    public float farPlane = 50f;
    [Tooltip("Distance from the camera to position the light for the matrix calculation.")]
    public float distanceFromCamera = 20f;

    void Update()
    {
        if (volumetricFogMaterial == null || directionalLight == null)
            return;

        // Use the main camera as reference for centering the light space matrix.
        Camera cam = Camera.main;
        if (cam == null)
            return;

        // Get the directional light's forward vector (direction in which the light is pointing)
        Vector3 lightDir = directionalLight.transform.forward;

        // Position the light at a point offset from the camera along the light direction.
        Vector3 camPos = cam.transform.position;
        Vector3 lightPos = camPos - lightDir * distanceFromCamera;

        // Create a view matrix for the light.
        // The light "looks at" the camera's position, which helps center the projection.
        Matrix4x4 lightView = Matrix4x4.LookAt(lightPos, camPos, Vector3.up);

        // Create an orthographic projection matrix for the directional light.
        Matrix4x4 lightProj = Matrix4x4.Ortho(
            -orthographicSize, orthographicSize,
            -orthographicSize, orthographicSize,
            nearPlane, farPlane);

        // Combine the projection and view matrices to form the light space matrix.
        Matrix4x4 lightSpaceMatrix = lightProj * lightView;

        // Update the shader property with the computed matrix.
        volumetricFogMaterial.SetMatrix("_LightSpaceMatrix", lightSpaceMatrix);
    }
}