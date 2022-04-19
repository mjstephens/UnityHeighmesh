using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Stephens.Heightmesh
{
    // [BurstCompile]
    // struct JobUpdateHeightmesh : IJobParallelFor
    // {
    //     internal NativeArray<float3> Vertices;
    //     [ReadOnly] [NativeDisableParallelForRestriction] internal NativeArray<DataHeightwaveRipple> WaveData;
    //     [ReadOnly] internal int WaveCount;
    //     [ReadOnly] internal float Time;
    //
    //     public void Execute(int index)
    //     {
    //         float3 p = Vertices[index];
    //         float y = 0.0f;
    //         for (int i = 0; i < WaveCount; i++)
    //         {
    //             float3 pos = WaveData[i].Position;
    //             float2 p1 = new float2 (p.x, p.z);
    //             float2 p2 = new float2 (pos.x, pos.z);
    //             float dist = math.distance (p1,p2);
    //             if (dist > 4)
    //                 continue;
    //
    //             float strength = MapValue(
    //                 dist, 
    //                 0f, 
    //                 WaveData[i].Distance,
    //                 WaveData[i].LifetimeRemaining, 0f);
    //             if (strength > 0)
    //             {
    //                 y += math.sin(dist - Time) * strength * WaveData[i].Strength;
    //             }
    //         }
    //         p.y = y;
    //         Vertices[index] = p;
    //     }
    //     
    //     [BurstCompile]
    //     static float MapValue(float refValue, float refMin, float refMax, float targetMin, float targetMax)
    //     {
    //         return targetMin + (refValue - refMin) * (targetMax - targetMin) / (refMax - refMin);
    //     }
    // }
}