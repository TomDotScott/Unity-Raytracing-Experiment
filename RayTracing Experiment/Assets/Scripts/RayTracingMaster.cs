using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    private RenderTexture target;

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
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
        Graphics.Blit(target, _destination);
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
