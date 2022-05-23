using System;
using System.Collections.Generic;
using Stephens.Heightmesh;
using UnityEngine;

namespace Stephens.Buoyancy
{
    [DefaultExecutionOrder(-499)]
    public class SystemBuoyancy : MonoBehaviour, ISolverResultsReceivable
    {
        #region SINGLETON

        internal static SystemBuoyancy Instance;
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                Init();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion SINGLETON


        #region VARIABLES

        private readonly List<IBuoyant> _surfaceObjects = new List<IBuoyant>();
        private readonly List<Vector3> _surfaceObjectPositions = new List<Vector3>();
        
        private readonly List<Vector3> _resultPositions = new List<Vector3>();
        private readonly List<Vector2> _resultOffsets = new List<Vector2>();

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Init()
        {
            
        }
        
        #endregion INITIALIZATION


        #region REGISTRATION

        internal void RegisterSurfaceObject(IBuoyant s)
        {
            _surfaceObjects.Add(s);
            _surfaceObjectPositions.Add(s.Position);
            _resultPositions.Add(s.Position);
            _resultOffsets.Add(Vector2.zero);
        }
        
        internal void UnregisterSurfaceObject(IBuoyant s)
        {
            int index = _surfaceObjects.IndexOf(s);
            _surfaceObjects.Remove(s);
            _surfaceObjectPositions.RemoveAt(index);
            _resultPositions.RemoveAt(index);
            _resultOffsets.RemoveAt(index);
        }

        #endregion REGISTRATION


        #region SOLVE

        private void Update()
        {
            // Update positions
            for (int i = 0; i < _surfaceObjectPositions.Count; i++)
            {
                _surfaceObjectPositions[i] = new Vector3(_surfaceObjects[i].Position.x, 0, _surfaceObjects[i].Position.z);
            }
            
            SystemHeightmesh.Instance.Solve(_surfaceObjectPositions.ToArray(), Vector3.zero, this, true);
        }

        private bool falg;
        void ISolverResultsReceivable.DoReceiveSolverResults(List<Vector3> positions, List<Vector2> offsets)
        {
            for (int i = 0; i < positions.Count; i++)
            { 
                _surfaceObjects[i].ReceiveSurfacePosition(positions[i], offsets[i]);
            }
        }

        #endregion SOLVE
    }
}