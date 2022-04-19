using UnityEngine;

namespace Stephens.Heightmesh
{
    public struct DataWaveGerstner
    {
        // Config data
        internal float Amplitude;
        internal Vector3 Origin;
        internal bool OmniDirectional;
        internal bool WorldAnchored;

        // Runtime calculated
        internal float Qi;
        internal float Wavelength;
        internal float WSpeed;
        internal Vector3 WindDirectionInput;
    }
}