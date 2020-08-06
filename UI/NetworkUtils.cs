using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Proyecto26;
using System;

[System.Serializable]
public class NetworkServers
{
    [System.Serializable]
    public class UserConnection
    {
        public int connectionId = 0;
    }

    [System.Serializable]
    public class Server
    {
        public string HostIP = null;
        public List<UserConnection> Users = new List<UserConnection>();
        // Using universal time to store the last moment the server was modified
        // So that clients only try to join recently modified servers (the ones that are active)
        public long timestamp = DateTime.UtcNow.ToBinary();

    };

    public List<Server> Servers = new List<Server>();
}

[System.Serializable]
public class User
{
    public string Nickname = null;
    public string Email = null;
    public int BestScore = 0;
}

[System.Serializable]
public class UserInLocal
{
    public string email = null;
    public string password = null;
    public string nickname = null;

    public UserInLocal(string email, string password, string nickname)
    {
        this.email = email;
        this.password = password;
        this.nickname = nickname;
    }
}

[Serializable]
public class FirebaseError
{
    [Serializable]
    public class SubError
    {
        public int code = 0;
        public string message = null;
    }

    public SubError error = new SubError();
}

[Serializable]
public class FirebaseSignResponse
{
    public string localId = null;
}

public class NetworkUtils : MonoBehaviour
{
    private const string projectId = "bloodthirst-b8c4f"; // Found in Firebase project settings
    public static readonly string DatabaseUrl = $"https://{projectId}.firebaseio.com/";
    public static readonly string ApiKey = "AIzaSyD3TVB2KH7eTPJWMdYoKPhgk5BdgI7e0Rw";
    private string signUpBaseEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";
    private string signInBaseEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";

    private NetworkHUD _networkHUD = null;
    private FirebaseSignResponse _firebaseSignResponse = null;

    #region IP

    public static string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "0.0.0.0";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    private void Start()
    {
        _networkHUD = GetComponent<NetworkHUD>();
    }

    #endregion

    #region Authentication

    public void SignUp(UserInLocal userInLocal, System.Action<Exception> callback)
    {
        VerifyIfNicknameExists(userInLocal.nickname, (e) => {
            if (e != null)
            {
                callback(e);
            } else
            {
                SignUpWithEmailAndPassword(userInLocal, callback);
            }
        });
    }

    private void VerifyIfNicknameExists(string nickname, System.Action<Exception> callback)
    {
        // Before we sign up, we need to make sure that the nickname hasn't already been taken
        RestClient.Request(new RequestHelper
        {
            Uri = DatabaseUrl + "UsersByNickname/" + nickname + ".json",
            Method = "GET"
        }).Then(verifyExistingUserResponse =>
        {
            User userToVerify = JsonUtility.FromJson<User>(verifyExistingUserResponse.Text);
            if (userToVerify == null)
            {
                callback(null);
            }
            else
            {
                callback(new Exception("Nickname is already taken"));
            }
        }).Catch(e => {
            // This error is special in a way that when raises, it means that we don't have the nickname stored
            // It actually means that we don't have any user at all. So we are safe to create the user
            // Means, we call the callback with no exception given.
            callback(null);
        });
    }

    public void SignUpWithEmailAndPassword(UserInLocal userInLocal, System.Action<Exception> callback)
    {
        RestClient.Request(new RequestHelper
        {
            Uri = signUpBaseEndpoint + ApiKey,
            Method = "Post",
            Params = new Dictionary<string, string>
            {
                { "email", userInLocal.email },
                { "password", userInLocal.password }
            },
            IgnoreHttpException = true
        }).Then(response => {
            FirebaseSignResponse firebaseSignResponse = JsonUtility.FromJson<FirebaseSignResponse>(response.Text);
            if (response.StatusCode == 200)
            {
                User user = new User();
                user.Nickname = userInLocal.nickname;
                user.Email = userInLocal.email;
                // Now we create an instance of the user in the database
                RestClient.Request(new RequestHelper
                {
                    Method = "PUT",
                    Uri = DatabaseUrl + "UsersByNickname/" + user.Nickname + ".json",
                    Body = user
                }).Then(responseFromUserCreation => {
                    if (firebaseSignResponse == null)
                    {
                        callback(new Exception("Couldn't create user instance"));
                    } else
                    {
                        // Now we create another instance of the user, but now using his id as the identifier(This is necessary so we are able to retrieve the user nickname when he signs up using only his email)
                        RestClient.Request(new RequestHelper
                        {
                            Method = "PUT",
                            Uri = DatabaseUrl + "UsersById/" + firebaseSignResponse.localId + ".json",
                            Body = user
                        }).Then(responseFromUserByEmailCreation => {
                            callback(null);
                        }).Catch(e => {
                            callback(e);
                        });
                    }
                }).Catch(e => {
                    callback(e);
                });
            } else {
                FirebaseError firebaseError = JsonUtility.FromJson<FirebaseError>(response.Text);
                if (firebaseError.error.message == "EMAIL_EXISTS")
                    firebaseError.error.message = "Email already exists";

                callback(new Exception(firebaseError.error.message));
            } 
        }).Catch(e => {
            callback(e);
        });
    }

    public void SignIn(string emailOrNickname, string password, System.Action<Exception> callback)
    {
        // If the string passsed is an email, then we are going to be using the firebase signin endpoint
        if (Bloodthirst.Utils.IsEmail(emailOrNickname))
        {
            SignInWithEmail(emailOrNickname, password, (e) =>
            {
                // Now, we try to retrieve the user's nickname and store it in playerprefs
                if (e != null)
                {
                    callback(e);
                } else
                {
                    RestClient.Request(new RequestHelper
                    {
                        Uri = DatabaseUrl + "UsersById/" + _firebaseSignResponse.localId + ".json",
                        Method = "GET"
                    }).Then(response => {
                        User user = JsonUtility.FromJson<User>(response.Text);
                        if (user == null)
                        {
                            callback(new Exception("User not found"));
                        } else
                        {
                            // Now that we got the user nickname, we finally store it in playerprefs
                            SavedData savedData = GameManager.Instance.SaveManager.SavedData;
                            savedData.Nickname = user.Nickname;
                            GameManager.Instance.SaveManager.Save(savedData);
                            callback(null);
                        }
                    }).Catch(e1 => { callback(e1); });
                }
            });
        } else
        {
            RestClient.Request(new RequestHelper
            {
                Uri = DatabaseUrl + "UsersByNickname/" + emailOrNickname + ".json",
                Method = "GET",
            }).Then(response =>
            {
                User user = JsonUtility.FromJson<User>(response.Text);
                if (user == null)
                {
                    callback(new Exception("User not found"));
                } else
                {
                    // Now that we found the user's email, we sign in using his email:
                    SignInWithEmail(user.Email, password, callback);
                }
            }).Catch(e => {
                callback(new Exception("User not found"));
            });
        }
    }

    private void SignInWithEmail(string email, string password, System.Action<Exception> callback)
    {
        RestClient.Request(new RequestHelper
        {
            Uri = signInBaseEndpoint + ApiKey,
            Method = "POST",
            Params = new Dictionary<string, string>
                {
                    { "email", email },
                    { "password", password }
                },
            IgnoreHttpException = true
        }).Then(response => {
            _firebaseSignResponse = JsonUtility.FromJson<FirebaseSignResponse>(response.Text);
            if (response.StatusCode == 200)
            {
                // When I sign in using an email, I still need to get the username of the signed in person
                // Sign in with email is successful, so we call the callback with no error
                callback(null);
            }
            else
            {
                FirebaseError firebaseError = JsonUtility.FromJson<FirebaseError>(response.Text);
                if (firebaseError.error.message == "EMAIL_NOT_FOUND")
                    firebaseError.error.message = "Email Not Found";

                if (firebaseError.error.message == "INVALID_PASSWORD")
                    firebaseError.error.message = "Invalid Password";

                callback(new Exception(firebaseError.error.message));
            }
        }).Catch(e => {
            callback(e);
        });
    }

    #endregion

    #region Basic Network Data request

    public void GetNetworkServers(System.Action<NetworkServers> callback)
    {
        RestClient.Get<NetworkServers>($"{DatabaseUrl}NetworkServers.json").Then(networkData =>
        {
            callback(networkData);
        }).Catch(error => { Debug.Log(error.Message); _networkHUD.SetConnectionError(error.Message); });
    }

    public void PostNetworkData(NetworkServers networkData, System.Action callback)
    {
        RestClient.Put<NetworkServers>(DatabaseUrl + "NetworkServers.json", networkData).Then(response => {
            callback();
        }).Catch(error => { Debug.Log(error.Message); _networkHUD.SetConnectionError(error.Message); });
    }

    public void GetRandomServerHost(System.Action<string> callback)
    {
        List<NetworkServers.Server> availableServers = new List<NetworkServers.Server>();

        GetNetworkServers(networkServers =>
        {
            foreach (NetworkServers.Server server in networkServers.Servers)
            {
                // Only join when the number of users is inferior to 4
                // and superior to 0 so to ensure the host is there
                if (server.Users.Count < 4 && server.Users.Count > 0)
                {
                    byte[] utcNowBytes = BitConverter.GetBytes(server.timestamp);
                    long utcNowLongBack = BitConverter.ToInt64(utcNowBytes, 0);
                    DateTime utcNowBack = DateTime.FromBinary(utcNowLongBack);

                    // We only join servers that have been recently updated (active servers that weren't left by the client)
                    if ((DateTime.UtcNow - utcNowBack).TotalSeconds <= NetworkHUD.UPDATE_SERVER_DELAY + NetworkHUD.UPDATE_ERROR_MARGIN)
                        availableServers.Add(server);
                }
            }
            if (availableServers.Count > 0)
            {
                callback(availableServers[UnityEngine.Random.Range(0, availableServers.Count)].HostIP);
            } else callback(null);
        });
    }

    #endregion

    #region Server creation/deletion/Update

    public void UpdateServer(string hostIP)
    {
        GetNetworkServers(networkServers =>
        {
            foreach(NetworkServers.Server server in networkServers.Servers)
            {
                if (server.HostIP == hostIP)
                {
                    server.timestamp = DateTime.UtcNow.ToBinary();
                    PostNetworkData(networkServers, () =>
                    {
                        Debug.Log("-----------Server has been updated. Means server is active-----------");
                    });
                    break;
                }
            }
        });
    }

    public void PostServer(NetworkServers.Server server, System.Action addHostAsClient)
    {
        GetNetworkServers((NetworkServers) =>
        {
            bool serverAlreadyExists = false;
            // If the server already exists, then we exit
            foreach (NetworkServers.Server serverInNetworkData in NetworkServers.Servers)
            {
                if (serverInNetworkData.HostIP == server.HostIP)
                {
                    serverAlreadyExists = true;
                    serverInNetworkData.Users.Clear();
                    // We update the last connection time of the server
                    serverInNetworkData.timestamp = DateTime.UtcNow.ToBinary();
                    break;
                }
            }

            if (!serverAlreadyExists)
                NetworkServers.Servers.Add(server);

            PostNetworkData(NetworkServers, () =>
            {
                Debug.Log("-----------Server added in the database-----------");
                addHostAsClient();
            });

        });
    }

    public void RemoveServer(string serverIP)
    {
        GetNetworkServers(networkData =>
        {
            networkData.Servers.RemoveAll(server => server.HostIP == serverIP);
            PostNetworkData(networkData, () =>
            {
                Debug.Log("-----------Server removed from the database-----------");
            });
        });
    }

    #endregion

    #region Client Addition/Removal

    public void AddClientToServer(string serverIP, NetworkServers.UserConnection user)
    {
        GetNetworkServers(networkServers =>
        {
            foreach (NetworkServers.Server serverInNetworkData in networkServers.Servers)
            {
                if (serverInNetworkData.HostIP == serverIP)
                {
                    serverInNetworkData.Users.Add(user);
                    break;
                }
            }

            PostNetworkData(networkServers, () =>
            {
                Debug.Log("-----------Client added to the database-----------");
            });
        });
    }

    public void RemoveClientFromServer(string serverIP, int connectionId)
    {
        GetNetworkServers(networkServers =>
        {
            foreach (NetworkServers.Server serverInNetworkData in networkServers.Servers)
            {
                if (serverInNetworkData.HostIP == serverIP)
                {
                    serverInNetworkData.Users.RemoveAll(user => user.connectionId == connectionId);

                    break;
                }
            }

            PostNetworkData(networkServers, () =>
            {
                Debug.Log("-----------Client removed from the database-----------");
            });
        });
    }

    #endregion

    #region Score Handling

    public void UpdatePlayerBestScore(string playerNickname, int currentPlayerScore)
    {
        // We get the player's score first
        RestClient.Request(new RequestHelper
        {
            Uri = DatabaseUrl + "UsersByNickname/" + playerNickname + ".json",
            Method = "GET"
        }).Then(response =>
        {
            User user = JsonUtility.FromJson<User>(response.Text);
            if (user != null && user.BestScore < currentPlayerScore)
            {
                user.BestScore = currentPlayerScore;
                RestClient.Request(new RequestHelper
                {
                    Uri = DatabaseUrl + "UsersByNickname/" + playerNickname + ".json",
                    Method = "PUT",
                    Body = user
                }).Then(updatedScoreResponse => {
                    Debug.Log("Best score for player " + playerNickname + " has been updated.");
                }).Catch(e => { Debug.Log(e.Message); });
            }
        }).Catch(e => { Debug.Log(e.Message); });
    }

    #endregion
}
