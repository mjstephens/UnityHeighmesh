using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class WaveGerstner : HeightmeshInput<DataConfigWaveGerstner, DataWaveGerstner>
    {
        #region CONSTRUCTOR

        internal WaveGerstner (DataConfigWaveGerstner config) : base(config)
        {
            
        }

        #endregion CONSTRUCTOR


        #region DATA

        protected override void UpdateData(float delta)
        {
            // Precalculate data to avoid re-calculation inside vertex job
            float wavelength = 6.28318f / _config.Wavelength;				        // 2pi over wavelength(hardcoded)
            float qi = _config.PeakSharpness / (_config.Amplitude * wavelength);    // 0.8 = peak value, 1 is the sharpest peaks
            float direction = Mathf.Deg2Rad * _config.Direction;
            Vector3 windDirInput = new Vector3(
                Mathf.Sin(direction), 
                0,
                Mathf.Cos(direction)) * (1 - (_config.OmniDirectional ? 1 : 0));
		        
            _data = new DataWaveGerstner()
            {
                Origin = _config.Origin,
                Amplitude = _config.Amplitude,
                OmniDirectional = _config.OmniDirectional,
                WorldAnchored = _config.WorldAnchored,
                Offset = GetClampedOffset(delta, _config.Speed, _data.Offset),
			        
                Wavelength = wavelength,
                Qi = qi,
                WindDirectionInput = windDirInput
            };
        }

        #endregion DATA


        #region SOLVE

        // From: https://github.com/Unity-Technologies/BoatAttack/blob/42f9e9e6f2877573382244f8a07968848dbab9b0/Packages/com.verasl.water-system/Scripts/GerstnerWavesJobs.cs
        internal static Vector3 CalcForVertexCPU(
            Vector3 vertex, 
            Vector3 omni,
            Vector3 origin,
            DataWaveGerstner data, 
            float countMulti)
        {
            float amplitude = data.Amplitude;
            Vector3 omniPos = origin;
            float omniDir = data.OmniDirectional ? 1 : 0;
            
            //  calculate wind direction - TODO - currently radians
            Vector3 windOmniInput = (omni - omniPos) * omniDir;
            Vector3 windDir = data.WindDirectionInput + windOmniInput;
            windDir = Vector3.Normalize(windDir);
            float dir = Vector3.Dot(windDir, omni - omniPos * omniDir);

            ////////////////////////////position output calculations/////////////////////////
            float calc = dir * data.Wavelength - data.Offset; // the wave calculation
            float cosCalc = Mathf.Cos(calc); // cosine version(used for horizontal undulation)
            float sinCalc = Mathf.Sin(calc); // sin version(used for vertical undulation)

            // calculate the offsets for the current point
            vertex.x = data.Qi * amplitude * windDir.x * cosCalc;
            vertex.z = data.Qi * amplitude * windDir.z * cosCalc;
            vertex.y = sinCalc * amplitude * countMulti; // the height is divided by the number of waves 

            return vertex;
        }

        #endregion SOLVE
    }
}