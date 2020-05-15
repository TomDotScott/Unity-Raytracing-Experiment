using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    private RenderTexture target;
    private Camera mainCamera;
    public Texture sky;
    private uint currentSample = 0;
    private Material addMaterial;
    public Light directionalLight;


    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (transform.hasChanged || directionalLight.transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
            directionalLight.transform.hasChanged = false;
        }
    }

    private void SetShaderParameters()
    {
        Vector3 light = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(light.x, light.y, light.z, directionalLight.intensity));

        rayTracingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetTexture(0, "_SkyBoxTexture", sky);
    }

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        SetShaderParameters();
        Render(_destination);
    }

    private void Render(RenderTexture _destination)
    {
        //Initalise the RenderTexture
        InitRenderTexture();

        //set the target
        rayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //Blit the resulting texture to the screen
        if(addMaterial == null)
        {
            addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, _destination, addMaterial);
        currentSample++;
    }

    private void InitRenderTexture()
    {
        if(target == null || target.width != Screen.width || target.height != Screen.height)
        {
            if(target != null)
            {
                target.Release();
            }
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }

    }
}
