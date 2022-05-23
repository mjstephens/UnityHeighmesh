using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    public interface ISolverResultsReceivable
    {
        void DoReceiveSolverResults(List<Vector3> positions, List<Vector2> offsets);
    }
}