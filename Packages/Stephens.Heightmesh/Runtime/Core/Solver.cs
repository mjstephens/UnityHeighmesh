using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal abstract class Solver
    {
        #region VARIABLES

        protected readonly Heightmesh _heightmesh;

        #endregion VARIABLES
        
        
        #region CONSTRUCTOR

        internal Solver(Heightmesh heightmesh)
        {
            _heightmesh = heightmesh;
        }
 
        #endregion CONSTRUCTOR
        
        
        #region SOLVE

        internal abstract void Solve(
            Vector3 meshPosition,
            Vector3[] originalVertices,
            List<DataHeightwaveRipple> dataRipples,
            DataWaveSin[] dataWaveSin,
            DataWaveGerstner[] dataWaveGerstner,
            float time);

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

        protected static float CalcSinWave(
            Vector3 vertex, 
            DataWaveSin wave, 
            float time)
        {
            Vector3 dir = wave.Direction * vertex;
            return Mathf.Sin(time * wave.Speed + (dir.z * wave.Amplitude)) * wave.Strength;
        }
        
        protected static Vector3 CalcGerstnerWave(
            Vector3 vertex, 
            Vector3 omni,
            DataWaveGerstner wave, 
            float countMulti, 
            float time)
        {
            float amplitude = wave.Amplitude;
            Vector3 omniPos = wave.Origin;
            float omniDir = wave.OmniDirectional ? 1 : 0;
            
            //  calculate wind direction - TODO - currently radians
            Vector3 windOmniInput = (omni - omniPos) * omniDir;
            Vector3 windDir = wave.WindDirectionInput + windOmniInput;
            windDir = Vector3.Normalize(windDir);
            float dir = Vector3.Dot(windDir, omni - omniPos * omniDir);

            ////////////////////////////position output calculations/////////////////////////
            float calc = dir * wave.Wavelength + -time * wave.WSpeed; // the wave calculation
            float cosCalc = Mathf.Cos(calc); // cosine version(used for horizontal undulation)
            float sinCalc = Mathf.Sin(calc); // sin version(used for vertical undulation)

            // calculate the offsets for the current point
            vertex.x = wave.Qi * amplitude * windDir.x * cosCalc;
            vertex.z = wave.Qi * amplitude * windDir.z * cosCalc;
            vertex.y = sinCalc * amplitude * countMulti; // the height is divided by the number of waves 

            return vertex;
        }

        #endregion SOLVE
    }
}