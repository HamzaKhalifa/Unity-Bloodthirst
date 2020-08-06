using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    #region Private Fields

    public float Horizontal = 0f;
    public float Vertical = 0f;
    public Vector2 MouseInput = Vector2.zero;
    public bool Mouse1 = false;
    public bool Mouse2 = false;
    public bool R = false;
    public bool IsWalking = false;
    public bool IsSprinting = false;
    public bool IsCrouching = false;
    public bool A = false;
    public bool Z = false;
    public bool F = false;
    public bool Space = false;
    public bool T = false;
    public bool Tab = false;
    public bool P = false;
    public bool O = false;
    public bool I = false;
    public bool Escape = false;

    #endregion

    private void Update()
    {
        Horizontal = Input.GetAxis("Horizontal");
        Vertical = Input.GetAxis("Vertical");
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Mouse1 = Input.GetButton("Fire1");
        Mouse2 = Input.GetButton("Fire2");
        R = Input.GetKeyDown(KeyCode.R);
        IsWalking = Input.GetKey(KeyCode.LeftAlt);
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        IsCrouching = Input.GetKey(KeyCode.C);
        A = Input.GetKeyDown(KeyCode.A);
        Z = Input.GetKeyDown(KeyCode.Z);
        F = Input.GetKeyDown(KeyCode.F);
        Space = Input.GetKeyDown(KeyCode.Space);
        T = Input.GetKeyDown(KeyCode.T);
        Tab = Input.GetKeyDown(KeyCode.Tab);

        // For Projectiles
        P = Input.GetKeyDown(KeyCode.P);
        O = Input.GetKeyDown(KeyCode.O);
        I = Input.GetKeyDown(KeyCode.I);

        Escape = Input.GetKeyDown(KeyCode.Escape);
    }
}
