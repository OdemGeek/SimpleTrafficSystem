using ImersiFOX.TrafficSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.AICarControllerSystem
{
    public class AICarController : MonoBehaviour
    {
        private float steerAngle;
        private float verticalInput;
        private float currentSteerAngle;
        private float currentbreakForce;
        private bool isBreaking;
        private float speed;
        private float lastTimeChangedLane;
        private float distanceToTarget;

        private const float LANE_CHANGE_FREQ = 4f;
        private const float POINT_DISTANCE_THREASHOLD = 5f;
        private const float WHEELS_UPDATE_DISTANCE_SQR = 12000;

        [SerializeField] private float motorForce;
        [SerializeField] private float breakForce;
        [SerializeField] private float maxSteerAngle;

        [SerializeField] private Rigidbody rb;
        [SerializeField] private WheelCollider frontLeftWheelCollider;
        [SerializeField] private WheelCollider frontRightWheelCollider;
        [SerializeField] private WheelCollider rearLeftWheelCollider;
        [SerializeField] private WheelCollider rearRightWheelCollider;

        [SerializeField] private Transform frontLeftWheelTransform;
        [SerializeField] private Transform frontRightWheeTransform;
        [SerializeField] private Transform rearLeftWheelTransform;
        [SerializeField] private Transform rearRightWheelTransform;

        [SerializeField] private CarInputs carInputs;
        [SerializeField] private MeshRenderer mesh;

        private void FixedUpdate()
        {
            speed = rb.velocity.magnitude;
            distanceToTarget = Vector3.Distance(transform.position, carInputs.targetPosition);
            GetInput();
            HandleMotor();
            HandleSteering();

            // If we don't have camera associated update wheels
            // If camera close to car update wheels
            if (TrafficManager._playerCamera == null)
                UpdateWheels();
            else if ((TrafficManager._playerCamera.transform.position - transform.position).sqrMagnitude < WHEELS_UPDATE_DISTANCE_SQR && mesh.isVisible)
            {
                UpdateWheels();
            }
        }

        private bool CheckCollisions()
        {
            bool wasCollision = false;
            if (Physics.Raycast(transform.position, transform.forward, 14f))
            {
                wasCollision = true;
                if (Time.time - lastTimeChangedLane < LANE_CHANGE_FREQ) return wasCollision;
                lastTimeChangedLane = Time.time;
                carInputs.ChangeLane();
            }
            return wasCollision;
        }

        private void GetInput()
        {
            Vector2 directionToTarget = (new Vector2(transform.position.x - carInputs.targetPosition.x, transform.position.z - carInputs.targetPosition.z)).normalized;
            float angleToTarget = Vector2.SignedAngle(new Vector2(-transform.forward.x, -transform.forward.z), directionToTarget);
            steerAngle = -angleToTarget;
            //float curvatureSpeed = 1 - Mathf.Clamp01(carInputs.curvature * 20);
            //curvatureSpeed = curvatureSpeed * curvatureSpeed;
            float angleSpeed = Mathf.Clamp(Vector2.Dot(new Vector2(carInputs.tangent.x, carInputs.tangent.z), new Vector2(transform.forward.x, transform.forward.z)), 0.15f, 1);
            bool haveCollision = CheckCollisions();
            verticalInput = speed < carInputs.targetSpeed && !haveCollision ? 0.8f * angleSpeed : 0f;
            isBreaking = speed - carInputs.targetSpeed > 1 || haveCollision;

            if (distanceToTarget < POINT_DISTANCE_THREASHOLD ||
                Vector2.Dot(new Vector2(carInputs.tangent.x, carInputs.tangent.z), directionToTarget) > 0) carInputs.needNewPoint = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, verticalInput * (isBreaking ? 0 : 1));
            Gizmos.DrawSphere(carInputs.targetPosition, 1f);
            Gizmos.DrawLine(carInputs.targetPosition, transform.position);
        }
#endif

        private void HandleMotor()
        {
            float canMotor = speed > carInputs.maxSpeed ? 0f : 1f;
            frontLeftWheelCollider.motorTorque = verticalInput * motorForce * canMotor;
            frontRightWheelCollider.motorTorque = verticalInput * motorForce * canMotor;
            currentbreakForce = isBreaking ? breakForce : 0f;
            ApplyBreaking();
        }

        private void ApplyBreaking()
        {
            frontRightWheelCollider.brakeTorque = currentbreakForce;
            frontLeftWheelCollider.brakeTorque = currentbreakForce;
            rearLeftWheelCollider.brakeTorque = currentbreakForce;
            rearRightWheelCollider.brakeTorque = currentbreakForce;
        }

        private void HandleSteering()
        {
            currentSteerAngle = Mathf.Clamp(steerAngle, -maxSteerAngle, maxSteerAngle);
            frontLeftWheelCollider.steerAngle = currentSteerAngle;
            frontRightWheelCollider.steerAngle = currentSteerAngle;
        }

        private void UpdateWheels()
        {
            UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
            UpdateSingleWheel(frontRightWheelCollider, frontRightWheeTransform);
            UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
            UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        }

        private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
        {
            Vector3 pos;
            Quaternion rot
    ; wheelCollider.GetWorldPose(out pos, out rot);
            wheelTransform.rotation = rot;
            wheelTransform.position = pos;
        }
    }
}
