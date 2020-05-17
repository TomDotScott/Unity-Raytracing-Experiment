using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingObject : MonoBehaviour
{
    private void OnEnable() => RayTracingMaster.RegisterObject(this);

    private void OnDisable() => RayTracingMaster.UnregisterObject(this);
}
