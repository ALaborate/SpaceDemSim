using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonsProvider : RotationProvider
{
    public float sensitivity = 1f;
    public float gravity = 1f;
    private float x, y;
    protected new void Start()
    {
        base.Start();
    }

    void Update()
    {
        if (!readyToUpdate) return;
        UpdateAxes();

        var coef = gravity * Time.deltaTime;
        if (Mathf.Abs(y) < coef)
            coef = Mathf.Abs(y);
        y += -Mathf.Sign(y) * coef;
        coef = gravity * Time.deltaTime;
        if (Mathf.Abs(x) < coef)
            coef = Mathf.Abs(x);
        x += -Mathf.Sign(x) * coef;
        if (DownHold)
        {
            x += sensitivity * Time.deltaTime;
        }
        if (UpHold)
        {
            x -= sensitivity * Time.deltaTime;
        }
        if (LeftHold)
        {
            y -= sensitivity * Time.deltaTime;
        }
        if (RightHold)
        {
            y += sensitivity * Time.deltaTime;
        }

        x = Mathf.Clamp(x, -1, 1);
        y = Mathf.Clamp(y, -1, 1);
    }

    private bool LeftHold, RightHold, UpHold, DownHold;
    // those public methods are called from event triggers on buttons under ControlButtons gameobject
    public void OnLeftButtonDown() { LeftHold = true; }
    public void OnLeftButtonUp() { LeftHold = false; }
    public void OnRightButtonDown() { RightHold = true; }
    public void OnRightButtonUp() { RightHold = false; }
    public void OnUpButtonDown() { UpHold = true; }
    public void OnUpButtonUp() { UpHold = false; }
    public void OnDownButtonDown() { DownHold = true; }
    public void OnDownButtonUp() { DownHold = false; }

    public override Vector2 GetAngles()
    {
        Update();
        return new Vector2(x, y);
    }

    private void UpdateAxes()
    {
        if (Input.GetKeyDown(KeyCode.W)) { UpHold = true; }
        if (Input.GetKeyUp(KeyCode.W)) { UpHold = false; }
        if (Input.GetKeyDown(KeyCode.S)) { DownHold = true; }
        if (Input.GetKeyUp(KeyCode.S)) { DownHold = false; }
        if (Input.GetKeyDown(KeyCode.A)) { LeftHold = true; }
        if (Input.GetKeyUp(KeyCode.A)) { LeftHold = false; }
        if (Input.GetKeyDown(KeyCode.D)) { RightHold = true; }
        if (Input.GetKeyUp(KeyCode.D)) { RightHold = false; }
    }
}
