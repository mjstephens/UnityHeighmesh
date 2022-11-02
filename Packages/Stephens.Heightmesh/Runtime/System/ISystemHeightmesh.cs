using System.Collections.Generic;

namespace Stephens.Heightmesh
{
    public interface ISystemHeightmesh
    {
        #region PROPERTIES

        List<DataConfigHeightmeshInput> Inputs { get; }
        
        #endregion PROPERTIES
    }
}