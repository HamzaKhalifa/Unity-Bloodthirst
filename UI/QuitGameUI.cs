using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class QuitGameUI : MonoBehaviour
{
    [SerializeField] private GameObject _quitPanel = null;
    [SerializeField] private AudioClip _activeSound = null;
    [SerializeField] private AudioClip _inactiveSound = null;

    private NetworkHUD _networkHUD = null;
    public NetworkHUD NetworkHUD
    {
        get
        {
            if (_networkHUD == null)
                _networkHUD = FindObjectOfType<NetworkHUD>();

            return _networkHUD;
        }
    }

    private void Start()
    {
        _quitPanel.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance.InputManager.Escape)
        {
            bool active = !_quitPanel.activeSelf;
            _quitPanel.gameObject.SetActive(active);

            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = active;

            GameManager.Instance.AudioManager.PlayOneShotSound(active ? _activeSound : _inactiveSound, 1, 0, 2, transform.position);
        }
    }

    public void QuitGame()
    {
        _quitPanel.SetActive(false);
        NetworkHUD.NetworkManager.StopClient();
        if (GameManager.Instance.LocalPlayer.isServer)
        {
            NetworkHUD.NetworkManager.StopServer();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
