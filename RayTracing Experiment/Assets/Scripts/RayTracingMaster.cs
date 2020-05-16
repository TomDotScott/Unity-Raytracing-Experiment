using UnityEngine;
using System.Collections.Generic;




public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;

    private RenderTexture target;
    private RenderTexture converged;

    private Camera mainCamera;
    public Texture sky;
    private uint currentSample = 0;
    private Material addMaterial;
    public Light directionalLight;

    [Header("Sphere Controls")]
    public int sphereSeed;
    public Vector2 sphereRadius;
    public uint spheresMax;
    public float spherePlacementRadius;
    private ComputeBuffer sphereBuffer;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }

    private void OnEnable()
    {
        currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (sphereBuffer != null)
        {
            sphereBuffer.Release();
        }
    }

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

    private void SetUpScene()
    {
        Random.InitState(sphereSeed);
        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        for (int i = 0; i < spheresMax; i++)
        {
            Sphere sphere = new Sphere();
            // Radius and radius
            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * spherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            // Add the sphere to the list
            spheres.Add(sphere);
            Debug.Log("Sphere added!");
        SkipSphere:
            continue;
        }
        // Assign to compute buffer
        sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        sphereBuffer.SetData(spheres);
    }

    private void SetShaderParameters()
    {
        Vector3 light = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(light.x, light.y, light.z, directionalLight.intensity));
        rayTracingShader.SetFloat("_Seed", Random.value);
        rayTracingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetTexture(0, "_SkyBoxTexture", sky);
        rayTracingShader.SetBuffer(0, "_Spheres", sphereBuffer);
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
        if (addMaterial == null)
        {
            addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, converged, addMaterial);
        Graphics.Blit(converged, _destination);
        currentSample++;
    }

    private void InitRenderTexture()
    {
        if (target == null || target.width != Screen.width || target.height != Screen.height)
        {
            if (target != null)
            {
                target.Release();
            }
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
        if (converged == null || converged.width != Screen.width || converged.height != Screen.height)
        {
            if (converged != null)
            {
                converged.Release();
            }
            converged = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            converged.enableRandomWrite = true;
            converged.Create();
        }
    }
}
