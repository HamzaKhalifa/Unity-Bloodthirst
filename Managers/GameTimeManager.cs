using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class GameTimeManager : NetworkBehaviour
{
    [SerializeField] private Text _timeText = null;
    [SerializeField] private float _endGameTime = 60f;
    [SerializeField] private GameObject _startGamePanel = null;
    [SerializeField] private GameObject _startGameButton = null;
    [SerializeField] private Text _privateHostInstruction = null;
    [SerializeField] private GameObject _waitForHostIndicator = null;

    #region synched Fields

    [SyncVar]
    private float _time = 0;
    [SyncVar]
    private bool _gameStarted = false;

    #endregion

    public bool GameStopped
    {
        get
        {
            if (_gameStarted)
                return _time >= _endGameTime;
            else
            {
                return true;
            }
        }
    }

    private void Start()
    {
        InitializeForStart();

        // It could be that the client joins when the game is already playing
        if (!GameStopped)
        {
            ClientsStartGame();
            // When the player joins a playing game, we need to spawn him somewhere so that he doesn't stay flying in the iar
            GameManager.Instance.LocalPlayer.CmdInstantPlayerRespawn(GameManager.Instance.LocalPlayer.netId);
        }
    }

    private void Update()
    {
        if (GameStopped) return;

        // Only the server updates the time
        if (isServer) 
            _time += Time.deltaTime;

        // Everyone will update the text with the synched variable time
        if (_timeText != null)
        {
            _timeText.text = (int)_time + "/" + _endGameTime;
        }

        // Only the server ends the game
        if(isServer & _time >= _endGameTime)
        {
            StopGame();
        }
    }

    #region Start Game

    // Only the server should be able to call this method
    public void StartGameButton()
    {
        GameManager.Instance.LocalPlayer.CmdStartGame();
    }

    public void ServerStartGame()
    {
        // These two variables are synched with the clients
        _time = 0f;
        _gameStarted = true;

        // The day variables like exposure and ambient light are synched with the clients with hooks
        GameManager.Instance.DaytimeManager.RestartDay();

        // And change the players' positions.
        // Only the server is responsible of this
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            GameManager.Instance.LocalPlayer.CmdInstantPlayerRespawn(player.netId);
        }
    }

    public void ClientsStartGame()
    {
        // We destroy all the enemies and change the player's positions
        // I have a problem here: 
        GameManager.Instance.AISpawner.RestartGame();
        _startGamePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    private void StopGame()
    {
        GameManager.Instance.LocalPlayer.CmdStopGame();
    }

    public void ServerStopGame()
    {

    }

    public void ClientsStopGame()
    {
        GameManager.Instance.LeaderboardManager.HandleLeaderboardShowing(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Invoke("InitializeForStart", 10);
    }

    public void InitializeForStart()
    {
        _startGamePanel.SetActive(true);
        _startGameButton.SetActive(isServer);

        // Checking if it's a private host instruction 
        _privateHostInstruction.gameObject.SetActive(GameManager.Instance.NetworkHUD.ConnectionType == ConnectionType.PrivateHost);
        if (GameManager.Instance.NetworkHUD.ConnectionType == ConnectionType.PrivateHost)
        {
            _privateHostInstruction.text = "Give this to other competitors: " + NetworkHUD.LocalIP;
        }

        _waitForHostIndicator.SetActive(!isServer);
        GameManager.Instance.LeaderboardManager.HandleLeaderboardShowing(false);
        GameManager.Instance.LeaderboardManager.ResetScores();
    }
}
