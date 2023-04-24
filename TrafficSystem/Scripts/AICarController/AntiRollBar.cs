using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.AICarControllerSystem
{
    [AddComponentMenu("Traffic System/AI/Anti Roll Bar")]
    public class AntiRollBar : MonoBehaviour
    {
        [SerializeField] private WheelCollider _wheelL;
        [SerializeField] private WheelCollider _wheelR;
        [SerializeField] private float _antiRoll = 1000f;
        [SerializeField] private Rigidbody _rigidbody;

        public void FixedUpdate()
        {
            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = _wheelL.GetGroundHit(out hit);
            if (groundedL)
                travelL = (-_wheelL.transform.InverseTransformPoint(hit.point).y - _wheelL.radius) / _wheelL.suspensionDistance;

            bool groundedR = _wheelR.GetGroundHit(out hit);
            if (groundedR)
                travelR = (-_wheelR.transform.InverseTransformPoint(hit.point).y - _wheelR.radius) / _wheelR.suspensionDistance;

            float antiRollForce = (travelL - travelR) * _antiRoll;

            if (groundedL)
                _rigidbody.AddForceAtPosition(_wheelL.transform.up * -antiRollForce,
                _wheelL.transform.position);

            if (groundedR)
                _rigidbody.AddForceAtPosition(_wheelR.transform.up * antiRollForce,
                _wheelR.transform.position);
        }
    }
}
