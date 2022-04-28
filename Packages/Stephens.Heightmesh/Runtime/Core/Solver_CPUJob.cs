using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Solver_CPUJob : Solver
    {
        private NativeArray<Vector2> _vertexOffsets;
        
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
            
            // Allocate native arrays for use in jobs
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(originalVertices, Allocator.TempJob);
            _vertexOffsets = new NativeArray<Vector2>(originalVertices.Length, Allocator.TempJob);
            
            // Solve inputs using jobs + native arrays
            SolveMeshMathInputs(heightmesh, vertices, simOffset);
            SolveMeshMapInputs(heightmesh, vertices);
            SolveVertexOffsetAdditions(heightmesh, vertices);

            // Update mesh
            heightmesh.Mesh.SetVertices(vertices);
            heightmesh.Mesh.RecalculateNormals();
            
            // Dispose of native arrays
            vertices.Dispose();
            _vertexOffsets.Dispose();
        }

        /// <summary>
        /// "Math" inputs are solved in a single job - these are global inputs that don't require specific vertex distance checks, but
        /// rather are solved usinq equations (noise + waves)
        /// </summary>
        private void SolveMeshMathInputs(Heightmesh heightmesh, NativeArray<Vector3> vertices, Vector3 simOffset)
        {
            NativeArray<DataWaveSin> sinWaveData = new NativeArray<DataWaveSin>(_dataWaveSin.ToArray(), Allocator.TempJob);
            var gerstnerWaveData = new NativeArray<DataWaveGerstner>(_dataWaveGerstner.ToArray(), Allocator.TempJob);
            NativeArray<DataNoise> noiseData = new NativeArray<DataNoise>(_dataNoise.ToArray(), Allocator.TempJob);
            
            JobHeightmeshSolverMathInputs job = new JobHeightmeshSolverMathInputs
            {
                Vertices = vertices,
                XZOffsets = _vertexOffsets,
                MeshPosition = heightmesh.transform.position,
                SinWaveData = sinWaveData,
                SinWaveCount = _dataWaveSin.Count,
                GerstnerWaveData = gerstnerWaveData,
                GerstnerWaveCount = _dataWaveGerstner.Count,
                NoiseData = noiseData,
                NoiseCount = _dataNoise.Count,
                SimulationOffset = simOffset
            };
            
            // From: https://github.com/Unity-Technologies/MeshApiExamples/blob/master/Assets/ProceduralWaterMesh/ProceduralWaterMesh.cs
            switch (heightmesh.DataConfig.Mode)
            {
                case HeightmeshUpdateMode.CPU_Job:
                {
                    // Directly execute the vertex modification code, on a single thread.
                    // This will not get into Burst-compiled code.
                    for (var i = 0; i < vertices.Length; ++i)
                    {
                        job.Execute(i);
                    }
                    break;
                }
                case HeightmeshUpdateMode.CPU_JobBurst:
                    // Execute Burst-compiled code, but with inner loop count that is
                    // "all the vertices". Effectively this makes it single threaded.
                    job.Schedule(vertices.Length, vertices.Length).Complete();
                    break;
                case HeightmeshUpdateMode.CPU_JobBurstThreaded:
                    // Execute Burst-compiled code, multi-threaded.
                    job.Schedule(vertices.Length, 16).Complete();
                    break;
            }
            
            sinWaveData.Dispose();
            gerstnerWaveData.Dispose();
            noiseData.Dispose();
        }

        /// <summary>
        /// Heightmesh inputs are solved in a separate job. These require the array of vertices as well of the array of pixel data from
        /// the target heightmesh.
        /// </summary>
        private void SolveMeshMapInputs(Heightmesh heightmesh, NativeArray<Vector3> vertices)
        {
            for (int i = 0; i < _dataHeightmap.Count; i++)
            {
                JobHeightmeshSolverHeightmaps job = new JobHeightmeshSolverHeightmaps()
                {
                    Map = _dataHeightmap[i],
                    Vertices = vertices,
                    MeshWidth = heightmesh.DataConfig.SurfaceActualWidth,
                    MeshPosition = heightmesh.transform.position,
                    Pixels = _dataConfigHeightmap[i].Pixels
                };
                
                switch (heightmesh.DataConfig.Mode)
                {
                    case HeightmeshUpdateMode.CPU_Job:
                        for (var j = 0; j < vertices.Length; ++j)
                        {
                            job.Execute(j);
                        }
                        break;
                    case HeightmeshUpdateMode.CPU_JobBurst: job.Schedule(vertices.Length, vertices.Length).Complete(); break;
                    case HeightmeshUpdateMode.CPU_JobBurstThreaded: job.Schedule(vertices.Length, 16).Complete(); break;
                }
            }
        }

        /// <summary>
        /// Our final job needs to add the vertices with the xz offsets. In this jobified version, the offsets must be calculated separately
        /// and added only after all other input calculations are finished. Otherwise, xz vertex offsets created by the gerstner wave inputs
        /// would throw off subsequent input calculations that rely on the vertices being evenly spaced.
        /// </summary>
        private void SolveVertexOffsetAdditions(Heightmesh heightmesh, NativeArray<Vector3> vertices)
        {
            JobHeightmeshSolverAddVertexOffsets job = new JobHeightmeshSolverAddVertexOffsets()
            {
                Vertices = vertices,
                XZOffsets = _vertexOffsets
            };
            
            switch (heightmesh.DataConfig.Mode)
            {
                case HeightmeshUpdateMode.CPU_Job:
                    for (var j = 0; j < vertices.Length; ++j)
                    {
                        job.Execute(j);
                    }
                    break;
                case HeightmeshUpdateMode.CPU_JobBurst: job.Schedule(vertices.Length, vertices.Length).Complete(); break;
                case HeightmeshUpdateMode.CPU_JobBurstThreaded: job.Schedule(vertices.Length, 16).Complete(); break;
            }
        }

        #endregion SOLVE


        #region MATH INPUTS

        [BurstCompile(FloatPrecision.Low,FloatMode.Fast)]
        private struct JobHeightmeshSolverMathInputs : IJobParallelFor
        {
            internal NativeArray<Vector3> Vertices;
            internal NativeArray<Vector2> XZOffsets;
            [ReadOnly] internal Vector3 MeshPosition;
            [ReadOnly] internal float MeshWidth;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataWaveSin> SinWaveData;
            [ReadOnly] internal int SinWaveCount;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataWaveGerstner> GerstnerWaveData;
            [ReadOnly] internal int GerstnerWaveCount;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataNoise> NoiseData;
            [ReadOnly] internal int NoiseCount;
            [ReadOnly] internal Vector3 SimulationOffset;
        
            public void Execute(int index)
            {
                Vector3 vertex = Vertices[index];
                Vector2 offset = XZOffsets[index];
                
                if (SinWaveCount > 0)
                {
                    vertex.y += SolveSinWaves(vertex, MeshPosition, SinWaveData, SinWaveCount, SimulationOffset);
                }

                if (GerstnerWaveCount > 0)
                {
                    // Gerstner waves require special treatment; since they can offset the xz positions of vertices, they could throw off
                    // subsequent calculations that assume the vertices are evenly spaced. For this reason we store the offsets generated
                    // by gerstner waves separately, and only apply them after all other calculations are completed
                    DataVertexGerstner data = SolveGerstnerWaves(
                        vertex, 
                        MeshPosition, 
                        GerstnerWaveData, 
                        GerstnerWaveCount, 
                        SimulationOffset);
                    
                    vertex.y = data.Position.y;
                    offset += data.XZOffset;
                }
                
                if (NoiseCount > 0)
                {
                    vertex.y += SolveNoise(vertex, MeshPosition, NoiseData, NoiseCount, SimulationOffset);
                }

                Vertices[index] = vertex;
                XZOffsets[index] = offset;
            }
        }
        
        private static float SolveSinWaves(
            Vector3 vertex, 
            Vector3 meshPosition,
            NativeArray<DataWaveSin> waveData, 
            int count,
            Vector3 offset)
        {
            float y = vertex.y;
            for(int i = 0; i < count; i++)
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

        private static DataVertexGerstner SolveGerstnerWaves(
            Vector3 vertex,
            Vector3 meshPosition,
            NativeArray<DataWaveGerstner> waveData,
            int count,
            Vector3 offset)
        {
            float waveCountMulti = 1f / count;
            DataVertexGerstner data = new DataVertexGerstner();
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = (waveData[i].WorldAnchored ? vertex + meshPosition : vertex) + offset;
                Vector3 omni = pos;
                Vector3 origin = (waveData[i].WorldAnchored ? waveData[i].Origin - meshPosition : waveData[i].Origin) + offset;
                if (waveData[i].WorldAnchored && waveData[i].OmniDirectional)
                {
                    omni = vertex;
                }

                Vector3 thisData = WaveGerstner.CalcForVertexCPU(pos, omni, origin, waveData[i], waveCountMulti);
                data = new DataVertexGerstner()
                {
                    Position = data.Position + new Vector3(0, thisData.y, 0),
                    XZOffset = new Vector2(thisData.x, thisData.z)
                };
            }

            return data;
        }
        
        private static float SolveNoise(
            Vector3 vertex,
            Vector3 meshPosition,
            NativeArray<DataNoise> data,
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

        private static float SolveRippleWaves(
            Vector3 vertex, 
            NativeArray<DataHeightwaveRipple> dataRipples, 
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

        #endregion MATH INPUTS


        #region HEIGHTMAPS

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct JobHeightmeshSolverHeightmaps : IJobParallelFor
        {
            internal NativeArray<Vector3> Vertices;
            [ReadOnly] internal DataHeightmap Map;
            [ReadOnly] internal float MeshWidth;
            [ReadOnly] internal Vector3 MeshPosition;
            [ReadOnly] internal NativeArray<float> Pixels;   // Pixels per map

            public void Execute(int index)
            {
                Vector3 vertex = Vertices[index];
                
                vertex.y = SolveHeightmap(
                    vertex, 
                    Pixels,
                    MeshWidth,
                    MeshPosition,
                    Map);

                Vertices[index] = vertex;
            }
        }
        
        private static float SolveHeightmap(
            Vector3 vertex,
            NativeArray<float> pixels,
            float meshWidth,
            Vector3 meshPosition,
            DataHeightmap data)
        {
            float y = vertex.y;
            y += Heightmap.CalcForVertexCPU(vertex, vertex, pixels, meshPosition, meshWidth, data);
            return y;
        }

        #endregion HEIGHTMAPS


        #region FINISH VERTEX CALCULATIONS

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct JobHeightmeshSolverAddVertexOffsets : IJobParallelFor
        {
            internal NativeArray<Vector3> Vertices;
            [ReadOnly] internal NativeArray<Vector2> XZOffsets;

            public void Execute(int index)
            {
                Vector3 vertex = Vertices[index];
                Vector2 offset = XZOffsets[index];
                Vertices[index] = new Vector3(vertex.x + offset.x, vertex.y, vertex.z + offset.y);
            }
        }

        #endregion FINISH VERTEX CALCULATIONS
    }
}