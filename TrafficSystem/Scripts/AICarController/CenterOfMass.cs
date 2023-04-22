using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.AICarControllerSystem
{
    public class CenterOfMass : MonoBehaviour
    {
        [SerializeField] private Transform _point;
        [SerializeField] private Rigidbody _rigidbody;

        private void Start()
        {
            _rigidbody.centerOfMass = _point.localPosition;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TryGetComponent(out _rigidbody);
        }
#endif
    }
}
