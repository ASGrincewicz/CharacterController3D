using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VeganimusStudios
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _followTarget = null;
        [SerializeField] private Vector3 _followOffset;
        private Transform _transform;
        private Vector3 _followPos;
        private void Start() => _transform = transform;

        private void Update()
        {
            _followPos = _followTarget.position;
            if (_followTarget == null) return;
            _transform.position = _followPos + _followOffset;
           
        }
    }
}
