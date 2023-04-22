using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.TrafficSystem
{
    [DefaultExecutionOrder(-5)]
    public class CameraInstance : MonoBehaviour
    {
        private void Awake()
        {
            TrafficManager._playerCamera = transform;
        }
    }
}
