using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Heightmesh_",
        menuName = "Heightmesh/Heightmesh")]
    public class DataConfigHeightmesh : ScriptableObject
    {
        [Header("Quality")]
        [SerializeField] internal HeightmeshUpdateMode Mode = HeightmeshUpdateMode.CPU_JobBurstThreaded;
        [SerializeField] internal float UpdateRateRipples = 0.1f;
        

        [Header("Surface")]
        [SerializeField] internal float UVScale = 100;
        [SerializeField] internal float SurfaceActualWidth = 50; 
        [SerializeField] internal float SurfaceActualLength = 50;
        [SerializeField] internal int SurfaceWidthPoints = 100;
        [SerializeField] internal int SurfaceLengthPoints = 100;
    }
}