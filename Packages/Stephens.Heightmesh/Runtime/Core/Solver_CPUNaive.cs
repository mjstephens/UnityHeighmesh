using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Solver_CPUNaive : Solver
    {
        #region VARIABLES

        private readonly List<Vector3> _meshVertices = new List<Vector3>();

        #endregion VARIABLES


        #region SOLVE

        internal override void Solve(
            Heightmesh heightmesh, 
            Vector3[] originalVertices, 
            List<IHeightmeshInput> data, 
            List<DataConfigHeightmeshInput> configs,
            Vector3 simOffset,
            float time)
        {
            base.Solve(heightmesh, originalVertices, data, configs, simOffset, time);
            
            // Copy original vertices for manipulation
            _meshVertices.Clear();
            for (int i = 0; i < originalVertices.Length; i++)
            {
                _meshVertices.Add(originalVertices[i]);
            }
            
            Vector3 meshPosition = heightmesh.transform.position;
            for (int i = 0; i < _meshVertices.Count; i++)
            {
                Vector3 vertex = _meshVertices[i];
                
                if (_sinCount > 0)
                {
                    vertex.y += SolveSinWaves(vertex, meshPosition, _dataWaveSin, _sinCount, simOffset);
                }
                
                if (_gerstnerCount > 0)
                {
                    vertex = SolveGerstnerWaves(vertex, meshPosition, _dataWaveGerstner, _gerstnerCount, simOffset);
                }
            
                if (_noiseCount > 0)
                {
                    vertex.y += SolveNoise(vertex, meshPosition, _dataNoise, _noiseCount, simOffset);
                }

                if (_mapCount > 0)
                {
                    vertex.y = SolveHeightmap(
                        vertex, 
                        _meshVertices[i],
                        heightmesh.DataConfig.SurfaceActualWidth,
                        meshPosition,
                        _dataHeightmap,
                        _dataConfigHeightmap,
                        _mapCount);
                }

                _meshVertices[i] = vertex;
            }
            
            // Update mesh
            heightmesh.Mesh.SetVertices(_meshVertices);
            heightmesh.Mesh.RecalculateNormals();
        }

        private static float SolveSinWaves(
            Vector3 vertex, 
            Vector3 meshPosition,
            List<DataWaveSin> waveData, 
            int count,
            Vector3 offset)
        {
            float y = vertex.y;
            for (int i = 0; i < count; i++)
            {
                if (waveData[i].WorldAnchored)
                {
                    y += meshPosition.y;
                    vertex += meshPosition;
                }
                
                y += WaveSin.CalcForVertexCPU(vertex + offset, waveData[i]);
            }

            return y;
        }

        private static Vector3 SolveGerstnerWaves(
            Vector3 vertex,
            Vector3 meshPosition,
            List<DataWaveGerstner> waveData,
            int count,
            Vector3 offset)
        {
            float waveCountMulti = 1f / count;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = (waveData[i].WorldAnchored ? meshPosition + vertex : vertex) + offset;
                Vector3 omni = pos;
                Vector3 origin = (waveData[i].WorldAnchored ? waveData[i].Origin - meshPosition : waveData[i].Origin) + offset;
                if (waveData[i].WorldAnchored && waveData[i].OmniDirectional)
                {
                    omni = vertex;
                }
                
                vertex += WaveGerstner.CalcForVertexCPU(pos, omni, origin, waveData[i], waveCountMulti);
            }
            
            return vertex;
        }

        private static float SolveNoise(
            Vector3 vertex,
            Vector3 meshPosition,
            List<DataNoise> data,
            int count,
            Vector3 offset)
        {
            float y = vertex.y;
            for (int i = 0; i < count; i++)
            {
                y += Noise.CalcForVertexCPU(vertex + offset, meshPosition, data[i]);
            }

            return y;
        }
        
        private static float SolveHeightmap(
            Vector3 vertex,
            Vector3 vertexOriginal,
            float meshWidth,
            Vector3 meshPosition,
            List<DataHeightmap> data,
            List<DataConfigHeightmap> dataConfig,
            int count)
        {
            float y = vertex.y;
            
            for (int i = 0; i < count; i++)
            {
                y += Heightmap.CalcForVertexCPU(vertex, vertexOriginal, dataConfig[i].Pixels, meshPosition, meshWidth, data[i]);
            }

            return y;
        }

        private static float SolveRippleWaves(
            Vector3 vertex, 
            List<DataHeightwaveRipple> dataRipples, 
            int ripplesCount, 
            float time)
        {
            Vector2 v2 = new Vector2(vertex.x, vertex.z);
            float y = vertex.y;
            
            for (int i = 0; i < ripplesCount; i++)
            {
                y += CalcRipples(v2, dataRipples[i], time);
            }

            return y;
        }

        #endregion SOLVE
        
    }
}