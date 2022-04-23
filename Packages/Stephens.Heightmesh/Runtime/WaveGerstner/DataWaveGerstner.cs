using UnityEngine;

namespace Stephens.Heightmesh
{
    public struct DataWaveGerstner : IHeightmeshInputData
    {
        // Config data
        internal float Amplitude;
        internal Vector3 Origin;
        internal bool OmniDirectional;
        internal bool WorldAnchored;

        // Runtime calculated
        internal float Offset;
        internal float Qi;
        internal float Wavelength;
        internal Vector3 WindDirectionInput;
    }
}