using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImersiFOX.TrafficSystem
{
    [CreateAssetMenu(fileName = "CarsData", menuName = "TrafficSystem/CarsData", order = 1)]
    public class CarsData : ScriptableObject
    {
        [SerializeField] private string[] _cars;
        
        public string GetCar(int _index)
        {
            return _cars[_index];
        }

        public string GetCarRandom()
        {
            return _cars[Random.Range(0, _cars.Length)];
        }
    }
}
