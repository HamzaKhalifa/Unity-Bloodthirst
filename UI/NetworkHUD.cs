using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public enum ConnectionType
{
    None,
    PublicHost,
    PrivateHost,
    PrivateClient,
    PublicClient
}

public class NetworkHUD : NetworkBehaviour
{
    //public const float UPDATE_SERVER_DELAY = 30f;
    // For testing
    public const float UPDATE_SERVER_DELAY = 30f;
    public const float UPDATE_ERROR_MARGIN = 5f;
    
    #region Inspector Dependencies

    [Header("Authentification Dependencies")]
    [SerializeField] GameObject _authentificationPanel = null;

    [SerializeField] InputField _signupPlayerNameInputField = null;
    [SerializeField] InputField _signUpEmailInputField = null;
    [SerializeField] InputField _signUpPasswordInputField = null;
    [SerializeField] Text _signUpErrorText = null;

    [SerializeField] InputField _signInPlayerNameOrEmailInputField = null;
    [SerializeField] InputField _signInPasswordInputField = null;
    [SerializeField] Text _signInErrorText = null;

    [SerializeField] GameObject _signUpForm = null;
    [SerializeField] GameObject _signInForm = null;
    [SerializeField] GameObject _authenticationPendingUI = null;

    [Header("Connection Dependencies")]
    [SerializeField] GameObject _connectionPanel = null;
    [SerializeField] InputField _serverIPInputField = null;
    [SerializeField] Text _errorText = null;
    [SerializeField] GameObject _globalLeaderboardUI = null;

    [Header("Connecting Dependencies")]
    [SerializeField] GameObject _connectingPanel = null;

    #endregion

    #region CacheFields

    private NetworkManager _networkManager = null;
    public NetworkManager NetworkManager { get { return _networkManager; } }

    private static string _localIP = null;
    public static string LocalIP {
        get
        {
            if (_localIP == null)
                _localIP = NetworkUtils.LocalIPAddress();

            return _localIP;
        }
    }

    private ConnectionType _connectionType = ConnectionType.None;
    public ConnectionType ConnectionType { get { return _connectionType; } }

    private NetworkUtils _networkUtils = null;
    public NetworkUtils NetworkUtils { get { return _networkUtils; } }

    #endregion

    #region Private Fields

    private bool _connecting = false;
    private float _updateServerTimer = 0f;
    private IEnumerator _joinRandomHostCoroutine = null;

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        _networkUtils = GetComponent<NetworkUtils>();
        _serverIPInputField.text = "localhost";
        GameManager.Instance.OnLocalPlayerJoined += (player) =>
        {
            _connectionPanel.SetActive(false);
            _globalLeaderboardUI.SetActive(false);
            _connectingPanel.SetActive(false);

            player.CmdOnPlayerConnected(GameManager.Instance.SaveManager.SavedData.Nickname);
        };

        InitializeUI();

        InitializeSavedData();

        Telepathy.Server.OnServerStarted += AddHostIPEntry;
        Telepathy.Server.OnServerStopped += RemoveHostIPEntry;

        _networkManager.OnClientConnected += AddClientToHost;
        _networkManager.OnClientDisconnected += RemoveClientFromHost;
        // Handling client connection errors
        _networkManager.OnClientStopped += OnClientStopped;

    }

    private void Update()
    {
        if (_connecting)
        {
            if (!_networkManager.isNetworkActive && _connectionType == ConnectionType.PrivateClient)
            {
                _networkManager.StopClient();
                SetConnectionError("Connection Failed");

                RemoveHostIPEntry();
            }

            if (GameManager.Instance.LocalPlayer != null && GameManager.Instance.LocalPlayer.isServer && _connectionType == ConnectionType.PublicHost) {
                _updateServerTimer += Time.deltaTime;
                if (_updateServerTimer >= UPDATE_SERVER_DELAY)
                {
                    _updateServerTimer = 0f;
                    Debug.Log("-----------Server about to be updated-----------");
                    _networkUtils.UpdateServer(LocalIP);
                }
            }
        }
    }

    #endregion

    #region Connection Methods

    public void BeginConnection()
    {
        _connecting = true;
        _errorText.text = "";
        _connectingPanel.SetActive(true);
        _connectionPanel.SetActive(false);
        _globalLeaderboardUI.SetActive(false);
    }

    public void SetConnectionError(string text)
    {
        // If the client stops when we are joining a random host, it means we found an active server but couldn't join, this means we are in the threshold time period between the moment the server was shut down and the moment it starts being considered inactive: 35 second
        // So we make another attempt
        if (_connectionType == ConnectionType.PublicClient)
        {
            if (_joinRandomHostCoroutine != null)
            {
                StopCoroutine(_joinRandomHostCoroutine);
                _joinRandomHostCoroutine = null;
            }

            _joinRandomHostCoroutine = JoinRandomHostCoroutine();
            StartCoroutine(_joinRandomHostCoroutine);
        }
        else
        {
            _errorText.text = text;
            _connectingPanel.SetActive(false);
            _connectionPanel.SetActive(true);
            _globalLeaderboardUI.SetActive(true);
            _connectionType = ConnectionType.None;
            _connecting = false;
        }
        
    }

    /// <summary>
    /// Public hosting using database
    /// </summary>
    public void SartPublicHost()
    {
        BeginConnection();
        _connectionType = ConnectionType.PublicHost;
        _networkManager.StartHost();
    }

    /// <summary>
    /// Starting a private host
    /// </summary>
    public void StartHost()
    {
        BeginConnection();
        _connectionType = ConnectionType.PrivateHost;
        _networkManager.StartHost();
    }

    /// <summary>
    /// Start client using a manually entered Host IP
    /// </summary>
    public void StartClient()
    {
        BeginConnection();
        _connectionType = ConnectionType.PrivateClient;

        _networkManager.networkAddress = _serverIPInputField.text;
        try
        {
            _networkManager.StartClient();
        } catch (Exception e)
        {
            SetConnectionError(e.Message);
        }
    }

    /// <summary>
    /// Start client using a random host IP in the database
    /// </summary>
    public void JoinRandomHost()
    {
        BeginConnection();
        _connectionType = ConnectionType.PublicClient;

        try
        {
            _networkUtils.GetRandomServerHost(serverHostIP =>
            {
                if (serverHostIP == null)
                {
                    throw new Exception("Could not find available host! Try being host yourself and wait for others to join in :)");
                }
                else
                {
                    Debug.Log("Joining Random host");
                    _networkManager.networkAddress = serverHostIP;
                    _networkManager.StartClient();
                }
            });
        } catch(Exception e)
        {
            Debug.Log("It seems we are catching an error");
            SetConnectionError(e.Message);
        }
    }

    private IEnumerator JoinRandomHostCoroutine()
    {
        yield return new WaitForSeconds(2);

        JoinRandomHost();

        _joinRandomHostCoroutine = null;
    }

    public void StopClient()
    {
        // We could be having a join random host coroutine playing. It's going to take us back to the "connecting" state if we don't stop it
        if (_joinRandomHostCoroutine != null)
        {
            StopCoroutine(_joinRandomHostCoroutine);
            _joinRandomHostCoroutine = null;
        }

        _connectionType = ConnectionType.None;
        _networkManager.StopClient();
        _connectingPanel.SetActive(false);
        _connectionPanel.SetActive(true);
        _globalLeaderboardUI.SetActive(true);
        _connecting = false;
    }

    public void ShopButton()
    {
        SceneManager.LoadScene(1);
    }

    private void OnClientStopped()
    {
        // If the client stops when we are joining a random host, it means we found an active server but couldn't join, this means we are in the threshold time period between the moment the server was shut down and the moment it starts being considered inactive: 35 second
        // So we make another attempt
        SetConnectionError("CONNECTION FAILED");
    }

    public void DiconnectButton()
    {
        GameManager.Instance.SaveManager.UpdateConnected(false);
        InitializeUI();
    }

    #endregion

    #region Authentification Methods

    public void SignUp()
    {
        if (_signupPlayerNameInputField.text.Trim() == "")
        {
            _signUpErrorText.text = "Nickname is necessary!";
            return;
        }

        if (_signUpEmailInputField.text.Trim() == "")
        {
            _signUpErrorText.text = "Email is necessary!";
            return;
        }

        if (_signUpPasswordInputField.text.Trim() == "")
        {
            _signUpErrorText.text = "Password is necessary";
            return;
        }

        if (!Bloodthirst.Utils.IsEmail(_signUpEmailInputField.text))
        {
            _signUpErrorText.text = "Email must be valid!";
            return;
        }

        _signUpForm.SetActive(false);
        _authenticationPendingUI.SetActive(true);

        SavedData savedData = GameManager.Instance.SaveManager.SavedData;
        savedData.Nickname = _signupPlayerNameInputField.text;
        savedData.Email = _signUpEmailInputField.text;
        savedData.Password = _signUpPasswordInputField.text;

        GameManager.Instance.SaveManager.Save(savedData);

        UserInLocal userInLocal = new UserInLocal(_signUpEmailInputField.text, _signUpPasswordInputField.text, _signupPlayerNameInputField.text);
        _networkUtils.SignUp(userInLocal, (e) =>
        {
            if (e != null)
            {
                _signUpErrorText.text = e.Message;
                _signUpForm.SetActive(true);
                _authenticationPendingUI.SetActive(false);
            } else
            {
                // After we are done authenticating, we go to the connection panel
                GameManager.Instance.SaveManager.UpdateConnected(true);
                InitializeUI();
            }
        });
    }

    public void SignIn()
    {
        if (_signInPlayerNameOrEmailInputField.text.Trim() == "")
        {
            _signInErrorText.text = "Nickname or email is necessary!";
            return;
        }

        if (_signInPasswordInputField.text.Trim() == "")
        {
            _signInErrorText.text = "Password is necessary";
            return;
        }

        _signInForm.SetActive(false);
        _authenticationPendingUI.SetActive(true);

        SavedData savedData = GameManager.Instance.SaveManager.SavedData;
        // If the entered text is an email, then we save the email in playerprefs
        // Else, we save the playername
        if (Bloodthirst.Utils.IsEmail(_signInPlayerNameOrEmailInputField.text)) {
            savedData.Email = _signInPlayerNameOrEmailInputField.text;
        } else
        {
            savedData.Nickname = _signInPlayerNameOrEmailInputField.text;
        }
        savedData.Password = _signInPasswordInputField.text;

        GameManager.Instance.SaveManager.Save(savedData);

        _networkUtils.SignIn(_signInPlayerNameOrEmailInputField.text, _signInPasswordInputField.text, (e) =>
        {
            if (e != null)
            {
                _signInErrorText.text = e.Message;
                _signInForm.SetActive(true);
                _authenticationPendingUI.SetActive(false);
            } else
            {
                GameManager.Instance.SaveManager.UpdateConnected(true);
                // After we are done authenticating, we go to the connection panel
                _authentificationPanel.SetActive(false);
                _connectionPanel.SetActive(true);
                _globalLeaderboardUI.SetActive(true);
            }
        });
    }

    public void ToSignUp()
    {
        _signInForm.SetActive(false);
        _signUpForm.SetActive(true);
    }

    public void ToSignIn()
    {
        _signUpForm.SetActive(false);
        _signInForm.SetActive(true);
    }

    public void InitializeUI()
    {
        _errorText.text = "";
        _connectingPanel.SetActive(false);
        _connectionPanel.SetActive(false);

        _signUpErrorText.text = "";
        _signInErrorText.text = "";
        _authentificationPanel.SetActive(true);
        _signUpForm.SetActive(true);
        _signInForm.SetActive(false);
        _authenticationPendingUI.SetActive(false);


        if (GameManager.Instance.SaveManager.IsConnected)
        {
            _connectionPanel.SetActive(true);
            _globalLeaderboardUI.SetActive(true);
            _authentificationPanel.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

    private void InitializeSavedData()
    {
        SavedData savedData = GameManager.Instance.SaveManager.SavedData;
        if (savedData != null)
        {
            _signupPlayerNameInputField.text = savedData.Nickname;
            _signUpEmailInputField.text = savedData.Email;
            _signUpPasswordInputField.text = savedData.Password;

            _signInPlayerNameOrEmailInputField.text = savedData.Nickname;
            _signInPasswordInputField.text = savedData.Password;
        }
    }

    #endregion

    #region Handling Firebase database

    private void AddHostIPEntry()
    {
        if (_connectionType != ConnectionType.PublicHost) return;

        Debug.Log("-----------Adding host IP entry-----------");

        // Making a server
        NetworkServers.Server server = new NetworkServers.Server();
        server.HostIP = LocalIP;

        _networkUtils.PostServer(server, () => {
            NetworkServers.UserConnection user = new NetworkServers.UserConnection();
            user.connectionId = GameManager.Instance.LocalPlayer.connectionToServer.connectionId;

            _networkUtils.AddClientToServer(LocalIP, user);
        });
    }

    private void RemoveHostIPEntry()
    {
        if (_connectionType != ConnectionType.PublicHost) return;

        Debug.Log("-----------Removing host IP entry-----------");

        _networkUtils.RemoveServer(LocalIP);
    }

    private void AddClientToHost(NetworkConnection networkConnection)
    {
        if (_connectionType != ConnectionType.PublicHost || networkConnection.connectionId == 0) return;
        Debug.Log("-----------Adding Client to host-----------");

        //return;
        NetworkServers.UserConnection user = new NetworkServers.UserConnection();
        user.connectionId = networkConnection.connectionId;

        _networkUtils.AddClientToServer(LocalIP, user);
    }

    private void RemoveClientFromHost(NetworkConnection networkConnection)
    {
        if (_connectionType != ConnectionType.PublicHost) return;

        Debug.Log("-----------Removing Client from host-----------");

        _networkUtils.RemoveClientFromServer(LocalIP, networkConnection.connectionId);
    }

    #endregion
}
 