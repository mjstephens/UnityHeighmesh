using System;
using UnityEngine;

namespace Stephens.Heightmesh
{
    [Serializable]
    internal struct DataHeightmapModification
    {
        [SerializeField] internal Vector2 Offset;
        [SerializeField] internal float Strength;
    }
}