using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public class Tool
{
    public Sprite Sprite = null;
    public SpawnedTool SpawnedTool = null;
    public Transform Decoration = null;
    public bool InPossession = false;
    public Transform SpawnPosition = null;
    public string AnimationName = "PlaceMine";
    public bool Stop = true;
}

public class PlayerTools : NetworkBehaviour
{
    [SerializeField] private List<Tool> _tools = new List<Tool>();
    [SerializeField] private Animator _animator = null;

    [Header("Sounds")]
    [SerializeField] private AudioClip _switchProjectileSound = null;
    [SerializeField] private AudioClip _grenadeSafetyPinSound = null;

    private int _selectedToolIndex = -1;
    private Player _player = null;

    public Tool SelectedTool { get { if (_selectedToolIndex != -1) return _tools[_selectedToolIndex]; return null;  } }
    public int SelectedToolIndex { get { return _selectedToolIndex; } }
    public List<Tool> Tools { get { return _tools; } }

    #region Monobehavior Callbacks

    private void Start()
    {
        _player = GetComponent<Player>();

        // A new coming player is going to get the tools state of all other players
        if (_player.isLocalPlayer)
        {
            Player[] players = FindObjectsOfType<Player>();
            foreach(Player player in players)
            {
                CmdGetToolsStateFromServer(player.netId);
            }

            CmdSwitchTool(1);
        }
    }

    private void Update()
    {
        // Handling showing tools
        foreach(Tool tool in _tools)
        {
            tool.Decoration.gameObject.SetActive(tool.InPossession);
        }

        if (!isLocalPlayer) return;

        if (GameManager.Instance.InputManager.P && SelectedTool != null
            && _player.CharacterController.isGrounded)
        {
            bool use = true;
            if (SelectedTool.Stop && !_player.CharacterController.isGrounded)
            {
                use = false;
            }

            if (use)
                CmdThrowAnimation();
        }

        #region Switching tools buttons
        if (GameManager.Instance.InputManager.O)
        {
            CmdSwitchTool(1);
        }
        if(GameManager.Instance.InputManager.I)
        {
            CmdSwitchTool(-1);
        }
        #endregion
    }

    #endregion

    #region Synchronizing tools for new coming players

    [Command]
    private void CmdGetToolsStateFromServer(uint playerNetId)
    {
        Player[] players = FindObjectsOfType<Player>();
        List<Player> playersList = new List<Player>();
        foreach (Player player in players)
        {
            playersList.Add(player);
        }
        PlayerTools playerTools = playersList.Find(player => player.netId == playerNetId).PlayerTools;

        bool[] inPossession = new bool[playerTools._tools.Count];
        for (int i = 0; i < playerTools._tools.Count; i++)
        {
            inPossession[i] = playerTools._tools[i].InPossession;
        }

        RpcUpdateToolsStateForEveryone(inPossession, playerTools._selectedToolIndex, playerNetId);
    }

    [ClientRpc]
    private void RpcUpdateToolsStateForEveryone(bool[] inPossession, int selectedToolIndex, uint playerNetId)
    {
        Player[] players = FindObjectsOfType<Player>();
        List<Player> playersList = new List<Player>();
        foreach (Player player in players)
        {
            playersList.Add(player);
        }
        PlayerTools playerTools = playersList.Find(player => player.netId == playerNetId).PlayerTools;

        for (int i = 0; i < playerTools._tools.Count; i++)
        {
            playerTools._tools[i].InPossession = inPossession[i];
        }

        playerTools._selectedToolIndex = selectedToolIndex;
    }

    #endregion

    #region Using Tools

    [Command]
    private void CmdThrowAnimation()
    {
        RpcThrowAnimation();
    }

    [ClientRpc]
    private void RpcThrowAnimation()
    {
        _animator.SetTrigger(SelectedTool.AnimationName);
    }

    [Command]
    public void CmdUseTool(Vector3 throwDestination)
    {
        if (SelectedTool == null) return;

        if (SelectedTool.SpawnPosition != null)
        {
            SpawnedTool spawnedTool = Instantiate(SelectedTool.SpawnedTool, SelectedTool.SpawnPosition.position, Quaternion.identity);
            spawnedTool.ThrowerIdentifier = netId;
            // Initializing to add the force for the throwable
            spawnedTool.InitializeThrow(throwDestination);
            NetworkServer.Spawn(spawnedTool.gameObject, GameManager.Instance.LocalPlayer.connectionToClient);
        }

        RpcUseTool();
    }

    [ClientRpc]
    private void RpcUseTool()
    {
        _tools[_selectedToolIndex].InPossession = false;
        SwitchTool(1);
    }

    #endregion

    #region Switching Tool

    [Command]
    private void CmdSwitchTool(int direction)
    {
        RpcSwitchTool(direction);
    }

    [ClientRpc]
    private void RpcSwitchTool(int direction)
    {
        SwitchTool(direction);
    }

    private void SwitchTool(int direction)
    {
        int indexOfCurrentlySelectedToolInInPossessionTools = 0;
        // Create a temporary list of tools that are in possession
        List<Tool> inPossessionTools = new List<Tool>();
        foreach(Tool tool in _tools)
        {
            if (tool.InPossession)
            {
                inPossessionTools.Add(tool);
            }
        }

        // If we don't have any tools, we merely return
        if (inPossessionTools.Count <= 0) {
            _selectedToolIndex = -1;
            return;
        }

        if (_selectedToolIndex == -1) _selectedToolIndex = 0;
        for (int i = 0; i < inPossessionTools.Count; i++)
        {
            if (inPossessionTools[i] == SelectedTool)
            {
                indexOfCurrentlySelectedToolInInPossessionTools = i;
                break;
            }
        }

        indexOfCurrentlySelectedToolInInPossessionTools += direction;

        if (indexOfCurrentlySelectedToolInInPossessionTools >= inPossessionTools.Count)
        {
            indexOfCurrentlySelectedToolInInPossessionTools = 0;
        }
        if (indexOfCurrentlySelectedToolInInPossessionTools < 0)
            indexOfCurrentlySelectedToolInInPossessionTools = inPossessionTools.Count - 1;


        if (_switchProjectileSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_switchProjectileSound, 1, 0, 2, transform.position);
        }

        for (int i = 0; i < _tools.Count; i++)
        {
            if (_tools[i] == inPossessionTools[indexOfCurrentlySelectedToolInInPossessionTools])
            {
                _selectedToolIndex = i;
                break;
            }
        }
    }

    #endregion

    #region Obtaining Tool

    [Command]
    public void CmdObtainTools(int[] indexes)
    {
        RpcObtainTools(indexes);
    }

    [ClientRpc]
    private void RpcObtainTools(int[] indexes)
    {
        foreach (int index in indexes)
        {
            if (_tools.Count > index)
            {
                _tools[index].InPossession = true;
            }
        }

        // We switch to the obtained tool
        _selectedToolIndex = indexes[0];
    }

    #endregion

    #region Grenade Safety Pin Sound

    public void PlayGrenadeSafetyPinSound()
    {
        GameManager.Instance.AudioManager.PlayOneShotSound(_grenadeSafetyPinSound, 1, _player == GameManager.Instance.LocalPlayer == _player ? 0 : 1, 1, transform.position);
    }

    #endregion
}
