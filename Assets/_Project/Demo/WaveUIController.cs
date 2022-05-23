using System;
using System.Collections;
using System.Collections.Generic;
using Stephens.Heightmesh;
using UnityEngine;
using UnityEngine.UI;

public class WaveUIController : MonoBehaviour
{
    #region VARIABLES

    [SerializeField] private Slider _gerstner1Amplitude;
    [SerializeField] private Slider _gerstner1Length;
    [SerializeField] private Slider _gerstner1Speed;
    [SerializeField] private Slider _gerstner1Direction;
    
    [SerializeField] private Slider _gerstner2Amplitude;
    [SerializeField] private Slider _gerstner2Length;
    [SerializeField] private Slider _gerstner2Speed;
    [SerializeField] private Slider _gerstner2Direction;
    
    [SerializeField] private Slider _sinAmplitude;
    [SerializeField] private Slider _sinLength;
    [SerializeField] private Slider _sinSpeed;
    [SerializeField] private Slider _sinDirection;
    
    [SerializeField] private Button _noiseToggle;
    [SerializeField] private Slider _noiseStrength;
    [SerializeField] private Slider _noiseSpread;
    [SerializeField] private Slider _noiseSpeed;
    
    [SerializeField] private Button _reset;


    private SystemHeightmesh _hm;
    private int _noiseIndex;

    #endregion VARIABLES


    #region INITIALIZATION

    private void Awake()
    {
        _hm = SystemHeightmesh.Instance;
        _noiseToggle.onClick.AddListener(OnNoiseToggle);
        _reset.onClick.AddListener(OnReset);
    }

    private void Update()
    {
        if (_hm.Inputs.Length != 4)
            return;
        
        (_hm.Inputs[0] as DataConfigWaveGerstner).Amplitude = _gerstner1Amplitude.value;
        (_hm.Inputs[0] as DataConfigWaveGerstner).Wavelength = _gerstner1Length.value;
        (_hm.Inputs[0] as DataConfigWaveGerstner).Speed = _gerstner1Speed.value;
        (_hm.Inputs[0] as DataConfigWaveGerstner).Direction = _gerstner1Direction.value;
        
        (_hm.Inputs[1] as DataConfigWaveGerstner).Amplitude = _gerstner2Amplitude.value;
        (_hm.Inputs[1] as DataConfigWaveGerstner).Wavelength = _gerstner2Length.value;
        (_hm.Inputs[1] as DataConfigWaveGerstner).Speed = _gerstner2Speed.value;
        (_hm.Inputs[1] as DataConfigWaveGerstner).Direction = _gerstner2Direction.value;
        
        (_hm.Inputs[2] as DataConfigWaveSin).Amplitude = _sinAmplitude.value;
        (_hm.Inputs[2] as DataConfigWaveSin).Wavelength = _sinLength.value;
        (_hm.Inputs[2] as DataConfigWaveSin).Speed = _sinSpeed.value;
        (_hm.Inputs[2] as DataConfigWaveSin).Direction = _sinDirection.value;
        
        (_hm.Inputs[3] as DataConfigNoise).Strength = _noiseStrength.value;
        (_hm.Inputs[3] as DataConfigNoise).Spread = _noiseSpread.value;
        (_hm.Inputs[3] as DataConfigNoise).Speed = new Vector2(_noiseSpeed.value, 0);
    }

    private void OnNoiseToggle()
    {
        _noiseIndex++;
        if (_noiseIndex > 2)
        {
            _noiseIndex = 0;
        }

        UpdateNoiseType();
    }

    private void OnReset()
    {
        _gerstner1Amplitude.value = 0;
        _gerstner1Length.value = 50;
        _gerstner1Speed.value = 1.5f;
        _gerstner1Direction.value = 0;
        
        _gerstner2Amplitude.value = 0;
        _gerstner2Length.value = 50;
        _gerstner2Speed.value = 1.5f;
        _gerstner2Direction.value = 0;
        
        _sinAmplitude.value = 0;
        _sinLength.value = 0.1f;
        _sinSpeed.value = 0;
        _sinDirection.value = 0;

        _noiseStrength.value = 0;
        _noiseSpread.value = 0.03f;
        _noiseSpeed.value = 0;
        _noiseIndex = 0;
        UpdateNoiseType();
    }

    private void UpdateNoiseType()
    {
        switch (_noiseIndex)
        {
            case 0: (_hm.Inputs[3] as DataConfigNoise).Type = NoiseType.Perlin; break;
            case 1: (_hm.Inputs[3] as DataConfigNoise).Type = NoiseType.Cellular; break;
            case 2: (_hm.Inputs[3] as DataConfigNoise).Type = NoiseType.Simplex; break;
        }
    }

    #endregion INITIALIZATION
}
