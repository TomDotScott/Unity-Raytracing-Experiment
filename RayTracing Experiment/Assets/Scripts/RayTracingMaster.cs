using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skyBoxTexture;
    public Light directionalLight;

    [Header("Spheres")]
    public int randomSeed;
    public Vector2 sphereRadius = new Vector2(3.0f, 8.0f);
    public uint maxSpheres = 100;
    public float spherePlacementRadius = 100.0f;

    private Camera mainCamera;
    private RenderTexture targetTexture;
    private RenderTexture convergedTexture;
    private Material addMaterial;
    private uint currentSample = 0;
    private ComputeBuffer sphereBuffer;
    private static bool meshesRequireRebuilding = false;
    private static List<RayTracingObject> rayTracingObjects = new List<RayTracingObject>();
    private static List<MeshObject> meshObjects = new List<MeshObject>();
    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> indices = new List<int>();
    private ComputeBuffer meshObjectBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;

    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indicesOffset;
        public int indicesCount;
    }

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
        sphereBuffer?.Release();
        meshObjectBuffer?.Release();
        vertexBuffer?.Release();
        indexBuffer?.Release();
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

    public static void RegisterObject(RayTracingObject obj)
    {
        rayTracingObjects.Add(obj);
        meshesRequireRebuilding = true;
    }
    public static void UnregisterObject(RayTracingObject obj)
    {
        rayTracingObjects.Remove(obj);
        meshesRequireRebuilding = true;
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
            foreach (Sphere other in spheres)
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
        if (sphereBuffer != null)
            sphereBuffer.Release();
        if (spheres.Count > 0)
        {
            sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            sphereBuffer.SetData(spheres);
        }
    }

    private void RebuildMeshObjectBuffers()
    {
        if (!meshesRequireRebuilding)
        {
            return;
        }

        meshesRequireRebuilding = false;
        currentSample = 0;

        // Clear all lists
        meshObjects.Clear();
        vertices.Clear();
        indices.Clear();

        // Loop over all objects and gather their data
        foreach (RayTracingObject obj in rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            // Add vertex data
            int firstVertex = vertices.Count;
            vertices.AddRange(mesh.vertices);

            // Add index data - if the vertex buffer wasn't empty before, the
            // indices need to be offset
            int firstIndex = RayTracingMaster.indices.Count;
            var indices = mesh.GetIndices(0);
            RayTracingMaster.indices.AddRange(indices.Select(index => index + firstVertex));

            // Add the object itself
            meshObjects.Add(new MeshObject()
            {
                localToWorldMatrix = obj.transform.localToWorldMatrix,
                indicesOffset = firstIndex,
                indicesCount = indices.Length
            });
        }

        CreateComputeBuffer(ref meshObjectBuffer, meshObjects, 72);
        CreateComputeBuffer(ref vertexBuffer, vertices, 12);
        CreateComputeBuffer(ref indexBuffer, indices, 4);
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            rayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void SetShaderParameters()
    {
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyBoxTexture);
        rayTracingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetFloat("_Seed", Random.value);

        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        SetComputeBuffer("_Spheres", sphereBuffer);
        SetComputeBuffer("_MeshObjects", meshObjectBuffer);
        SetComputeBuffer("_Vertices", vertexBuffer);
        SetComputeBuffer("_Indices", indexBuffer);
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
        RebuildMeshObjectBuffers();
        SetShaderParameters();
        Render(destination);
    }
}
