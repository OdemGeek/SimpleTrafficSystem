using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.TrafficSystem
{
    [AddComponentMenu("Traffic System/AI/Car Inputs")]
    public class CarInputs : MonoBehaviour
    {
        public float maxSpeed { get; internal set; }
        public Vector3 targetPosition { get; internal set; }
        public float targetSpeed { get; internal set; }
        public Vector3 tangent { get; internal set; }
        [HideInInspector] public bool needNewPoint = true;

        internal int laneIndex;
        internal bool needLaneChange = false;

        public void ChangeLane()
        {
            needLaneChange = true;
        }
    }
}
