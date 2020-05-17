using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingObject : MonoBehaviour
{
    Material material;
    private void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        
    }

    public Material GetMaterial() { return material; }

    private void OnEnable() => RayTracingMaster.RegisterObject(this);

    private void OnDisable() => RayTracingMaster.UnregisterObject(this);
}
