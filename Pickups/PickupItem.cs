using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PickupItem : NetworkBehaviour
{
    [SerializeField] private float _price = 8.99f;
    [SerializeField] private float _length = .5f;

    private float _minPos = 0f;

    public float Price { get { return _price; } }

    #region Monobehavior Callbacks

    private void Awake()
    {
        _minPos = transform.position.y - _length / 2;
    }

    protected virtual void Update()
    {
        transform.position = new Vector3(transform.position.x, _minPos + Mathf.PingPong(Time.time, _length), transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.gameObject != GameManager.Instance.LocalPlayer.gameObject)
        {
            //Destroy(gameObject);
            NetworkServer.Destroy(gameObject);
            return;
        }

        Pickup(other.transform);

        //Destroy(gameObject);
        NetworkServer.Destroy(gameObject);
    }

    #endregion

    protected virtual void Pickup(Transform player)
    {
    }
}
