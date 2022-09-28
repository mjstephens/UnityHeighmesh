using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Manager class used in this package demo scene. This should be refactored into project-specific architecture
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class SystemHeightmesh : MonoBehaviour
    {
        #region SINGLETON

        public static SystemHeightmesh Instance;
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                Init();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion SINGLETON
        
        
        #region VARIABLES

        [Header("References")]
        [SerializeField] private DataConfigHeightmeshInput[] _inputData;
        [SerializeField] private SimulationSolverMode _solveMode;

        public DataConfigHeightmeshInput[] Inputs => _inputData;
        public Vector3 CurrentOffset => _simulationOffset;
        
        private Vector3 _simulationOffset;
        private float _time = 0.1f;
        private Solver _solver;
        private DataSolver _solverData;
        private IHeightmeshInput[] _inputs;
        private bool _hasInit;
        private readonly List<Heightmesh> _heightmeshes = new List<Heightmesh>();

        #endregion VARIABLES


        #region INITIALIZATION

        private void Init()
        {
            // Create solver
            _solver = CreateSolver();
            
            // Resolve wave types into classes
            _inputs = new IHeightmeshInput[_inputData.Length];
            for (int i = 0; i < _inputData.Length; i++)
            {
                switch (_inputData[i])
                {
                    case DataConfigWaveGerstner gerstner: _inputs[i] = new WaveGerstner(gerstner); break;
                    case DataConfigWaveSin sin: _inputs[i] = new WaveSin(sin); break;
                    case DataConfigNoise noise: _inputs[i] = new Noise(noise); break;
                    case DataConfigHeightmap map: _inputs[i] = new Heightmap(map); break;
                }
            }

            _solver.Data = GetDataForSolver();
            _hasInit = true;
        }
        
        private Solver CreateSolver()
        {
            switch (_solveMode)
            {
                case SimulationSolverMode.CPU_Naive: 
                    return new Solver_CPUNaive(_solveMode);
                    break;
                case SimulationSolverMode.CPU_Job:
                case SimulationSolverMode.CPU_JobBurst:
                case SimulationSolverMode.CPU_JobBurstThreaded:
                    return new Solver_CPUJob(_solveMode);
                    break;
                case SimulationSolverMode.GPU_Compute:

                    break;
            }
	        
            // Fallback
            return new Solver_CPUNaive(_solveMode);
        }

        #endregion INITIALIZATION


        #region REGISTRATION

        internal void RegisterHeightmesh(Heightmesh heightmesh)
        {
            _heightmeshes.Add(heightmesh);
        }
        
        internal void UnregisterHeightmesh(Heightmesh heightmesh)
        {
            _heightmeshes.Remove(heightmesh);
        }

        #endregion REGISTRATION


        #region API

        /// <summary>
        /// Solves the given positions for the inputs in this system
        /// </summary>
        /// <param name="solvePositions"></param>
        /// <param name="anchorPosition"></param>
        /// <param name="receiver"></param>
        /// <param name="debug"></param>
        public void Solve(
            Vector3[] solvePositions, 
            Vector3 anchorPosition, 
            ISolverResultsReceivable receiver = null, 
            bool debug = false)
        {
            _solver.SolvePositions(solvePositions, anchorPosition, _time, debug);
            receiver?.DoReceiveSolverResults(_solver.SolvedPositions, _solver.SolvedOffsets);
        }

        public void SetSimulationOffset(Vector3 offset)
        {
            _simulationOffset = offset;
        }

        public void IncrementSimulationOffset(Vector3 offset)
        {
            _simulationOffset += offset;
        }

        #endregion API


        #region UPDATE

        private void Update()
        {
            TickInputs();
            TickHeightmesh();
        }
        
        private void TickInputs()
        {
            if (!_hasInit)
                return;
            
            _time += Time.deltaTime;
            _solver.SimulationOffset = _simulationOffset;
            
            // Update inputs
            foreach (IHeightmeshInput input in _inputs)
            {
                input.Tick(Time.deltaTime);
            }

            // Update solver with new data
            _solver.Data = GetDataForSolver();
        }

        private void TickHeightmesh()
        {
            if (!_hasInit)
                return;
            
            foreach (Heightmesh heightmesh in _heightmeshes)
            {
                Solve(heightmesh.OriginalVertices, heightmesh.transform.position, heightmesh);
            }
        }
        
        #endregion UPDATE


        #region DATA

        private DataSolver GetDataForSolver()
        {
            List<DataWaveSin> sinData = new List<DataWaveSin>();
            List<DataWaveGerstner> gerstnerData = new List<DataWaveGerstner>();
            List<DataNoise> noiseData = new List<DataNoise>();
            foreach (IHeightmeshInput input in _inputs)
            {
                switch (input)
                {
                    case WaveGerstner gerstner: gerstnerData.Add(gerstner.Data); break;
                    case WaveSin sin: sinData.Add(sin.Data); break;
                    case Noise noise: noiseData.Add(noise.Data); break;
                }
            }

            _solverData = new DataSolver()
            {
                DataSin = sinData.ToArray(),
                DataGerstner = gerstnerData.ToArray(),
                DataNoise = noiseData.ToArray()
            };
            return _solverData;
        }

        #endregion DATA


        #region CLEANUP

        private void OnDestroy()
        {
            foreach (IHeightmeshInput input in _inputs)
            {
                if (input is Heightmap h)
                {
                    h.Cleanup();
                }
            }
        }

        #endregion CLEANUP
    }
}