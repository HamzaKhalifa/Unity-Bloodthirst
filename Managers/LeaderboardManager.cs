using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerProfile
{
    public Player Player = null;
    public string Nickname = "Blood Thirsty";
    public int Score = 0;
    public uint netId;

    public GameObject playerProfilePanel;
    public Text playerProfilePanelPlayerNameText;
    public Text playerProfilePanelScoreText;
}

public class InComingScore
{
    public PlayerProfile PlayerProfile = null;
    public int Amount = 0;
}

public class LeaderboardManager : NetworkBehaviour
{
    #region Inspector Assigned Fields

    [SerializeField] private GameObject _leaderboardPanel = null;
    [SerializeField] private GameObject _playerProfilesPanel = null;
    [SerializeField] private List<Text> _connectedPlayersNamesTexts = new List<Text>();

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerProfilePanel = null;

    #endregion

    #region Private Fields

    List<PlayerProfile> _playerProfiles = new List<PlayerProfile>();
    private Queue<InComingScore> _incomingScorePoints = new Queue<InComingScore>();
    private IEnumerator _updateScoreCoroutine = null;

    #endregion

    public List<PlayerProfile> PlayerProfiles { get { return _playerProfiles; } }

    #region Monobehavior

    private void Start()
    {
        _leaderboardPanel.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance.InputManager.Tab)
        {
            bool active = !_leaderboardPanel.activeSelf;
            HandleLeaderboardShowing(active);
        }

        PlayerProfile[] playerProfilesCopy = _playerProfiles.ToArray();
        foreach (PlayerProfile playerProfile in playerProfilesCopy)
        {
            if (playerProfile.Player == null)
            {
                OnPlayerDisconnected(playerProfile.netId);
            }
        }

        if (_incomingScorePoints.Count > 0 && _updateScoreCoroutine == null)
        {
            _updateScoreCoroutine = UpdateScoreCoroutine(_incomingScorePoints.Dequeue());
            StartCoroutine(_updateScoreCoroutine);
        }
    }

    public void HandleLeaderboardShowing(bool active)
    {
        _leaderboardPanel.SetActive(active);
    }

    #endregion

    #region public Methods

    public void AddPlayer(string playerName, Player player)
    {
        PlayerProfile profile = new PlayerProfile();
        profile.netId = player.netId;
        profile.Nickname = playerName;
        profile.Player = player;

        _playerProfiles.Add(profile);

        RepaintLeaderboard();
    }

    public void RepaintLeaderboard()
    {
        string[] playerNames = new string[_playerProfiles.Count];
        uint[] playerNetworkIds = new uint[_playerProfiles.Count];
        int[] playerScores = new int[_playerProfiles.Count];
        for (int i = 0; i < _playerProfiles.Count; i++)
        {
            playerNames[i] = _playerProfiles[i].Nickname;
            playerNetworkIds[i] = _playerProfiles[i].netId;
            playerScores[i] = _playerProfiles[i].Score;
        }

        RpcDoRepaint(playerNames, playerNetworkIds, playerScores);
    }

    [ClientRpc]
    public void RpcDoRepaint(string[] playerNames, uint[] playerNetworkIds, int[] playerScores)
    {
        // We destroy all player profiles instantiated prefabs first
        foreach (PlayerProfile playerProfile in _playerProfiles)
        {
            Destroy(playerProfile.playerProfilePanel);
        }
        _playerProfiles.Clear();

        for (int i = 0; i < playerNames.Length; i++)
        {
            PlayerProfile profile = new PlayerProfile();
            profile.netId = playerNetworkIds[i];
            profile.Nickname = playerNames[i];
            profile.Score = playerScores[i];

            // Finding the player
            Player[] players = FindObjectsOfType<Player>();
            foreach (Player player in players)
            {
                if (player.netId == profile.netId)
                {
                    profile.Player = player;
                    break;
                }
            }

            _playerProfiles.Add(profile);
        }

        // Now we sort them by score
        _playerProfiles.Sort((playerProfile1, playerProfile2) => playerProfile1.Score.CompareTo(playerProfile2.Score));

        for (int i = 0; i < _playerProfiles.Count; i++)
        {
            PlayerProfile playerProfile = _playerProfiles[i];
            GameObject playerProfilePanel = Instantiate(_playerProfilePanel, _playerProfilesPanel.transform);
            playerProfile.playerProfilePanel = playerProfilePanel;
            playerProfile.playerProfilePanelPlayerNameText = playerProfilePanel.transform.Find("Player Name").GetComponent<Text>();
            playerProfile.playerProfilePanelPlayerNameText.text = playerProfile.Nickname;
            playerProfile.playerProfilePanelScoreText = playerProfilePanel.transform.Find("Player Score").GetComponent<Text>();
            playerProfile.playerProfilePanelScoreText.text = "Score: " + playerProfile.Score;

            // Now updating player names
            _connectedPlayersNamesTexts[i].text = playerProfile.Nickname;
        }

        // Now emptying the player names that don't have players to them
        for (int i = _connectedPlayersNamesTexts.Count - 1; i >= _playerProfiles.Count; i--)
        {
            _connectedPlayersNamesTexts[i].text = "";
        }
    }

    private void OnPlayerDisconnected(uint netId)
    {
        PlayerProfile profileToRemove = _playerProfiles.Find((profile) => netId == profile.netId);
        if (profileToRemove != null)
        {
            Destroy(profileToRemove.playerProfilePanel);
            _playerProfiles.Remove(profileToRemove);
            RepaintLeaderboard();
        }
    }

    public void EnqueueScore(uint netId, int amount)
    {
        PlayerProfile playerProfile = _playerProfiles.Find((profile) => netId == profile.netId);
        InComingScore incomingScore = new InComingScore();
        incomingScore.PlayerProfile = playerProfile;
        incomingScore.Amount = amount;

        _incomingScorePoints.Enqueue(incomingScore);
    }

    public IEnumerator UpdateScoreCoroutine(InComingScore incomingScore)
    {
        // We update the score here, then the hook gets called for the clients and updates the score text
        int newScore = incomingScore.PlayerProfile.Score + incomingScore.Amount;
        RpcSetScore(incomingScore.PlayerProfile.netId, newScore);

        _updateScoreCoroutine = null;

        yield break;
    }

    [ClientRpc]
    private void RpcSetScore(uint netId, int score)
    {
        PlayerProfile playerProfile = _playerProfiles.Find((profile) => netId == profile.netId);
        if (playerProfile != null)
        {
            SetPlayerProfileScore(playerProfile, score);
        }
    }

    public void ResetScores()
    {
        foreach(PlayerProfile playerProfile in _playerProfiles)
        {
            SetPlayerProfileScore(playerProfile, 0);
        }
    }

    private void SetPlayerProfileScore(PlayerProfile playerProfile, int score)
    {
        playerProfile.Score = score;
        if (playerProfile.playerProfilePanelPlayerNameText != null)
            playerProfile.playerProfilePanelScoreText.text = "Score: " + playerProfile.Score;
    }

    public void UpdateBestScoreForEachClient()
    {
        foreach(PlayerProfile playerProfile in _playerProfiles)
        {
            GameManager.Instance.NetworkHUD.NetworkUtils.UpdatePlayerBestScore(playerProfile.Nickname, playerProfile.Score);
        }
    }

    #endregion

}
