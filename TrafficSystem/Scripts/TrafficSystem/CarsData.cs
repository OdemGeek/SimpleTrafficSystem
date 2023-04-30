using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ImersiFOX.TrafficSystem
{
    [CreateAssetMenu(fileName = "CarsData", menuName = "TrafficSystem/CarsData", order = 1)]
    public class CarsData : ScriptableObject
    {
        [SerializeField] private CarPrefab[] _cars;
        [SerializeField, HideInInspector] private int _sumOfChances;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sumOfChances = 0;
            for (int i = 0; i < _cars.Length; i++)
            {
                _sumOfChances += _cars[i].chance;
                _cars[i].chance = Mathf.Max(1, _cars[i].chance);
            }
        }
#endif

        public AssetReferenceGameObject GetCar(int _index)
        {
            return _cars[_index].prefab;
        }

        public AssetReferenceGameObject GetCarRandom()
        {
            int value = Random.Range(0, _sumOfChances + 1);
            int chance = 0;
            int index = 0;
            for (int i = 0; i < _cars.Length; i++)
            {
                chance += _cars[i].chance;
                if (value <= chance)
                {
                    index = i;
                    break;
                }
            }
            return _cars[index].prefab;
        }
    }

    [System.Serializable]
    public struct CarPrefab
    {
        public AssetReferenceGameObject prefab;
        [Range(1, 100)]
        public int chance;
    }
}
