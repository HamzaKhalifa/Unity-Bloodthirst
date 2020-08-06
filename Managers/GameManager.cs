using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;
    public event System.Action<Player> OnLocalPlayerJoined = null;

    [SerializeField] private Camera _startCamera = null;
    [SerializeField] private Canvas _gameCanvas = null;
    [SerializeField] private LeaderboardManager _leaderboardManager = null;
    [SerializeField] private GameObject _loadingIndicator = null;
    [SerializeField] private RespawnManager _respawnManager = null;
    [SerializeField] private AISpawner _aiSpawner = null;
    [SerializeField] private GameTimeManager _gameTimeUI = null;
    [SerializeField] private DaytimeManager _daytimeManager = null;
    [SerializeField] private ScopeUI _scopeUI = null;
    [SerializeField] private NetworkHUD _networkHUD = null;

    #region Cache Fields

    private InputManager _inputManager = null;
    private TimerManager _timerManager = null;
    private AudioManager _audioManager = null;
    private SaveManager _saveManager = null; 

    #endregion

    #region Private Fields

    private Player _localPlayer = null;

    #endregion

    #region Public Accessors

    public Camera StartCamera { get { return _startCamera; } }

    public InputManager InputManager { get { return _inputManager; } }
    public TimerManager TimerManager { get { return _timerManager; } }
    public RespawnManager RespawnManager { get { return _respawnManager; } }
    public AudioManager AudioManager { get { return _audioManager; } }
    public LeaderboardManager LeaderboardManager { get { return _leaderboardManager; } }
    public AISpawner AISpawner { get { return _aiSpawner; } }
    public Player LocalPlayer
    {
        get { return _localPlayer; }
        set {
            _localPlayer = value;
            if (OnLocalPlayerJoined != null)
            {
                _gameCanvas.gameObject.SetActive(true);
                OnLocalPlayerJoined(_localPlayer);
            }
        }
    }
    public SaveManager SaveManager { get { return _saveManager; } }
    public GameTimeManager GameTimeUI { get { return _gameTimeUI; } }
    public DaytimeManager DaytimeManager { get { return _daytimeManager; } }
    public ScopeUI ScopeUI { get { return _scopeUI; } }
    public NetworkHUD NetworkHUD { get { return _networkHUD; } }

    #endregion


    #region Monobehavior Callbacks

    private void Awake()
    {
        Instance = this;

        _gameCanvas.gameObject.SetActive(false);
        _inputManager = GetComponent<InputManager>();
        _timerManager = GetComponent<TimerManager>();
        _audioManager = GetComponent<AudioManager>();
        _saveManager = GetComponent<SaveManager>();

        SetLoadingIndicator(false);
    }

    #endregion

    #region Public Methods

    public void SetLoadingIndicator(bool reloading = true)
    {
        _loadingIndicator.gameObject.SetActive(reloading);
    }

    #endregion
}
