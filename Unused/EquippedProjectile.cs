using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EquippedProjectile : MonoBehaviour
{
    [SerializeField] private string _animationName = "ThrowGrenade";
    [SerializeField] private Transform _hand = null;
    [SerializeField] private bool _inPossession = false;
    [SerializeField] private Transform _decoration = null;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private GameObject _mesh = null;
    [SerializeField] private float _explosionDelay = 5f;
    [SerializeField] private Explosion _explosion = null;
    [SerializeField] private Sprite _sprite = null;

    #region Cache Fields

    private Rigidbody _rigidBody = null;
    private PlayerProjectile _playerProjectile = null;

    #endregion

    #region Public Accessors

    public bool InPossession { get { return _inPossession; } set {
            _inPossession = value;
    } }
    public PlayerProjectile PlayerProjectile { set { _playerProjectile = value; } }
    public Sprite Sprite { get { return _sprite; } }
    public IEnumerator Coroutine { get { return _coroutine; } }
    public string AnimationName { get { return _animationName; } }

    #endregion

    private IEnumerator _coroutine = null;

    #region Monobehavior Callback

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        ReInitialize();
    }

    private void Update()
    {
        _decoration.gameObject.SetActive(_inPossession);
    }

    #endregion

    public void Throw(Vector3 throwDestination)
    {
        _inPossession = false;
        _mesh.gameObject.SetActive(true);
        _rigidBody.isKinematic = false;
        transform.parent = null;

        Vector3 throwDirection = (throwDestination - transform.position).normalized;
        _rigidBody.AddForce(throwDirection * _throwForce);

        _coroutine = Explode(throwDirection);
        StartCoroutine(_coroutine);
    }

    private IEnumerator Explode(Vector3 direction)
    {
        yield return null;

        _rigidBody.AddForce(direction * _throwForce);

        yield return new WaitForSeconds(_explosionDelay);

        Explosion explosion = Instantiate(_explosion, transform.position, Quaternion.identity);
        explosion.ThrowerIdentifier = _playerProjectile.netId;

        // We nullify the coroutine before the reinitialization because this one will need to test if the couroutine is null or not
        _coroutine = null;

        // To put the grenade back and unmoving in the player's hand
        ReInitialize();
    }

    #region Reininialization

    private void ReInitialize()
    {
        if (_coroutine != null)
        {
            Invoke("RpcReInitialize", _explosionDelay);
            return;
        }
        _rigidBody.isKinematic = true;
        _mesh.gameObject.SetActive(false);
        InitializePosition();
    }

    private void InitializePosition()
    {
        transform.SetParent(_hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    #endregion
}
