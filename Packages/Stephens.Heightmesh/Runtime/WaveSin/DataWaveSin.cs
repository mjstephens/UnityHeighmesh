using System;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal struct DataWaveSin : IHeightmeshInputData
    {
        internal float Opacity;
        internal Quaternion Direction;
        internal float Amplitude;
        internal float Wavelength;
        internal bool WorldAnchored;

        internal float Offset;
        internal float Time;
    }
}