using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Manager class used in this package demo scene. This should be refactored into project-specific architecture
    /// </summary>
    [DefaultExecutionOrder(-500)]
    internal class SystemHeightmesh : MonoBehaviour
    {
        #region SINGLETON

        internal static SystemHeightmesh Instance;
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

        [SerializeField] private DataConfigHeightmeshInput[] _inputData;
        
        private float _time;
        private readonly List<DataConfigHeightmeshInput> _configData = new List<DataConfigHeightmeshInput>();
        private readonly List<IHeightmeshInput> _inputs = new List<IHeightmeshInput>();
        private readonly List<Heightmesh> _heightmeshes = new List<Heightmesh>();

        #endregion VARIABLES


        #region INITIALIZATION

        private void Init()
        {
            // Resolve wave types into classes
            foreach (DataConfigHeightmeshInput data in _inputData)
            {
                _configData.Add(data);
                switch (data)
                {
                    case DataConfigWaveGerstner gerstner: _inputs.Add(new WaveGerstner(gerstner)); break;
                    case DataConfigWaveSin sin: _inputs.Add(new WaveSin(sin)); break;
                    case DataConfigNoise noise: _inputs.Add(new Noise(noise)); break;
                    case DataConfigHeightmap map: _inputs.Add(new Heightmap(map)); break;
                }
            }
        }

        #endregion INITIALIZATION


        #region REGISTRATION

        internal void RegisterHeightmesh(Heightmesh heightmesh)
        {
            _heightmeshes.Add(heightmesh);
        }
        
        internal void UnregisterHeightmesh(Heightmesh heightmesh)
        {
            _heightmeshes.Remove(heightmesh);
        }

        #endregion REGISTRATION


        #region UPDATE

        private void Update()
        {
            _time += Time.deltaTime;
            
            // Update inputs
            foreach (IHeightmeshInput input in _inputs)
            {
                input.Tick(Time.deltaTime);
            }
            
            // Update meshes
            foreach (Heightmesh heightmesh in _heightmeshes)
            {
                heightmesh.Solve(_inputs, _configData, _time);
            }
        }

        #endregion UPDATE


        #region CLEANUP

        private void OnDestroy()
        {
            foreach (IHeightmeshInput input in _inputs)
            {
                if (input is Heightmap h)
                {
                    h.Cleanup();
                }
            }
        }

        #endregion CLEANUP
    }
}