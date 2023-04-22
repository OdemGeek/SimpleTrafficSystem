using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace ImersiFOX.TrafficSystem
{
    public class TrafficManager : MonoBehaviour
    {
        public static Transform _playerCamera;
        [Space(10)]
        [SerializeField] private CarsData _carsData;
        [SerializeField] private SplineContainer _splineContainer;

        [Header("Road Settings")]
        [SerializeField] private int _oneSideLanes = 2;
        [SerializeField] private float _laneWidth = 5f;
        [SerializeField] private int _carsSimulationCount = 10;
        [SerializeField] private bool _leftSided = false;
        [SerializeField] private float _carsSpawnDistance = 150f;
        [SerializeField] private float _carsDeleteDistance = 350f;
        private float _carsDeleteDistanceSqr { get => _carsDeleteDistance * _carsDeleteDistance; }

        [SerializeField] private LayerMask _layerCars;

        [Header("AI Settings")]
        [SerializeField] private Vector2 _minMaxSpeed = new Vector2(40, 70);

        private CarInputs[] _cars;
        private Rigidbody[] _carsRb;

        private const float LOOK_AHEAD_DIST = 10.0f;
        private const float LOOK_AHEAD_DIST_CURVATURE = 25.0f;
        private const float DRAW_LANE_SEGMENT_LENGTH = 10f;

        private float _splineUnit;
        private float _splineOffset;
        private float _splineOffsetCurvature;
        private bool _spawnedCarThisFrame = false;

        private WaitForFixedUpdate waitForFixedUpdate;

        private void Awake()
        {
            _cars = new CarInputs[_carsSimulationCount];
            _carsRb = new Rigidbody[_carsSimulationCount];

            _splineUnit = 1f / _splineContainer.Spline.GetLength();
            _splineOffset = _splineUnit * LOOK_AHEAD_DIST;
            _splineOffsetCurvature = _splineUnit * LOOK_AHEAD_DIST_CURVATURE;
            waitForFixedUpdate = new WaitForFixedUpdate();
            StartCoroutine(SpawnCars());
        }

        IEnumerator SpawnCars()
        {
            for (int i = 0; i < _carsSimulationCount; i++)
            {
                bool spawned = false;
                _cars[i] = Instantiate(Resources.Load(_carsData.GetCarRandom()) as GameObject,
                    new Vector3(0, -2000, 0), Quaternion.identity)
                    .GetComponent<CarInputs>();
                _carsRb[i] = _cars[i].GetComponent<Rigidbody>();

                _cars[i].maxSpeed = UnityEngine.Random.Range(_minMaxSpeed.x, _minMaxSpeed.y);
                (Vector3 pos, Vector3 dir, int lane) = GetSpawnPosition(_playerCamera.position);
                Collider[] collsOverlap = Physics.OverlapSphere(pos, 10f, _layerCars, QueryTriggerInteraction.Ignore);
                if (collsOverlap.Length == 0)
                {
                    _cars[i].laneIndex = lane;
                    _cars[i].transform.position = pos;
                    _cars[i].transform.forward = dir;
                    _carsRb[i].velocity = dir * 5;
                    //_spawnedCarThisFrame = true;
                    Physics.SyncTransforms();
                }
                else
                {
                    _cars[i].transform.position = new Vector3(0, -2000, 0);
                    _carsRb[i].isKinematic = true;
                }

                //yield return waitForFixedUpdate;
            }
            yield return null;
        }

        private void FixedUpdate()
        {
            _spawnedCarThisFrame = false;
            for (int i = 0; i < _carsSimulationCount; i++)
            {
                if (_cars[i] == null) continue;
                if ((_cars[i].transform.position - _playerCamera.position).sqrMagnitude > _carsDeleteDistanceSqr && !_spawnedCarThisFrame)
                {
                    (Vector3 position, Vector3 dir, int lane) = GetSpawnPosition(_playerCamera.position);
                    // If it's empty space
                    Collider[] collsOverlap = Physics.OverlapSphere(position, 10f, _layerCars, QueryTriggerInteraction.Ignore);
                    if (collsOverlap.Length == 0)
                    {
                        _cars[i].laneIndex = lane;
                        _cars[i].transform.position = position;
                        _cars[i].transform.forward = dir;
                        _cars[i].needLaneChange = false;
                        _cars[i].needNewPoint = true;
                        _carsRb[i].velocity = dir * 5;
                        _carsRb[i].angularVelocity = Vector3.zero;
                        _carsRb[i].isKinematic = false;
                        //_spawnedCarThisFrame = true;
                        Physics.SyncTransforms();
                    }
                }
                if (!_cars[i].needNewPoint) continue;
                if (_cars[i].needLaneChange) RandomLaneSameSide(i);
                (Vector3 pos, float curvatureDistance, Vector3 tangent) = EvaluateTargetPointOnLane(_cars[i].laneIndex, _cars[i].transform);
                _cars[i].targetPosition = pos;
                _cars[i].tangent = tangent * Mathf.Sign(_cars[i].laneIndex) * (_leftSided ? 1 : -1); // Reverse tangent if we on oposite side

                _cars[i].targetSpeed = GetSpeedByCurvature(curvatureDistance);
                _cars[i].needNewPoint = false;
            }
        }

        private float GetSpeedByCurvature(float curvatureDistance)
        {
            return (curvatureDistance + 5) / 4f;
        }

        private (Vector3 position, Vector3 direction, int lane) GetSpawnPosition(Vector3 relativeTo)
        {
            int laneIndex = RandomLane();
            Ray ray = new Ray(_splineContainer.transform.InverseTransformPoint(relativeTo), Vector3.down); // Create ray from camera pos shooting down
            SplineUtility.GetNearestPoint(_splineContainer.Spline, ray, out float3 nearestPosition, out float t); // Get nearest pos on spline
            float evaluatePoint = t + (_carsSpawnDistance / _splineContainer.Spline.GetLength()) * (UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1);
            (Vector3 pos, Vector3 tangent) = EvaluatePositionAndTangentOnLane(evaluatePoint % 1, laneIndex);
            tangent = tangent * Mathf.Sign(laneIndex) * (_leftSided ? 1 : -1);
            pos = pos + Vector3.up;

            return (pos, tangent, laneIndex);
        }

        public int RandomLane()
        {
            int laneIndex = UnityEngine.Random.Range(-_oneSideLanes, _oneSideLanes);
            laneIndex = laneIndex >= 0 ? laneIndex + 1 : laneIndex; // Skip 0 lane
            return laneIndex;
        }

        public void RandomLaneSameSide(int i)
        {
            if (Mathf.Abs(_cars[i].laneIndex) + 1 <= _oneSideLanes)
            {
                _cars[i].laneIndex += (int)Mathf.Sign(_cars[i].laneIndex);
            }
            else if (Mathf.Abs(_cars[i].laneIndex) - 1 > 0)
            {
                _cars[i].laneIndex -= (int)Mathf.Sign(_cars[i].laneIndex);
            }
            _cars[i].needLaneChange = false;
        }

        public (Vector3 position, float curvature, Vector3 tangent) EvaluateTargetPointOnLane(int _laneIndex, Transform _car)
        {
            Ray ray = new Ray(_splineContainer.transform.InverseTransformPoint(_car.position), Vector3.down); // Create ray from car pos shooting down
            SplineUtility.GetNearestPoint(_splineContainer.Spline, ray, out float3 nearestPosition, out float t); // Get nearest pos on spline
            float3 tangentOrigin = SplineUtility.EvaluateTangent(_splineContainer.Spline, t); // Get nearest pos on spline

            SplineUtility.Evaluate(_splineContainer.Spline, (t + _splineOffset * Mathf.Sign(_laneIndex) * (_leftSided ? 1 : -1)) % 1f, out float3 pos, out float3 tangent, out float3 upVector); // Get next position with offset

            Vector3 laneDirOrigin = Vector3.Cross(((Vector3)tangentOrigin).normalized, ((Vector3)upVector).normalized); // Find direction to lane
            Vector3 laneDir = Vector3.Cross(((Vector3)tangent).normalized, ((Vector3)upVector).normalized); // Find direction to lane

            nearestPosition = nearestPosition + (float3)laneDirOrigin * _laneWidth * _laneIndex; // Find pos in lane
            pos = pos + (float3)laneDir * _laneWidth * _laneIndex;

            Vector3 nearestPos = _splineContainer.transform.TransformPoint(pos);

            float3 curvatureCenter = SplineUtility.EvaluateCurvatureCenter(_splineContainer.Spline, (t + _splineOffsetCurvature * Mathf.Sign(_laneIndex) * (_leftSided ? 1 : -1)) % 1f);
            float curvatureDistance = Vector3.Distance(nearestPosition, curvatureCenter);

            return (nearestPos, curvatureDistance, _splineContainer.transform.TransformDirection(tangent).normalized);
        }

        public Vector3 EvaluatePositionOnLane(float t, int _laneIndex)
        {
            SplineUtility.Evaluate(_splineContainer.Spline, t, out float3 pos, out float3 tangent, out float3 upVector); // Get position
            Vector3 laneDir = Vector3.Cross(((Vector3)tangent).normalized, ((Vector3)upVector).normalized); // Find direction to lane
            pos = pos + (float3)laneDir * _laneWidth * _laneIndex; // Find pos in lane
            Vector3 nearestPos = _splineContainer.transform.TransformPoint(pos);
            return nearestPos;
        }

        public (Vector3 position, Vector3 tangent) EvaluatePositionAndTangentOnLane(float t, int _laneIndex)
        {
            SplineUtility.Evaluate(_splineContainer.Spline, t, out float3 pos, out float3 tangent, out float3 upVector); // Get position
            Vector3 laneDir = Vector3.Cross(((Vector3)tangent).normalized, ((Vector3)upVector).normalized); // Find direction to lane
            pos = pos + (float3)laneDir * _laneWidth * _laneIndex; // Find pos in lane
            Vector3 nearestPos = _splineContainer.transform.TransformPoint(pos);
            return (nearestPos, _splineContainer.transform.TransformDirection(tangent).normalized);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            for (int i = 1; i <= _oneSideLanes; i++)
            {
                DrawLane(i);
                DrawLane(-i);
            }
        }

        private void DrawLane(int _laneIndex)
        {
            int resolution = (int)(_splineContainer.Spline.GetLength() / DRAW_LANE_SEGMENT_LENGTH);
            Vector3 prevPoint = EvaluatePositionOnLane(0, _laneIndex);
            for (int i = 1; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                Vector3 point = EvaluatePositionOnLane(t, _laneIndex);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif
    }
}
