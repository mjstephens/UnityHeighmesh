using Unity.Mathematics;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Noise : HeightmeshInput<DataConfigNoise, DataNoise>
    {
        #region CONSTRUCTOR

        internal Noise(DataConfigNoise config) : base(config)
        {
            
        }

        #endregion CONSTRUCTOR


        #region DATA
        
        protected override void UpdateData(float delta)
        {
            _data = new DataNoise()
            {
                Type = _config.Type,
                Strength = _config.Strength,
                Spread = _config.Spread,
                Offset = GetClampedOffset(delta, _config.Speed, _data.Offset)
            };
        }

        #endregion DATA


        #region SOLVE

        internal static float CalcForVertexCPU(
            Vector3 vertex,
            Vector3 meshPosition, 
            DataNoise data)
        {
            float x = (vertex.x + meshPosition.x - data.Offset.x) * data.Spread;
            float y = (vertex.z + meshPosition.z - data.Offset.y) * data.Spread;
            
            switch (data.Type)
            {
                case NoiseType.Perlin: return Mathf.PerlinNoise(x, y) * data.Strength;
                case NoiseType.Cellular: return noise.cellular2x2(new float2(x,y)).x * data.Strength;
                case NoiseType.Simplex: return noise.snoise(new float2(x,y)) * data.Strength;
            }

            return 0;
        }

        #endregion SOLVE
    }
}