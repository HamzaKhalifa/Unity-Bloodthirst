using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerMove : NetworkBehaviour
{
    [SerializeField] private float _gravityForce = 5f;
    [SerializeField] private float _jumpForce = 50f;
    [SerializeField] private bool _canJump = false;

    [SerializeField] private AudioClip _jumpClip = null;
    [SerializeField] private AudioClip _landClip = null;

    private float _vSpeed = 0f;
    private bool _isGrounded = false;

    private PlayerAnimation _playerAnimation;
    public PlayerAnimation PlayerAnimation
    {
        get
        {
            if (_playerAnimation == null)
                _playerAnimation = GetComponent<PlayerAnimation>();

            return _playerAnimation;
        }
    }

    private void Start()
    {
        if (!isLocalPlayer) enabled = false;
    }

    public void Move(Vector2 direction)
    {
        CharacterController characterController = GameManager.Instance.LocalPlayer.CharacterController;

        if (characterController == null) return;

        // X and Z movement
        Vector3 desiredMovement = transform.forward * direction.x * Time.deltaTime + transform.right * direction.y * Time.deltaTime;

        // Handle jumping
        if (characterController.isGrounded)
        {
            _vSpeed = 0;
            if (GameManager.Instance.InputManager.Space && _canJump && characterController.isGrounded)
            {
                // Play jump sound
                if (_jumpClip != null)
                {
                    GameManager.Instance.AudioManager.PlayOneShotSound(_jumpClip, 1, 0, 1);
                }

                // We directly set the player animation to landing/jumping (forgetting about the threshold that we set)
                PlayerAnimation.SetInAir();

                _vSpeed = _jumpForce;
            }
        }

        _vSpeed -= _gravityForce * Time.deltaTime;
        desiredMovement.y = _vSpeed;

        characterController.Move(desiredMovement);

        HandleLandSound();
    }

    #region For landing sound

    private void HandleLandSound()
    {
        CharacterController characterController = GameManager.Instance.LocalPlayer.CharacterController;
        if (characterController.isGrounded && !_isGrounded && _landClip != null)
        {
            CmdPlayLandSound();
        }
        _isGrounded = GameManager.Instance.LocalPlayer.CharacterController.isGrounded;
    }

    [Command]
    private void CmdPlayLandSound()
    {
        RpcPlayLandSound();
    }

    [ClientRpc]
    private void RpcPlayLandSound()
    {
        GameManager.Instance.AudioManager.PlayOneShotSound(_landClip, 1, 1, 1, transform.position);
    }

    #endregion
}
