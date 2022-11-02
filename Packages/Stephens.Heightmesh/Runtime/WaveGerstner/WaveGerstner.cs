using System;
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
            float qi = _config.PeakSharpness / (_config.Amplitude * wavelength);    // peak value, 1 is the sharpest peaks
            float direction = Mathf.Deg2Rad * _config.Direction;
            Vector3 windDirInput = new Vector3(
                Mathf.Sin(direction), 
                0,
                Mathf.Cos(direction)) * (1 - (_config.OmniDirectional ? 1 : 0));
		        
            _data = new DataWaveGerstner
            {
                Origin = _config.Origin,
                Amplitude = _config.Amplitude,
                OmniDirection = _config.OmniDirectional ? 1 : 0,
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
        
        internal static Vector3 GetGerstnerPosition(Vector3 pos, Vector3 anchor, Vector3 offset, bool pinned, bool omniDirectional)
        {
            //
            // We avoid unary negation for performance benefits (aka why this is written so verbose)
            //
            
            if (pinned)
            {
                if (omniDirectional)
                {
                    return (pos + anchor) + offset;
                }
                else
                {
                    return (pos + anchor) - offset;
                }
            }
            else
            {
                if (omniDirectional)
                {
                    return pos + offset;
                }
                else
                {
                    return pos - offset;
                }
            }
        }

        internal static Vector3 GetGerstnerOrigin(Vector3 origin, Vector3 anchor, Vector3 offset, bool pinned, bool omniDirectional)
        {
            //
            // We avoid unary negation for performance benefits (aka why this is written so verbose)
            //
            
            if (pinned)
            {
                if (omniDirectional)
                {
                    return (origin - anchor) + offset;
                }
                else
                {
                    return (origin - anchor) - offset;
                }
            }
            else
            {
                if (omniDirectional)
                {
                    return origin + offset;
                }
                else
                {
                    return origin - offset;
                }
            }
        }

        // From: https://github.com/Unity-Technologies/BoatAttack/blob/42f9e9e6f2877573382244f8a07968848dbab9b0/Packages/com.verasl.water-system/Scripts/GerstnerWavesJobs.cs
        internal static Vector3 CalcForVertexCPU(
            Vector3 vertex, 
            Vector3 omni,
            Vector3 origin,
            DataWaveGerstner data, 
            float countMulti)
        {
            if (data.Amplitude == 0)
            {
                return Vector3.zero;
            }
            
            //  calculate wind direction - TODO - currently radians
            Vector3 windOmniInput = CustomMultiply(CustomSubtract(omni, origin), data.OmniDirection);
            Vector3 windDir = data.WindDirectionInput + windOmniInput;
            windDir = CustomNormalize(windDir);
            float dir = Vector3.Dot(windDir, CustomSubtract(omni, (origin * data.OmniDirection)));

            ////////////////////////////position output calculations/////////////////////////
            float calc = dir * data.Wavelength - data.Offset; // the wave calculation
            double cosCalc = Math.Cos(calc); // cosine version(used for horizontal undulation)
            double sinCalc = Math.Sin(calc); // sin version(used for vertical undulation)

            // calculate the offsets for the current point
            vertex.x = data.Qi * data.Amplitude * windDir.x * (float)cosCalc;
            vertex.z = data.Qi * data.Amplitude * windDir.z * (float)cosCalc;
            vertex.y = (float)sinCalc * data.Amplitude * countMulti; // the height is divided by the number of waves 

            return vertex;
        }
        
        #endregion SOLVE


        #region UTILITY

        // For slight speed increase
        // From: https://forum.unity.com/threads/vector3-normalized-vs-normalize.274905/
        private static Vector3 CustomNormalize(Vector3 v)
        {
            double m = Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            if (m > 9.99999974737875E-06)
            {
                float fm = (float)m;
                v.x /= fm;
                v.y /= fm;
                v.z /= fm;
                return v;
            }
            else
                return Vector3.zero;
        }

        private static Vector3 CustomMultiply(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }
        
        private static Vector3 CustomMultiply(Vector3 v1, float f)
        {
            return new Vector3(v1.x * f, v1.y * f, v1.z * f);
        }

        private static Vector3 CustomSubtract(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        #endregion UTILITY
    }
}