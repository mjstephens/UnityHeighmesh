using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Solver_CPUNaive : Solver
    {
        #region VARIABLES

        // Cached properties
        private DataVertexGerstner _gerstnerData;
        
        #endregion VARIABLES
        
        
        #region CONSTRUCTION

        internal Solver_CPUNaive(SimulationSolverMode mode) : base(mode)
        {
            
        }

        #endregion CONSTRUCTION


        #region SOLVE

        internal override void SolvePositions(
            Vector3[] solvePositions,
            Vector3 anchorPosition,
            float time,
            bool debug = false)
        {
            // Reset lists for this solver iteration
            SolvedPositions.Clear();
            SolvedPositions.AddRange(solvePositions);

            SolvedOffsets.Clear();
            for (int i = 0; i < solvePositions.Length; i++)
            {
                SolvedOffsets.Add(Vector2.zero);
            }
            
            int sinCount = Data.CountSin;
            int gerstnerCount = Data.CountGerstner;
            int noiseCount = Data.CountNoise;

            // We iterate and solve for every provided position
            for (int i = 0; i < SolvedPositions.Count; i++)
            {
                Vector3 vertex = SolvedPositions[i];
                Vector2 offset = SolvedOffsets[i];
                
                if (sinCount > 0)
                {
                    vertex.y += SolveSinWaves(vertex, anchorPosition, Data.DataSin, sinCount, SimulationOffset);
                }
                
                if (gerstnerCount> 0)
                {
                    _gerstnerData = SolveGerstnerWaves(
                        vertex,
                        anchorPosition, 
                        Data.DataGerstner, 
                        gerstnerCount, 
                        SimulationOffset, debug);
                    
                    vertex.y = _gerstnerData.Position.y;
                    offset += _gerstnerData.XZOffset;
                }
            
                if (noiseCount > 0)
                {
                    vertex.y += SolveNoise(vertex, anchorPosition, Data.DataNoise, noiseCount, SimulationOffset);
                }

                // if (_mapCount > 0)
                // {
                //     vertex.y = SolveHeightmap(
                //         vertex, 
                //         _meshVertices[i],
                //         heightmesh.DataConfig.SurfaceActualWidth,
                //         meshPosition,
                //         _dataHeightmap,
                //         _dataConfigHeightmap,
                //         _mapCount);
                // }

                SolvedOffsets[i] = offset;
                SolvedPositions[i] = new Vector3(vertex.x + offset.x, vertex.y, vertex.z + offset.y);
            }
        }

        private static float SolveSinWaves(
            Vector3 position, 
            Vector3 anchorPosition,
            DataWaveSin[] waveData, 
            int count,
            Vector3 offset)
        {
            float y = position.y;
            for (int i = 0; i < count; i++)
            {
                if (waveData[i].WorldAnchored)
                {
                    y += anchorPosition.y;
                    position += anchorPosition;
                }
                
                y += WaveSin.CalcForVertexCPU(position - offset, waveData[i]);
            }

            return y;
        }

        private static DataVertexGerstner SolveGerstnerWaves(
            Vector3 position,
            Vector3 anchorPosition,
            DataWaveGerstner[] waveData,
            int count,
            Vector3 offset,
            bool debug = false)
        {
            float waveCountMulti = 1f / count;
            Vector2 calcOffset = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = WaveGerstner.GetGerstnerPosition(
                    position,
                    anchorPosition,
                    offset, 
                    waveData[i].WorldAnchored,
                    waveData[i].OmniDirectional);

                Vector3 origin = WaveGerstner.GetGerstnerOrigin(
                    waveData[i].Origin, 
                    anchorPosition, 
                    offset, 
                    waveData[i].WorldAnchored,
                    waveData[i].OmniDirectional);

                Vector3 omni = pos;
                if (waveData[i].WorldAnchored && waveData[i].OmniDirectional)
                {
                    omni = position;
                }
                
                Vector3 thisData = WaveGerstner.CalcForVertexCPU(pos, omni, origin, waveData[i], waveCountMulti);
                position += thisData;
                calcOffset += new Vector2(thisData.x, thisData.z);
            }

            //return position;
            return new DataVertexGerstner()
            {
                Position = position,
                XZOffset = calcOffset
            };
        }

        private static float SolveNoise(
            Vector3 vertex,
            Vector3 meshPosition,
            DataNoise[] data,
            int count,
            Vector3 offset)
        {
            float y = vertex.y;
            for (int i = 0; i < count; i++)
            {
                y += Noise.CalcForVertexCPU(vertex - offset, meshPosition, data[i]);
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