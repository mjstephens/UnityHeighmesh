using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Solver_CPUNaive : Solver
    {
        #region VARIABLES

        private readonly Vector3[] _verticesCopy;

        #endregion VARIABLES
        
        
        #region CONSTRUCTOR

        internal Solver_CPUNaive(Heightmesh heightmesh) : base(heightmesh)
        {
            _verticesCopy = new Vector3[heightmesh.Mesh.vertexCount];
        }

        #endregion CONSTRUCTOR
        
        
        #region SOLVE

        internal override void Solve(
            Vector3 meshPosition,
            Vector3[] originalVertices, 
            List<DataHeightwaveRipple> dataRipples,
            DataWaveSin[] dataWaveSin,
            DataWaveGerstner[] dataWaveGerstner,
            float time)
        {
            int ripplesCount = dataRipples.Count;
            int sinCount = dataWaveSin.Length;
            int gerstnerCount = dataWaveGerstner.Length;

            for (int i = 0; i < originalVertices.Length; i++)
            {
                _verticesCopy[i] = originalVertices[i];
            }

            bool doSolveRipples = ripplesCount > 0;
            bool doSolveSin = sinCount > 0;
            bool doSolveGerstner = gerstnerCount > 0;
            for (int i = 0; i < _verticesCopy.Length; i++)
            {
                if (doSolveSin)
                {
                    _verticesCopy[i].y += SolveSinWaves(_verticesCopy[i], meshPosition, dataWaveSin, sinCount, time);
                }

                if (doSolveGerstner)
                {
                    _verticesCopy[i] = SolveGerstnerWaves(_verticesCopy[i], meshPosition, dataWaveGerstner, gerstnerCount, time);
                }
                
                if (doSolveRipples)
                {
                    _verticesCopy[i].y = SolveRippleWaves(_verticesCopy[i], dataRipples, ripplesCount, time);
                }
            }
            
            // Update mesh
            _heightmesh.Mesh.SetVertices(_verticesCopy);
            _heightmesh.Mesh.RecalculateNormals();
        }

        private static float SolveSinWaves(
            Vector3 vertex, 
            Vector3 meshPosition,
            DataWaveSin[] waveData, 
            int count, 
            float time)
        {
            float y = vertex.y;
            for (int i = 0; i < count; i++)
            {
                if (waveData[i].WorldAnchored)
                {
                    y += meshPosition.y;
                    vertex += meshPosition;
                }
                
                y += CalcSinWave(vertex, waveData[i], time);
            }

            return y;
        }

        private static Vector3 SolveGerstnerWaves(
            Vector3 vertex,
            Vector3 meshPosition,
            DataWaveGerstner[] waveData,
            int count,
            float time)
        {
            float waveCountMulti = 1f / count;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = waveData[i].WorldAnchored ? vertex + meshPosition : vertex;
                Vector3 omni = pos;
                if (waveData[i].WorldAnchored && waveData[i].OmniDirectional)
                {
                    omni = vertex;
                }
                
                vertex += CalcGerstnerWave(pos, omni, waveData[i], waveCountMulti, time);
            }
            
            return vertex;
        }
        
        private static float SolveRippleWaves(
            Vector3 vertex, 
            List<DataHeightwaveRipple> dataRipples, 
            int ripplesCount, 
            float time)
        {
            Vector2 vertexPosition = new Vector2 (vertex.x, vertex.z);
            float y = vertex.y;
            
            for (int i = 0; i < ripplesCount; i++)
            {
                y += CalcRipples(vertexPosition, dataRipples[i], time);
            }

            return y;
        }

        #endregion SOLVE
    }
}