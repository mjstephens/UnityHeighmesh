using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Manager class used in this package demo scene. This should be refactored into project-specific architecture
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public abstract class SystemHeightmesh : MonoBehaviour, ISystemHeightmesh
    {
        #region VARIABLES
        
        [Header("Data")]
        [SerializeField] protected DataConfigHeightmesh _heightmeshData;
        [SerializeField] protected List<DataConfigHeightmeshInput> _inputData;
        [SerializeField] protected SimulationSolverMode _solveMode;

        [Header("References")]
        [SerializeField] protected GameObject _prefabHeightmeshObject;

        public List<DataConfigHeightmeshInput> Inputs => _inputData;

        private SimulationSolverMode _currentSolveMode;
        private Vector3 _simulationOffset;
        private float _time = 0.1f;
        private Solver _solver;
        private DataSolver _solverData;
        private IHeightmeshInput[] _inputs;
        private bool _hasInit;
        
        // Wave data caches
        private const int CONST_MaxWaveData = 5;
        private readonly DataWaveSin[] dataSinCache = new DataWaveSin[CONST_MaxWaveData];
        private readonly DataWaveGerstner[] dataGerstCache = new DataWaveGerstner[CONST_MaxWaveData];
        private readonly DataNoise[] dataNoiseCache = new DataNoise[CONST_MaxWaveData];

        #endregion VARIABLES


        #region INITIALIZATION

        protected void Awake()
        {
            SpawnHeightmeshObjects();
            Setup();
        }

        protected abstract void SpawnHeightmeshObjects();

        /// <summary>
        /// Creates heightmesh solver and caches inputs
        /// </summary>
        private void Setup()
        {
            // Create solver
            _currentSolveMode = _solveMode;
            _solver = CreateSolver();
            
            // Trim inputs to max amount
            while (_inputData.Count > CONST_MaxWaveData)
            {
                _inputData.RemoveAt(_inputData.Count - 1);
            }
            
            // Resolve wave types into classes
            _inputs = new IHeightmeshInput[_inputData.Count];
            for (int i = 0; i < _inputData.Count; i++)
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
            switch (_currentSolveMode)
            {
                case SimulationSolverMode.CPU_Naive: 
                    return new Solver_CPUNaive(_currentSolveMode);
                    break;
                case SimulationSolverMode.CPU_Job:
                case SimulationSolverMode.CPU_JobBurst:
                case SimulationSolverMode.CPU_JobBurstThreaded:
                    return new Solver_CPUJob(_currentSolveMode);
                    break;
                case SimulationSolverMode.GPU_Compute:

                    break;
            }
	        
            // Fallback
            return new Solver_CPUNaive(_currentSolveMode);
        }

        #endregion INITIALIZATION


        #region VALIDATION

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            
            _currentSolveMode = _solveMode;
            Setup();
        }

        #endregion VALIDATION


        #region API

        

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

        protected virtual void Update()
        {
            TickInputs();
        }
        
        private void TickInputs()
        {
            if (!_hasInit || _solver == null)
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

        protected void TickHeightmesh(Heightmesh[] heightmeshes)
        {
            if (!_hasInit)
                return;

            foreach (var heightmesh in heightmeshes)
            {
                if (heightmesh.gameObject.activeSelf && _solver != null)
                { 
                    Solver.Reset();
                    _solver.SolvePositions(heightmesh.OriginalVertices, heightmesh.transform.position, _time);
                    ((ISolverResultsReceivable)heightmesh).DoReceiveSolverResults(_solver.SolvedPositions, _solver.SolvedOffsets);
                }
            }
        }
        
        #endregion UPDATE


        #region DATA
        
        private DataSolver GetDataForSolver()
        {
            // Clear old arrays
            for (int i = 0; i < dataSinCache.Length; i++)
                dataSinCache[i] = new DataWaveSin();
            for (int i = 0; i < dataGerstCache.Length; i++)
                dataGerstCache[i] = new DataWaveGerstner();
            for (int i = 0; i < dataNoiseCache.Length; i++)
                dataNoiseCache[i] = new DataNoise();
            
            // Populate with new items
            int cSin = 0, cGerst = 0, cNoise = 0;
            for (int i = 0; i < _inputs.Length; i++)
            {
                switch (_inputs[i])
                {
                    case WaveGerstner gerstner: dataGerstCache[cGerst] = gerstner.Data; cGerst++; break;
                    case WaveSin sin: dataSinCache[cSin] = sin.Data; cSin++; break;
                    case Noise noise: dataNoiseCache[cNoise] = noise.Data; cNoise++; break;
                }
            }

            _solverData.DataSin = dataSinCache;
            _solverData.CountSin = cSin;
            _solverData.DataGerstner = dataGerstCache;
            _solverData.CountGerstner = cGerst;
            _solverData.DataNoise = dataNoiseCache;
            _solverData.CountNoise = cNoise;
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