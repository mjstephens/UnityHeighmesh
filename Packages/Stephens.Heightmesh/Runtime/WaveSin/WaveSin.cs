using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class WaveSin : HeightmeshInput<DataConfigWaveSin, DataWaveSin>
    {
        #region CONSTRUCTOR

        public WaveSin(DataConfigWaveSin config) : base(config)
        {
            
        }

        #endregion CONSTRUCTOR
        
        
        #region DATA

        protected override void UpdateData(float delta)
        {
            _data = new DataWaveSin()
            {
                Opacity = _config.Opacity,
                Direction = Quaternion.Euler(Vector3.up * _config.Direction),
                Amplitude = _config.Amplitude,
                Wavelength = _config.Wavelength,
                Offset = GetClampedOffset(delta, _config.Speed, _data.Offset),
                WorldAnchored = _config.WorldAnchored,
            };
        }

        #endregion DATA


        #region SOLVE

        internal static float CalcForVertexCPU(
            Vector3 vertex, 
            DataWaveSin data)
        {
            Vector3 dir = data.Direction * vertex;
            return (Mathf.Sin((dir * data.Wavelength).z + data.Offset) * data.Amplitude) * data.Opacity;
        }

        #endregion SOLVE
    }
}