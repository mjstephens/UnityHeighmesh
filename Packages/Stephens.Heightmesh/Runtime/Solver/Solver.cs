using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal abstract class Solver
    {
        #region VARIABLES

        internal Vector3 SimulationOffset { get; set; }
        
        protected readonly SimulationSolverMode _mode;
        public readonly List<Vector3> SolvedPositions = new List<Vector3>();
        public readonly List<Vector2> SolvedOffsets = new List<Vector2>();

        internal DataSolver Data { get; set; }
        
        protected readonly List<DataConfigHeightmap> _dataConfigHeightmap = new List<DataConfigHeightmap>();
        protected readonly List<DataHeightmap> _dataHeightmap = new List<DataHeightmap>();
        protected int _mapCount;
        protected static int _solvedSin;
        protected static int _solvedGerstner;
        protected static int _solvedNoise;

        #endregion VARIABLES


        #region CONSTRUCTION

        internal Solver(SimulationSolverMode mode)
        {
            _mode = mode;
        }

        #endregion CONSTRUCTION


        #region SOLVE

        internal static void Reset()
        {
            _solvedSin = 0;
            _solvedGerstner = 0;
            _solvedNoise = 0;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="solvePositions">The chunk of positions for which to solve</param>
        /// <param name="anchorPosition">The world reference position for this group of solve positions</param>
        /// <param name="time">Current simulation time</param>
        internal abstract void SolvePositions(
            Vector3[] solvePositions,
            Vector3 anchorPosition,
            float time,
            bool debug = false);

        protected static float CalcRipples(
            Vector2 vertex, 
            DataHeightwaveRipple ripple, 
            float time)
        {
            float dist =  Vector2.Distance (vertex, ripple.Position);
            if (dist > ripple.Distance)
                return 0;
		      
            float strength = Heightmesh.MapValue(
                dist, 
                0f, 
                ripple.Distance,
                ripple.LifetimeRemaining, 0f);
            if (strength > 0)
            {
                return Mathf.Sin(dist - (time * ripple.Speed)) * strength * ripple.Strength;
            }

            return 0;
        }

        #endregion SOLVE
    }
}