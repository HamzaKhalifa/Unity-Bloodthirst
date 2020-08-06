using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerAim : NetworkBehaviour
{
    [SerializeField] private float _lookHeight = 5f;

    private void Update()
    {
        transform.LookAt(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, _lookHeight)));
    }

    public float GetAngle()
    {
        float value = transform.rotation.eulerAngles.x;

        if (value >= 0 && value <= 90) { return value; }
        else return value - 360;
    }
}
