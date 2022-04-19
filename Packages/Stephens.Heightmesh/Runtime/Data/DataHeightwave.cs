using Unity.Mathematics;

namespace Stephens.Heightmesh
{
    [System.Serializable]
    public struct DataHeightwave
    {
        public float amplitude; // height of the wave in units(m)
        public float direction; // direction the wave travels in degrees from Z+
        public float wavelength; // distance between crest>crest
        public float2 origin; // Omi directional point of origin
        public float onmiDir; // Is omni?

        public DataHeightwave(float amp, float dir, float length, float2 org, bool omni)
        {
            amplitude = amp;
            direction = dir;
            wavelength = length;
            origin = org;
            onmiDir = omni ? 1 : 0;
        }
    }
}