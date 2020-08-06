using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Shooter : MonoBehaviour
{
    public enum EGunType {
        Handgun,
        Rifle
    }

    public enum CheckFireAction
    {
        PlayDrySound,
        DontFire,
        DoFire
    }

    [SerializeField] private bool _inPossession = false;
    [SerializeField] private EGunType _gunType = EGunType.Rifle;
    [SerializeField] private Sprite _sprite = null;
    [SerializeField] private Transform _hand = null;
    [SerializeField] private float _rateOfFire = .3f;
    [SerializeField] private float _range = float.MaxValue;
    [SerializeField] private float _reticleInaccuracy = 20f;
    [SerializeField] private float _aimReticleInaccuracy = 10f;
    [SerializeField] private float _reticleRecoverySpeed = 5f;
    [SerializeField] private float _aimReticleRecoverySpeed = 2.5f;
    [SerializeField] private float _instability = 20f;
    [SerializeField] private float _maxInstability = 150f;
    [SerializeField] private float _aimInstability = 10f;
    [SerializeField] private float _destabilizationSpeed = .05f;
    [SerializeField] private float _aimDestabilizationSpeed = .025f;
    [SerializeField] private float _recoil = 4f;
    [SerializeField] private float _aimSpeed = 10f;
    [SerializeField] private float _pushForce = 0f;
    [SerializeField] private float _damage = 1f;
    [SerializeField] private GameObject _bulletHole = null;
    [SerializeField] private Transform _decoration = null;
    [Tooltip("The weapon's mesh needs to be hidden when we zoom in with a scope")]
    [SerializeField] private GameObject _mesh = null;

    [Header("Shotgun Like Weapns")]
    [SerializeField] private int _numberOfBulletsFiredAtOnce = 1;
    [SerializeField] private float _spread = 5f;

    [Header("Scope")]
    [SerializeField] private bool _hasScope = false;
    [SerializeField] private float _zoom = 30f;

    [Header("Instantiated Bullet")]
    [SerializeField] private Bullet _bullet = null;
    [SerializeField] private Transform _firePosition = null;

    [Header("Sounds")]
    [SerializeField] private AudioClip _fireSound = null;
    [SerializeField] private AudioClip _drySound = null;
    [SerializeField] private AudioClip _scopeSound = null;

    #region Public Accessors

    public bool InPossession { get { return _inPossession; } set { _inPossession = value; } }
    public float AimSpeed { get { return _aimSpeed; } }
    public float ReticleInaccuracy { get { return _reticleInaccuracy; } }
    public float AimReticleInaccuracy { get { return _aimReticleInaccuracy; } }
    public float ReticleRecoverySpeed { get { return _reticleRecoverySpeed; } }
    public float AimReticleRecoverySpeed { get { return _aimReticleRecoverySpeed; } }
    public float Instability { get { return _instability; } }
    public float MaxInstability { get { return _maxInstability; } }
    public float DetabilizationSpeed { get { return _destabilizationSpeed; } }
    public float AimDetabilizationSpeed { get { return _aimDestabilizationSpeed; } }
    public float AimInstability { get { return _aimInstability; } }
    public float Recoil { get { return _recoil; } }
    public EGunType GunType { get { return _gunType; } }
    public Bullet Bullet { get { return _bullet; } }
    public WeaponReloader Reloader { get { return _reloader; } }
    public GameObject BulletHole { get { return _bulletHole; } }
    public bool HasScope { get { return _hasScope; } }
    public float Zoom { get { return _zoom; } }
    public AudioClip ScopeSound { get { return _scopeSound; } }
    public int NumberOfBulletsFiredAtOnce { get { return _numberOfBulletsFiredAtOnce; } }
    public float Spread { get { return _spread; } }
    public float Damage { get { return _damage; } }
    public Sprite Sprite { get { return _sprite; } }
    public float RateOfFire { get { return _rateOfFire; } }

    #endregion

    #region Cache Fields

    private Transform _muzzle = null;
    private Player _player = null;
    private Camera _playerCamera = null;
    protected WeaponReloader _reloader = null;
    private ParticleSystem _fireParticleSystem = null;
    private List<Rigidbody> _bodyParts = new List<Rigidbody>();
    public List<Rigidbody> BodyParts
    {
        get
        {
            if (_bodyParts.Count == 0)
            {
                Ragdoll ragdoll = GetComponentInParent<Ragdoll>();
                if (ragdoll != null) _bodyParts = ragdoll.BodyParts;

            }

            return _bodyParts;
        }
    }

    #endregion

    #region Private Fields

    private float _nextFireAllowed = 0f;
    protected bool _canFire = false;

    #endregion


    #region Monobehavior Callbacks

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _muzzle = transform.Find("Muzzle");
        if (_muzzle != null) _fireParticleSystem = _muzzle.GetComponent<ParticleSystem>();

        _reloader = GetComponent<WeaponReloader>();

        if (_hand != null)
        {
            transform.SetParent(_hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        if (!_player.PlayerStates.IsAiming && _mesh != null && _hasScope && !_mesh.gameObject.activeSelf)
        {
            HandleMeshActivation(true);
        }
    }

    #endregion

    public void SetPlayerCamera(Camera camera)
    {
        _playerCamera = camera;
    }

    #region Handling Fire

    public CheckFireAction CheckFire()
    {
        if (Time.time < _nextFireAllowed)
            return CheckFireAction.DontFire;

        _nextFireAllowed = Time.time + _rateOfFire;

        if (_reloader != null)
        {
            if (_reloader.IsReloading) return CheckFireAction.DontFire;

            if (_reloader.RoundsRemainingInClip == 0)
            {
                bool canReload = _reloader.Reload();
                if (_drySound != null && !canReload)
                {
                    return CheckFireAction.PlayDrySound;
                }

                return CheckFireAction.DontFire;
            };

            _reloader.TakeFromClip(1);
        }

        return CheckFireAction.DoFire;
    }

    public void FireRPC(CheckFireAction checkFireAction)
    {
        if (checkFireAction == CheckFireAction.DontFire) return;

        if (checkFireAction == CheckFireAction.PlayDrySound)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_drySound, 1, 1, 1, transform.position);
            return;
        }

        // Here, we are sure to fire, so we start by moving the reticle. But only if it's the local player
        if (_player.isLocalPlayer)
            GameManager.Instance.LocalPlayer.MyCrosshair.Destabilize();

        FireEffect();

        if (_fireSound != null)
            GameManager.Instance.AudioManager.PlayOneShotSound(_fireSound, 1, 1, 1, transform.position);
    }

    public virtual void Fire(CheckFireAction checkFireAction, Vector3 origin, Vector3 direction)
    {
        RaycastHit[] hitInfos = Physics.RaycastAll(origin, direction, _range, LayerMask.GetMask("Default", "BodyPart", "Wood", "Metal", "Grass", "Mine"));

        Debug.DrawRay(origin, direction, Color.red, 2);
        int closestHitIndex = -1;
        if (hitInfos.Length > 0)
        {
            for (int i = 0; i < hitInfos.Length; i++)
            {
                RaycastHit hitInfo = hitInfos[i];

                // We make sure we don't hit something behind us
                if (Vector3.Angle(hitInfo.transform.position - _player.transform.position, _player.transform.forward) > 90)
                {
                    continue;
                }

                bool hitInfoIsOurselves = false;
                // We make sure we don't hit ourselves
                foreach (Rigidbody bodyPart in BodyParts)
                {
                    if (hitInfo.transform.gameObject == bodyPart.gameObject)
                    {
                        hitInfoIsOurselves = true;
                        continue;
                    }
                }
                if (hitInfoIsOurselves) continue;

                if (closestHitIndex == -1) closestHitIndex = i;
                else
                {
                    float thisDistance = (hitInfo.point - transform.position).magnitude;
                    float closestDistance = (hitInfos[closestHitIndex].point - transform.position).magnitude;
                    if (thisDistance < closestDistance)
                    {
                        closestHitIndex = i;
                    }
                }
            }

            if (closestHitIndex != -1)
            {
                RaycastHit hitInfo = hitInfos[closestHitIndex];

                Rigidbody rigidbody = hitInfo.transform.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.AddForce(direction * _pushForce);
                }

                if (_bullet != null) {
                    Bullet bullet = Instantiate(_bullet, _firePosition.position, Quaternion.identity);
                    //bullet.GetComponent<NetworkIdentity>().AssignClientAuthority(GameManager.Instance.LocalPlayer.connectionToClient);
                    bullet.transform.forward = hitInfo.point - _firePosition.position;
                    bullet.ThrowerIdentifier = _player.netId;
                    NetworkServer.Spawn(bullet.gameObject, GameManager.Instance.LocalPlayer.connectionToClient);
                } else {
                    // We check if it's a mine
                    SpawnedTool spawnedTool = hitInfo.transform.GetComponent<SpawnedTool>();
                    bool collidedWithMine = false;
                    if (spawnedTool != null)
                    {
                        spawnedTool.Explode();
                        collidedWithMine = true;
                    }

                    // We check if it's a destructable
                    Health selfHealth = hitInfo.transform.GetComponent<Health>();
                    if (selfHealth != null)
                    {
                        selfHealth.TakeDamage(_damage, hitInfo.point, _player.netId);
                    }

                    // This is the health of the parent object of player or that of a state machine
                    AIStateMachine stateMachine = hitInfo.transform.GetComponentInParent<AIStateMachine>();
                    if (stateMachine != null)
                    {
                        stateMachine.Health.TakeDamage(_damage, hitInfo.point, _player.netId);
                    }

                    Player player = hitInfo.transform.GetComponentInParent<Player>();
                    if (player != null)
                    {
                        player.Health.TakeDamage(_damage, hitInfo.point, _player.netId);
                    }


                    // We instantiate bullethole (only if we didn't hit a body part)
                    if (_bulletHole != null && hitInfo.transform.gameObject.layer != LayerMask.NameToLayer("BodyPart")
                        && !collidedWithMine)
                    {
                        _player.PlayerShoot.RpcSpawnBulletHole(hitInfo.point + hitInfo.normal / 1000, -hitInfo.normal);
                    }
                }
            }
        }
    }

    private void FireEffect()
    {
        if (_fireParticleSystem == null) return;

        _fireParticleSystem.Play();
    }

    #endregion

    public void HandleEquip(bool equip)
    {
        bool activeDecoration = !equip;
        if (!_inPossession)
        {
            activeDecoration = false;
        }

        if (_decoration != null) _decoration.gameObject.SetActive(activeDecoration);

        gameObject.SetActive(equip);
    }

    /// <summary>
    /// We deacivate the weapon/shoter mesh when we use the scope. This is so
    /// to avoid having the mesh showing while we are zooming/scoping.
    /// </summary>
    /// <param name="activate"></param>
    public void HandleMeshActivation(bool activate)
    {
        if (_mesh != null) _mesh.SetActive(activate);
    }
}
