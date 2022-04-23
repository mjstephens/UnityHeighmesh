using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal abstract class Solver
    {
        #region VARIABLES

        protected readonly List<DataWaveSin> _dataWaveSin = new List<DataWaveSin>();
        protected int _sinCount;
        protected readonly List<DataWaveGerstner> _dataWaveGerstner = new List<DataWaveGerstner>();
        protected int _gerstnerCount;
        protected readonly List<DataNoise> _dataNoise = new List<DataNoise>();
        protected int _noiseCount;
        protected readonly List<DataConfigHeightmap> _dataConfigHeightmap = new List<DataConfigHeightmap>();
        protected readonly List<DataHeightmap> _dataHeightmap = new List<DataHeightmap>();
        protected int _mapCount;

        #endregion VARIABLES


        #region SOLVE
        
        internal virtual void Solve(
            Heightmesh heightmesh, 
            Vector3[] originalVertices, 
            List<IHeightmeshInput> data, 
            List<DataConfigHeightmeshInput> configs,
            float time)
        {
            _dataWaveGerstner.Clear();
            _dataWaveSin.Clear();
            _dataNoise.Clear();
            _dataHeightmap.Clear();

            // Sort + assign data
            for (int i = 0; i < data.Count; i++)
            {
                switch (data[i])
                {
                    case WaveGerstner gerstner: _dataWaveGerstner.Add(gerstner.Data); break;
                    case WaveSin sin: _dataWaveSin.Add(sin.Data); break;
                    case Noise noise: _dataNoise.Add(noise.Data); break;
                    case Heightmap map:
                        _dataConfigHeightmap.Add(configs[i] as DataConfigHeightmap);
                        _dataHeightmap.Add(map.Data);
                        break;
                }
            }

            _gerstnerCount = _dataWaveGerstner.Count;
            _sinCount = _dataWaveSin.Count;
            _noiseCount = _dataNoise.Count;
            _mapCount = _dataHeightmap.Count;
        }

        protected static float CalcRipples(
            Vector2 vertex, 
            DataHeightwaveRipple ripple, 
            float time)
        {
            float dist =  Vector2.Distance (vertex, ripple.Position);
            if (dist > ripple.Distance)
                return 0;
		      
            float strength = Heightmesh.MapValue(
                dist, 
                0f, 
                ripple.Distance,
                ripple.LifetimeRemaining, 0f);
            if (strength > 0)
            {
                return Mathf.Sin(dist - (time * ripple.Speed)) * strength * ripple.Strength;
            }

            return 0;
        }

        #endregion SOLVE
    }
}