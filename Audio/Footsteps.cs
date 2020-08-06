using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _default = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _metal = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _wood = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _grass = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _gravel = new List<AudioClip>();

    [SerializeField] private float _timeBetweenSteps = .2f;
    [SerializeField] private Transform _rayOrigin = null;


    #region Cache Fields

    private CharacterController _characterController = null;

    #endregion

    private Dictionary<int, List<AudioClip>> _layerClips = new Dictionary<int, List<AudioClip>>();
    private List<AudioClip> _currentClips = new List<AudioClip>();
    private float _lastStepTime = 0f;
    private bool _isForLocalPlayer = false;
    private Player _player = null;

    #region Monobehavior Callbacks
    
    private void Awake()
    {
        GameManager.Instance.OnLocalPlayerJoined += (player) =>
        {
            Player playerComponent = GetComponentInParent<Player>();
            if (playerComponent != null)
            {
                if (playerComponent.gameObject == player.gameObject)
                {
                    _isForLocalPlayer = true;
                }
            }
        };
    }

    private void Start()
    {
        _layerClips.Add(LayerMask.NameToLayer("Default"), _default);
        _layerClips.Add(LayerMask.NameToLayer("Metal"), _metal);
        _layerClips.Add(LayerMask.NameToLayer("Wood"), _wood);
        _layerClips.Add(LayerMask.NameToLayer("Grass"), _grass);
        _layerClips.Add(LayerMask.NameToLayer("Gravel"), _gravel);

        _characterController = GetComponentInParent<CharacterController>();

        _player = GetComponentInParent<Player>();

        if (_rayOrigin == null) _rayOrigin = transform;
    }

    private void Update()
    {
        
    }

    #endregion

    public void PlayFootstep()
    {
        // We only play footsteps when we are grounded
        if ((_characterController != null && !_characterController.isGrounded) || (_player != null && !_player.CanMove)) return;

        if (_player != null)
        {
            GameManager.Instance.LocalPlayer.CmdPlayFootstep();
        } else
        {
            ActualFootstepPlay();
        }
    }

    public void ActualFootstepPlay()
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(_rayOrigin.position, Vector3.down, out hitInfo, float.MaxValue, LayerMask.GetMask("Default", "Metal", "Wood", "Grass", "Gravel")))
        {
            _layerClips.TryGetValue(hitInfo.transform.gameObject.layer, out _currentClips);
        }

        if (Time.time < _lastStepTime + _timeBetweenSteps || _currentClips == null || _currentClips.Count == 0) return;
        _lastStepTime = Time.time;

        AudioClip clip = _currentClips[Random.Range(0, _currentClips.Count)];
        if (clip != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(clip, 1, _isForLocalPlayer ? 0 : 1, 1, transform.position);
        }
    }
}
