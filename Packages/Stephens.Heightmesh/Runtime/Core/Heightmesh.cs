using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Defines a mesh whose vertices can adapt to match a grid of heights
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Heightmesh : MonoBehaviour
    {
        #region VARIABLES

        [Header("Data")]
        [SerializeField] private DataConfigHeightmesh _configData;

        [SerializeField] private DataConfigWaveSin[] _dataSinWaves;
        [SerializeField] private DataConfigWaveGerstner[] _dataGerstnerWaves;
        [SerializeField] private DataConfigRipple _dataConfigRipples;

        [Header("Ripple Waves")]
        [SerializeField] private Transform[] _waveSources;


        private Solver _solver;
        private DataHeightmesh _data;

        internal DataConfigHeightmesh DataConfig => _configData;
        private Vector3[] _originalVertices;
        private MeshFilter _meshFilter;
        internal Mesh Mesh { get; set; }
        private float _localTime;
        private readonly List<DataHeightwaveRipple> _dataRipples = new List<DataHeightwaveRipple>();
        private readonly List<DataWaveSin> _dataSin = new List<DataWaveSin>();
        private readonly List<DataWaveGerstner> _dataGerstner = new List<DataWaveGerstner>();

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Awake()
        {
	        foreach (DataConfigWaveSin data in _dataSinWaves)
	        {
		        _dataSin.Add(new DataWaveSin()
		        {
			        Direction = Quaternion.Euler(Vector3.up * data.Direction),
			        Amplitude = data.Amplitude,
			        Wavelength = data.Wavelength,
			        Offset = 0,
			        WorldAnchored = data.WorldAnchored
		        });
	        }
	        
	        foreach (DataConfigWaveGerstner data in _dataGerstnerWaves)
	        {
		        _dataGerstner.Add(new DataWaveGerstner()
		        {
			        Origin = data.WorldAnchored ? transform.InverseTransformPoint(data.Origin) : data.Origin,
			        Amplitude = data.Amplitude,
			        OmniDirectional = data.OmniDirectional,
			        WorldAnchored = data.WorldAnchored,
			        Offset = 0
		        });
	        }
        }
        
        private Solver ResolveSolver()
        {
	        switch (_configData.Mode)
	        {
		        case HeightmeshUpdateMode.CPU_Naive: 
			        return new Solver_CPUNaive(this);
			        break;
		        case HeightmeshUpdateMode.CPU_Job:
		        case HeightmeshUpdateMode.CPU_JobBurst:
		        case HeightmeshUpdateMode.CPU_JobBurstThreaded:
			        return new Solver_CPUJob(this);
			        break;
		        case HeightmeshUpdateMode.GPU_Compute:

			        break;
	        }
	        
	        // Fallback
	        return new Solver_CPUNaive(this);
        }

        private void OnEnable()
        {
	        Mesh = CreateMesh();
	        if (_solver == null)
	        {
		        _solver = ResolveSolver();
	        }
	        StartCoroutine(CR_RippleGen());
        }

        #endregion INITIALIZATION

        
        #region UPDATE

        private void Update()
        {
	        _localTime += Time.deltaTime;
	        UpdateWaveSourcePositions(Time.deltaTime);
	        GetDataSinWaves(_dataSinWaves, Time.deltaTime);
	        GetDataGerstnerWaves(_dataGerstnerWaves, Time.deltaTime);

	        _solver?.Solve(
				transform.position,
		        _originalVertices, 
		        _dataRipples,
				_dataSin.ToArray(),
				_dataGerstner.ToArray(),
		        _localTime);
        }

        private void GetDataSinWaves(DataConfigWaveSin[] data, float delta)
        {
	        for (int i = 0; i < data.Length; i++)
	        {
		        _dataSin[i] = new DataWaveSin()
		        {
					Direction = Quaternion.Euler(Vector3.up * data[i].Direction),
					Amplitude = data[i].Amplitude,
					Wavelength = data[i].Wavelength,
					Offset = GetWaveOffset(delta, data[i].Speed, _dataSin[i].Offset),
					WorldAnchored = data[i].WorldAnchored,
		        };
	        }
        }

        private void GetDataGerstnerWaves(DataConfigWaveGerstner[] data, float delta)
        {
	        for (int i = 0; i < data.Length; i++)
	        {
		        // Precalculate data to avoid re-calculation inside vertex job
		        float wavelength = 6.28318f / data[i].Wavelength;					// 2pi over wavelength(hardcoded)
		        float qi = 0.8f / (data[i].Amplitude * wavelength * data.Length);	// 0.8 = peak value, 1 is the sharpest peaks
		        float direction = Mathf.Deg2Rad * data[i].Direction;
		        Vector3 windDirInput =
			        new Vector3(Mathf.Sin(direction), 0, Mathf.Cos(direction)) * (1 - (data[i].OmniDirectional ? 1 : 0));
		        
		        _dataGerstner[i] = new DataWaveGerstner()
		        {
			        Origin = data[i].WorldAnchored ? transform.InverseTransformPoint(data[i].Origin) : data[i].Origin,
			        Amplitude = data[i].Amplitude,
			        OmniDirectional = data[i].OmniDirectional,
			        WorldAnchored = data[i].WorldAnchored,
			        Offset = GetWaveOffset(delta, data[i].Speed, _dataGerstner[i].Offset),
			        
			        Wavelength = wavelength,
			        Qi = qi,
			        WindDirectionInput = windDirInput
		        };
	        }
        }
        
        private float GetWaveOffset(float delta, float speed, float previousOffset)
        {
	        float offset = previousOffset + (delta * speed);
	        if (offset > _configData.SurfaceActualWidth)
	        {
		        offset -= _configData.SurfaceActualWidth;
	        }
	        else if (offset < -_configData.SurfaceActualWidth)
	        {
		        offset += _configData.SurfaceActualWidth;
	        }
	        
	        return offset;
        }
        
        #endregion UPDATE


        #region WAVE SOURCES

        private IEnumerator CR_RippleGen()
        {
	        while (true)
	        {
		        foreach (Transform source in _waveSources)
		        {
			        _dataRipples.Add(new DataHeightwaveRipple()
			        {
				        Position = GetXZPosition(transform.InverseTransformPoint(source.position)),
				        Speed = _dataConfigRipples.Speed,
				        Strength = _dataConfigRipples.Strength,
				        Distance = _dataConfigRipples.Distance,
				        LifetimeRemaining = _dataConfigRipples.Lifetime
			        });
		        }
		        
		        yield return new WaitForSeconds(_configData.UpdateRateRipples);
	        }
        }

        void UpdateWaveSourcePositions(float delta)
        {
	        for (int i = 0; i < _dataRipples.Count; i++)
	        {
		        // Weaken ripple
		        float newLife = _dataRipples[i].LifetimeRemaining - delta;
		        if (newLife <= 0)
		        {
			        _dataRipples.RemoveAt(i);
		        }
		        else
		        {
			        _dataRipples[i] = new DataHeightwaveRipple()
			        {
				        Position = _dataRipples[i].Position,
				        Strength = _dataRipples[i].Strength,
				        Distance = _dataRipples[i].Distance,
				        Speed = _dataRipples[i].Speed,
				        LifetimeRemaining = newLife
			        };
		        }
	        }
        }

        private Vector2 GetXZPosition(Vector3 source)
        {
	        return new Vector2(source.x, source.z);
        }

        #endregion WAVE SOURCES

        
        #region MESH

        private Mesh CreateMesh()
        {
	        Mesh = new Mesh
	        {
		        // Use 32 bit index buffer to allow water grids larger than ~250x250
		        indexFormat = IndexFormat.UInt32
	        };

	        // Create initial grid of vertex positions
	        _originalVertices = new Vector3[_configData.SurfaceWidthPoints * _configData.SurfaceLengthPoints];
	        
	        int index = 0;
	        for (int i = 0; i < _configData.SurfaceWidthPoints; i++)
	        {
		        for (int j = 0; j < _configData.SurfaceLengthPoints; j++)
		        {
			        float x = MapValue(
				        i, 
				        0.0f, 
				        _configData.SurfaceWidthPoints - 1, 
				        -_configData.SurfaceActualWidth / 2.0f,
				        _configData.SurfaceActualWidth / 2.0f);
			        float z = MapValue(
				        j, 
				        0.0f, 
				        _configData.SurfaceLengthPoints - 1, 
				        -_configData.SurfaceActualLength / 2.0f, 
				        _configData.SurfaceActualLength / 2.0f);
			        _originalVertices[index++] = new Vector3(x, 0f, z);
		        }
	        }

	        // Create an index buffer for the grid
	        int[] indices = new int[(_configData.SurfaceWidthPoints - 1) * (_configData.SurfaceLengthPoints - 1) * 6];
	        index = 0;
	        for (int i = 0; i < _configData.SurfaceWidthPoints - 1; i++)
	        {
		        for (int j = 0; j < _configData.SurfaceLengthPoints - 1; j++)
		        {
			        int baseIndex = i * _configData.SurfaceLengthPoints + j;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + 1;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints + 1;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints + 1;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints;
		        }
	        }

	        Mesh.SetVertices(_originalVertices);
	        Mesh.triangles = indices;
	        Mesh.uv = GenerateUVs(_configData);
	        Mesh.RecalculateNormals();
	        GetComponent<MeshFilter>().mesh = Mesh;

	        return Mesh;
        }

        private Vector2[] GenerateUVs(DataConfigHeightmesh data)
        {
            Vector2[] uvs = new Vector2[(data.SurfaceWidthPoints * data.SurfaceLengthPoints)];
        
            //always set one uv over n tiles than flip the uv and set it again
            for (int x = 0; x <= data.SurfaceWidthPoints; x++)
            {
                for (int z = 0; z <= data.SurfaceLengthPoints; z++)
                {
                    Vector2 vec = new Vector2((x / data.UVScale) % 2, (z / data.UVScale) % 2);
                    int index = Index(x, z);
                    if (index < uvs.Length)
                    {
	                    uvs[index] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
                    }
                }
            }
        
            return uvs;
        }
        
        private int Index(int x, int z)
        {
            return x * (_configData.SurfaceWidthPoints) + z;
        }
        
        internal static float MapValue(float refValue, float refMin, float refMax, float targetMin, float targetMax)
        {
	        return targetMin + (refValue - refMin) * (targetMax - targetMin) / (refMax - refMin);
        }

        #endregion MESH
    }
}
