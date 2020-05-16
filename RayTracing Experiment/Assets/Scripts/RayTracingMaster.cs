using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skyboxTexture;
    public Light directionalLight;

    [Header("Sphere Settings")]
    public int randomSeed;
    public Vector2 sphereRadius;
    public uint maxSpheres;
    public float spherePlacementRadius;

    private Camera mainCamera;
    
    private RenderTexture targetTexture;
    private RenderTexture convergedTexture;
    private Material addMaterial;
    private uint currentSample;
    private ComputeBuffer buffer;
    

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    }

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (buffer != null)
            buffer.Release();
    }

    private void Update()
    {
        if (transform.hasChanged || directionalLight.transform.hasChanged)
        {
            transform.hasChanged = false;
            directionalLight.transform.hasChanged = false;
            currentSample = 0;
        }
    }

    private void SetUpScene()
    {
        Random.InitState(randomSeed);
        List<Sphere> spheres = new List<Sphere>();

        // Add a number of random spheres
        for (int i = 0; i < maxSpheres; i++)
        {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * spherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (var other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();
            float chance = Random.value;
            if (chance < 0.8f)
            {
                bool metal = chance < 0.4f;
                sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
                sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);
                sphere.smoothness = Random.value;
            }
            else
            {
                Color emission = Random.ColorHSV(0, 1, 0, 1, 3.0f, 8.0f);
                sphere.emission = new Vector3(emission.r, emission.g, emission.b);
            }

            // Add the sphere to the list
            spheres.Add(sphere);

        SkipSphere:
            continue;
        }

        // Assign to compute buffer
        if (buffer != null)
            buffer.Release();
        if (spheres.Count > 0)
        {
            buffer = new ComputeBuffer(spheres.Count, 56);
            buffer.SetData(spheres);
        }
    }

    private void SetShaderParameters()
    {
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        rayTracingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetFloat("_Seed", Random.value);

        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        if (buffer != null)
            rayTracingShader.SetBuffer(0, "_Spheres", buffer);
    }

    private void InitRenderTexture()
    {
        if (targetTexture == null || targetTexture.width != Screen.width || targetTexture.height != Screen.height)
        {
            // Release render texture if we already have one
            if (targetTexture != null)
            {
                targetTexture.Release();
                convergedTexture.Release();
            }

            // Get a render target for Ray Tracing
            targetTexture = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            targetTexture.enableRandomWrite = true;
            targetTexture.Create();
            convergedTexture = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            convergedTexture.enableRandomWrite = true;
            convergedTexture.Create();

            // Reset sampling
            currentSample = 0;
        }
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        rayTracingShader.SetTexture(0, "Result", targetTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        if (addMaterial == null)
            addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(targetTexture, convergedTexture, addMaterial);
        Graphics.Blit(convergedTexture, destination);
        currentSample++;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
}
