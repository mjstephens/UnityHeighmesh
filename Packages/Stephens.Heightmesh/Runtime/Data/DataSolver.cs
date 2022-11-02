namespace Stephens.Heightmesh
{
    /// <summary>
    /// Defines the input data for a solver
    /// </summary>
    internal struct DataSolver
    {
        internal DataWaveSin[] DataSin;
        internal int CountSin;
        internal DataWaveGerstner[] DataGerstner;
        internal int CountGerstner;
        internal DataNoise[] DataNoise;
        internal int CountNoise;
    }
}