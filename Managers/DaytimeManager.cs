using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DaytimeManager : NetworkBehaviour
{
    [SerializeField] private Material _skyboxMaterial = null;
    [SerializeField] private List<Color> _dayColors = new List<Color>();
    [SerializeField] private List<float> _exposures = new List<float>();
    [SerializeField] float _dayProgressionTime= 10f;
    [SerializeField] float _dayStateDelay = 2f;

    private float _dayStateTimer = 0f;
    private IEnumerator _coroutine = null;
    private int _currentDayColor = 0;

    [SyncVar(hook = "HookDayColor")]
    private Color _dayColor = Color.black;
    [SyncVar(hook = "HookExposure")]
    private float _exposure = 0f;


    private void Start()
    {
        _dayStateTimer = _dayStateDelay;
    }

    private void Update()
    {
        if (!isServer || GameManager.Instance.GameTimeUI.GameStopped) return;

        if (_coroutine == null)
        {
            _dayStateTimer += Time.deltaTime;
        }

        if (_dayStateTimer >= _dayStateDelay)
        {
            _dayStateTimer = 0f;
            _coroutine = LerpDay();
            StartCoroutine(_coroutine);
        }
    }

    private IEnumerator LerpDay()
    {
        float timer = 0f;

        Color initialColor = RenderSettings.ambientLight;
        int nextDayColorIndex = _currentDayColor + 1;
        if (nextDayColorIndex >= _dayColors.Count)
            nextDayColorIndex = 0;

        float initialeSkyBoxExposure = _exposures[_currentDayColor];
        float nextSkyBoyExposure = _exposures[nextDayColorIndex];

        while (timer <= _dayProgressionTime)
        {
            timer += Time.deltaTime;
            float _normalizeTime = timer / _dayProgressionTime;

            _dayColor = Color.Lerp(initialColor, _dayColors[nextDayColorIndex], _normalizeTime);
            _exposure = Mathf.Lerp(initialeSkyBoxExposure, nextSkyBoyExposure, _normalizeTime);

            yield return null;
        }

        _currentDayColor = nextDayColorIndex;
        RenderSettings.ambientLight = _dayColors[_currentDayColor];

        _coroutine = null;
    } 

    private void HookDayColor(Color oldColor, Color newColor)
    {
        RenderSettings.ambientLight = newColor;
    }

    private void HookExposure(float oldExposure, float newExposure)
    {
        _skyboxMaterial.SetFloat("_Exposure", newExposure);
    }

    // Only the server should call this method
    public void RestartDay()
    {
        _dayColor = _dayColors[0];
        _exposure = 1;
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _dayStateTimer = 0f;
    }
}
