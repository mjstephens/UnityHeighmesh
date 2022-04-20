using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Solver_CPUJob : Solver
    {
        #region CONSTRUCTOR

        internal Solver_CPUJob(Heightmesh heightmesh) : base(heightmesh)
        {
            
        }

        #endregion CONSTRUCTOR
        
        
        #region SOLVE

        internal override void Solve(
            Vector3 meshPosition,
            Vector3[] originalVertices, 
            float meshWidth,
            List<DataHeightwaveRipple> dataRipples, 
            DataWaveSin[] dataWaveSin,
            DataWaveGerstner[] dataWaveGerstner,
            DataNoise[] dataNoise,
            float time)
        {
            NativeArray<DataHeightwaveRipple> rippleData = new NativeArray<DataHeightwaveRipple>(dataRipples.ToArray(), Allocator.TempJob);
            NativeArray<DataWaveSin> sinWaveData = new NativeArray<DataWaveSin>(dataWaveSin, Allocator.TempJob);
            NativeArray<DataWaveGerstner> gerstnerWaveData = new NativeArray<DataWaveGerstner>(dataWaveGerstner, Allocator.TempJob);
            NativeArray<DataNoise> noiseData = new NativeArray<DataNoise>(dataNoise, Allocator.TempJob);
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(originalVertices, Allocator.TempJob);
            JobHeightmeshSolver job = new JobHeightmeshSolver
            {
                Vertices = vertices,
                MeshPosition = meshPosition,
                RippleData = rippleData,
                RippleCount = dataRipples.Count,
                SinWaveData = sinWaveData,
                SinWaveCount = dataWaveSin.Length,
                GerstnerWaveData = gerstnerWaveData,
                GerstnerWaveCount = dataWaveGerstner.Length,
                NoiseData = noiseData,
                NoiseCount = dataNoise.Length,
                Time = time
            };
	        
            switch (_heightmesh.DataConfig.Mode)
            {
                case HeightmeshUpdateMode.CPU_Job:
                {
                    // Directly execute the vertex modification code, on a single thread.
                    // This will not get into Burst-compiled code.
                    for (var i = 0; i < vertices.Length; ++i)
                        job.Execute(i);
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

            // Update mesh vertex positions from the NativeArray we calculated above.
            _heightmesh.Mesh.SetVertices(vertices);
	        
            // Recalculate mesh normals. Note: our mesh is a heightmap and we could use a more
            // efficient method of normal calculation, similar to what the GPU code path does.
            // Just use a simple generic function here for simplicity.
            _heightmesh.Mesh.RecalculateNormals();
            
            // Dispose of the native arrays now that we're done
            rippleData.Dispose();
            sinWaveData.Dispose();
            gerstnerWaveData.Dispose();
            noiseData.Dispose();
            vertices.Dispose();
        }

        #endregion SOLVE


        #region JOB

        [BurstCompile(FloatPrecision.Low,FloatMode.Fast)]
        private struct JobHeightmeshSolver : IJobParallelFor
        {
            internal NativeArray<Vector3> Vertices;
            [ReadOnly] internal Vector3 MeshPosition;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataHeightwaveRipple> RippleData;
            [ReadOnly] internal int RippleCount;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataWaveSin> SinWaveData;
            [ReadOnly] internal int SinWaveCount;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataWaveGerstner> GerstnerWaveData;
            [ReadOnly] internal int GerstnerWaveCount;
            [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataNoise> NoiseData;
            [ReadOnly] internal int NoiseCount;
            [ReadOnly] internal float Time;
        
            public void Execute(int index)
            {
                Vector3 vertexPosV3 = Vertices[index];
                
                if (SinWaveCount > 0)
                {
                    vertexPosV3.y += SolveSinWaves(vertexPosV3, MeshPosition, SinWaveData, SinWaveCount);
                }

                if (GerstnerWaveCount > 0)
                {
                    vertexPosV3 = SolveGerstnerWaves(vertexPosV3, MeshPosition, GerstnerWaveData, GerstnerWaveCount);
                }
                
                if (NoiseCount > 0)
                {
                    vertexPosV3.y += SolveNoise(vertexPosV3, MeshPosition, NoiseData);
                }
                
                if (RippleCount > 0)
                {
                    vertexPosV3.y = SolveRippleWaves(vertexPosV3, RippleData, RippleCount, Time);
                }

                Vertices[index] = vertexPosV3;
            }

            private static float SolveSinWaves(
                Vector3 vertex, 
                Vector3 meshPosition,
                NativeArray<DataWaveSin> waveData, 
                int count)
            {
                float y = vertex.y;
                for(int i = 0; i < count; i++)
                {
                    if (waveData[i].WorldAnchored)
                    {
                        y += meshPosition.y;
                        vertex += meshPosition;
                    }
                    
                    y += CalcSinWave(vertex, waveData[i]);
                }

                return y;
            }
        }

        private static Vector3 SolveGerstnerWaves(
            Vector3 vertex,
            Vector3 meshPosition,
            NativeArray<DataWaveGerstner> waveData,
            int count)
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
                
                vertex += CalcGerstnerWave(pos, omni, waveData[i], waveCountMulti);
            }
            
            return vertex;
        }
        
        private static float SolveNoise(
            Vector3 vertex,
            Vector3 meshPosition,
            NativeArray<DataNoise> data)
        {
            float y = vertex.y;
            foreach (DataNoise noise in data)
            {
                y += Mathf.PerlinNoise(
                         (vertex.x + meshPosition.x - noise.Offset.x) * noise.Spread,
                         (vertex.z + meshPosition.z - noise.Offset.y) * noise.Spread) 
                     * noise.Strength;
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

        #endregion JOB
    }
}