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

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Awake()
        {
	        
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
	        _solver?.Solve(
				transform.position,
		        _originalVertices, 
		        _dataRipples, 
		        GetDataSinWaves(_dataSinWaves),
		        GetDataGerstnerWaves(_dataGerstnerWaves, transform),
		        _localTime);
        }

        private static DataWaveSin[] GetDataSinWaves(DataConfigWaveSin[] data)
        {
	        DataWaveSin[] dataOut = new DataWaveSin[data.Length];
	        for (int i = 0; i < data.Length; i++)
	        {
		        dataOut[i] = new DataWaveSin()
		        {
					Direction = Quaternion.Euler(Vector3.up * data[i].Direction),
					Speed = data[i].Speed,
					Amplitude = data[i].Amplitude,
					Strength = data[i].Strength,
					WorldAnchored = data[i].WorldAnchored
		        };
	        }

	        return dataOut;
        }
        
        private static DataWaveGerstner[] GetDataGerstnerWaves(DataConfigWaveGerstner[] data, Transform transform)
        {
	        DataWaveGerstner[] dataOut = new DataWaveGerstner[data.Length];
	        for (int i = 0; i < data.Length; i++)
	        {
		        // Precalculate data to avoid re-calculation inside vertex job
		        float wavelength = 6.28318f / data[i].Wavelength;					// 2pi over wavelength(hardcoded)
		        float wSpeed = Mathf.Sqrt(9.8f * wavelength) * data[i].Speed;		// frequency of the wave based off wavelength
		        float qi = 0.8f / (data[i].Amplitude * wavelength * data.Length);	// 0.8 = peak value, 1 is the sharpest peaks
		        float direction = Mathf.Deg2Rad * data[i].Direction;
		        Vector3 windDirInput =
			        new Vector3(Mathf.Sin(direction), 0, Mathf.Cos(direction)) * (1 - (data[i].OmniDirectional ? 1 : 0));
		        
		        dataOut[i] = new DataWaveGerstner()
		        {
			        Origin = data[i].WorldAnchored ? transform.InverseTransformPoint(data[i].Origin) : data[i].Origin,
			        Amplitude = data[i].Amplitude,
			        OmniDirectional = data[i].OmniDirectional,
			        WorldAnchored = data[i].WorldAnchored,
			        
			        Wavelength = wavelength,
			        WSpeed = wSpeed,
			        Qi = qi,
			        WindDirectionInput = windDirInput
		        };
	        }

	        return dataOut;
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
