using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SoundEmitter : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _tauntSounds = new List<AudioClip>();
    [SerializeField] private float _radius = 10f;
    [SerializeField] private float _deplationTime = 4f;

    private SphereCollider _collider = null;
    private IEnumerator _coroutine = null;
    private bool _isForLocalPlayer = false;

    public void SetForLocalPlayer()
    {
        _isForLocalPlayer = true;
        // If the sound emitter is that of the player, then we set the parent to null, to avoid
        // having the zombies run after player
        transform.SetParent(null);
    }

    private void Start()
    {
        _collider = GetComponent<SphereCollider>();
        _collider.radius = 0f;
        _collider.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.InputManager.T && _isForLocalPlayer)
        {
            GameManager.Instance.LocalPlayer.CmdPlayerEmitSound();
        }
    }

    public void EmitSound()
    {
        // Only the server handles the sound emission
        if (!GameManager.Instance.LocalPlayer.isServer) return;

        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = TauntCoroutine();
        StartCoroutine(_coroutine);
    }

    private IEnumerator TauntCoroutine()
    {
        _collider.enabled = true;

        if (_tauntSounds.Count > 0)
        {
            AudioClip clip = _tauntSounds[Random.Range(0, _tauntSounds.Count)];
            if (clip != null)
            {
                GameManager.Instance.AudioManager.PlayOneShotSound(clip, 1, 1, 1, transform.position);
            }
        }

        _collider.radius = _radius;

        float timer = 0f;
        while (timer < _deplationTime)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / _deplationTime;

            _collider.radius = _radius * (1 - normalizedTime);

            yield return null;
        }

        _collider.radius = 0f;

        _collider.enabled = false;

        _coroutine = null;
    }
}
