using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class JobGerstnerHeightwave
    {
        //General variables
//         private bool _firstFrame = true;
//         private bool _processing;
//         private int _waveCount;
//         private NativeArray<DataHeightwave> _waveData; // Wave data from the water system
//
//         //Details for Buoyant Objects
//         private NativeArray<float3> _positions;
//         private int _positionCount;
//         private NativeArray<float3> _wavePos;
//         private NativeArray<float3> _waveNormal;
//         private JobHandle _waterHeightHandle;
//         static readonly Dictionary<int, int2> Registry = new Dictionary<int, int2>();
//
//         internal void Init(DataConfigHeightwave[] waves)
//         {
//             if(Debug.isDebugBuild)
//                 Debug.Log("Initializing Gerstner Waves Jobs");
//             //Wave data
//             _waveCount = waves.Length;
//             _waveData = new NativeArray<DataHeightwave>(_waveCount, Allocator.Persistent);
//             for (int i = 0; i < _waveData.Length; i++)
//             {
//                 _waveData[i] = waves[i].Wave;
//             }
//
//             _positions = new NativeArray<float3>(4096, Allocator.Persistent);
//             _wavePos = new NativeArray<float3>(4096, Allocator.Persistent);
//             _waveNormal = new NativeArray<float3>(4096, Allocator.Persistent);
//         }
//
//         public void Cleanup()
//         {
//             if(Debug.isDebugBuild)
//                 Debug.Log("Cleaning up Gerstner Wave Jobs");
//             _waterHeightHandle.Complete();
//
//             //Cleanup native arrays
//             _waveData.Dispose();
//             _positions.Dispose();
//             _wavePos.Dispose();
//             _waveNormal.Dispose();
//         }
//
//         public void UpdateSamplePoints(ref NativeArray<float3> samplePoints, int guid)
//         {
//             CompleteJobs();
//
//             if (Registry.TryGetValue(guid, out var offsets))
//             {
//                 for (int i = offsets.x; i < offsets.y; i++) _positions[i] = samplePoints[i - offsets.x];
//             }
//             else
//             {
//                 if (_positionCount + samplePoints.Length >= _positions.Length) 
//                     return;
//                 
//                 offsets = new int2(_positionCount, _positionCount + samplePoints.Length);
//                 Registry.Add(guid, offsets);
//                 _positionCount += samplePoints.Length;
//             }
//         }
//
//         public void GetData(int guid, ref float3[] outPos, ref float3[] outNorm)
//         {
//             if (!Registry.TryGetValue(guid, out int2 offsets)) return;
//             
//             _wavePos.Slice(offsets.x, offsets.y - offsets.x).CopyTo(outPos);
//             if(outNorm != null)
//                 _waveNormal.Slice(offsets.x, offsets.y - offsets.x).CopyTo(outNorm);
//         }
//
//         // Height jobs for the next frame
//         public void UpdateHeights()
//         {
//             if (_processing) return;
//             
//             _processing = true;
//
// #if STATIC_EVERYTHING
//             float t = 0.0f;
// #else
//             float t = Time.time;
// #endif
//
//             // Buoyant Object Job
//             HeightJob waterHeight = new HeightJob()
//             {
//                 WaveData = _waveData,
//                 Position = _positions,
//                 OffsetLength = new int2(0, _positions.Length),
//                 Time = t,
//                 OutPosition = _wavePos,
//                 OutNormal = _waveNormal
//             };
//                 
//             _waterHeightHandle = waterHeight.Schedule(_positionCount, 32);
//                 
//             JobHandle.ScheduleBatchedJobs();
//
//             _firstFrame = false;
//         }
//
//         private void CompleteJobs()
//         {
//             if (_firstFrame || !_processing) return;
//             
//             _waterHeightHandle.Complete();
//             _processing = false;
//         }
//
//         // Gerstner Height C# Job
//         [BurstCompile]
//         private struct HeightJob : IJobParallelFor
//         {
//             [ReadOnly]
//             public NativeArray<DataHeightwave> WaveData; // wave data stroed in vec4's like the shader version but packed into one
//             [ReadOnly]
//             public NativeArray<float3> Position;
//
//             [WriteOnly]
//             public NativeArray<float3> OutPosition;
//             [WriteOnly]
//             public NativeArray<float3> OutNormal;
//
//             [ReadOnly]
//             public float Time;
//             [ReadOnly]
//             public int2 OffsetLength;
//
//             // The code actually running on the job
//             public void Execute(int i)
//             {
//                 if (i < OffsetLength.x || i >= OffsetLength.y - OffsetLength.x) return;
//                 
//                 float waveCountMulti = 1f / WaveData.Length;
//                 float3 wavePos = new float3(0f, 0f, 0f);
//                 float3 waveNorm = new float3(0f, 0f, 0f);
//
//                 for (int wave = 0; wave < WaveData.Length; wave++) // for each wave
//                 {
//                     // Wave data vars
//                     float2 pos = Position[i].xz;
//
//                     float amplitude = WaveData[wave].amplitude;
//                     float direction = WaveData[wave].direction;
//                     float wavelength = WaveData[wave].wavelength;
//                     float2 omniPos = WaveData[wave].origin;
//                     ////////////////////////////////wave value calculations//////////////////////////
//                     float w = 6.28318f / wavelength; // 2pi over wavelength(hardcoded)
//                     float wSpeed = math.sqrt(9.8f * w); // frequency of the wave based off wavelength
//                     const float peak = 0.8f; // peak value, 1 is the sharpest peaks
//                     float qi = peak / (amplitude * w * WaveData.Length);
//
//                     float2 windDir = new float2(0f, 0f);
//                     direction = math.radians(direction); // convert the incoming degrees to radians
//                     float2 windDirInput = new float2(math.sin(direction), math.cos(direction)) * (1 - WaveData[wave].onmiDir); // calculate wind direction - TODO - currently radians
//                     float2 windOmniInput = (pos - omniPos) * WaveData[wave].onmiDir;
//
//                     windDir += windDirInput;
//                     windDir += windOmniInput;
//                     windDir = math.normalize(windDir);
//                     float dir = math.dot(windDir, pos - (omniPos * WaveData[wave].onmiDir));
//
//                     ////////////////////////////position output calculations/////////////////////////
//                     float calc = dir * w + -Time * wSpeed; // the wave calculation
//                     float cosCalc = math.cos(calc); // cosine version(used for horizontal undulation)
//                     float sinCalc = math.sin(calc); // sin version(used for vertical undulation)
//
//                     // calculate the offsets for the current point
//                     wavePos.x += qi * amplitude * windDir.x * cosCalc;
//                     wavePos.z += qi * amplitude * windDir.y * cosCalc;
//                     wavePos.y += sinCalc * amplitude * waveCountMulti; // the height is divided by the number of waves 
//
//                     ////////////////////////////normal output calculations/////////////////////////
//                     float wa = w * amplitude;
//                     // normal vector
//                     float3 norm = new float3(-(windDir.xy * wa * cosCalc),
//                         1 - (qi * wa * sinCalc));
//                     waveNorm += (norm * waveCountMulti) * amplitude;
//                 }
//                 OutPosition[i] = wavePos;
//                 OutNormal[i] = math.normalize(waveNorm.xzy);
//             }
//         }
    }
}