using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Proyecto26;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class GlobalLeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject _globalLeaderboardPanel = null;
    [SerializeField] private GameObject _profilesContent = null;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerProfilePanel = null;

    private JObject _users = new JObject();

    private void Start()
    {
        _globalLeaderboardPanel.SetActive(false);
    }

    public void GlobalLeaderBoardButton()
    {
        // If we are about to activate the global leaderboard, then we send a request to get the users
        if (!_globalLeaderboardPanel.gameObject.activeSelf)
        {
            GetUsers();
        }

        _globalLeaderboardPanel.SetActive(!_globalLeaderboardPanel.activeSelf);
    }

    private void RepaintGlobalLeaderboard()
    {
        // We first remove all player contents
        for (int i = 0; i < _profilesContent.transform.childCount; i++)
        {
            Destroy(_profilesContent.transform.GetChild(i).gameObject);
        }

        List<User> users = new List<User>();

        foreach(var jUser in _users)
        {
            User user = JsonUtility.FromJson<User>(jUser.Value.ToString());
            users.Add(user);
        }

        users.Sort((x, y) => x.BestScore.CompareTo(y.BestScore));

        foreach(User user in users)
        {
            GameObject tmp = Instantiate(_playerProfilePanel, _profilesContent.transform);
            tmp.transform.Find("Player Name").GetComponent<Text>().text = user.Nickname;
            tmp.transform.Find("Player Score").GetComponent<Text>().text = user.BestScore + "";
        }
    }

    private void GetUsers()
    {
        RestClient.Request(new RequestHelper
        {
            Uri = NetworkUtils.DatabaseUrl + "UsersByNickname.json",
            Method = "GET"
        }).Then(response =>
        {
            _users = JObject.Parse(response.Text);
            RepaintGlobalLeaderboard();
        }).Catch(e =>
        {
            Debug.Log(e.Message);
        });
    }
}
