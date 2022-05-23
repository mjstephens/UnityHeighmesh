using System;
using System.Collections;
using Stephens.Heightmesh;
using UnityEngine;

namespace Stephens.Buoyancy
{
    /// <summary>
    /// Example of a buoyant object implementation that rides directly with the surface of the wave
    /// </summary>
    public class BuoyancySentinel : MonoBehaviour, IBuoyant
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] [Range(0, 1)] private float _stickyness = 1;
        [SerializeField] private Material _matOverwater;
        [SerializeField] private Material _matUnderwater;
        [SerializeField] private Renderer _renderer;
        
        public Vector3 Position => GetPositionForCalculation();
        internal bool Submerged => _state == SubmergedState.Fully;

        private Vector3 _submergePosition; // Position at which the object entered the water
        private Vector3 _wavePositionNoOffset;
        private Vector3 _currentOffset;
        private Vector3 _targetPosition;

        private Transform _target;

        private enum SubmergedState
        {
            Not,
            Transitioning,
            Fully
        }

        private SubmergedState _state;

        #endregion VARIABLES


        #region INITIALIZATION

        private void OnEnable()
        {
            SystemBuoyancy.Instance.RegisterSurfaceObject(this);
        }

        private void OnDisable()
        {
            SystemBuoyancy.Instance.UnregisterSurfaceObject(this);
        }

        internal void RegisterTarget(Transform target)
        {
            _target = target;
        }

        #endregion INITIALIZATION


        #region UPDATE

        private void Update()
        {
            switch (_state)
            {
                case SubmergedState.Not:
                    TickState_Not();
                    break;
                case SubmergedState.Transitioning:
                    TickState_Transitioning();
                    break;
                case SubmergedState.Fully:
                    TickState_Fully();
                    break;
            }
        }

        private void TickState_Not()
        {
            // Have we reached below the surface of the water directly below us?
            if (_target.position.y <= transform.position.y)
            {
                _state = SubmergedState.Transitioning;
            }
        }

        private void TickState_Transitioning()
        {
            
        }
        
        private void TickState_Fully()
        {
            // Have we risen above the waves we are currently in?
            if (_target.position.y > transform.position.y)
            {
                _state = SubmergedState.Not;
                _renderer.material = _matOverwater;
            }
        }

        #endregion


        #region SOLVE

        void IBuoyant.ReceiveSurfacePosition(Vector3 position, Vector2 offset)
        {
            _currentOffset = new Vector3(offset.x, 0, offset.y);
            _submergePosition = new Vector3(_target.position.x, position.y, _target.position.z);
            _wavePositionNoOffset = _submergePosition;
            transform.position = _submergePosition;
            
            if (_state == SubmergedState.Transitioning)
            {
                _submergePosition = transform.position - (_currentOffset * _stickyness);
                _state = SubmergedState.Fully;
                _renderer.material = _matUnderwater;
            }
        }

        #endregion SOLVE


        #region UTILITY

        private Vector3 GetPositionForCalculation()
        {
            return new Vector3(_wavePositionNoOffset.x - _currentOffset.x, 0, _wavePositionNoOffset.z - _currentOffset.z);
        }

        #endregion UTILITY
    }
} 