using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragMoveProvider : RotationProvider
{
    public float maxSensitivity = 1;
    public float maxLength = 400;
    public AnimationCurve sensitivityWrtLength;

    Vector2 diffAngles = Vector2.zero;
    Vector3 prevPos = Vector3.zero;
    Vector3 initialPos = Vector3.zero;


    protected new void Start()
    {
        base.Start();
    }
    void Update()
    {
        if (!readyToUpdate) return;
        if (Input.GetMouseButtonDown(0))
        {
            initialPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            var sensitivity = sensitivityWrtLength.Evaluate((Input.mousePosition - initialPos).magnitude/maxLength) * maxSensitivity;
            var x = (-(prevPos.y - Input.mousePosition.y) * sensitivity);
            var y = (prevPos.x - Input.mousePosition.x) * sensitivity;
            x = Mathf.Clamp(x, -1, 1);
            y = Mathf.Clamp(y, -1, 1);
            diffAngles.Set(x, y);
            prevPos = Input.mousePosition;
        }
        else
        {
            diffAngles.Set(0, 0);
        }
    }

    public override Vector2 GetAngles()
    {
        Update();
        return diffAngles;
    }
}
