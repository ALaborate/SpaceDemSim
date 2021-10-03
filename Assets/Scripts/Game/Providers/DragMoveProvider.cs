using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragMoveProvider : RotationProvider
{
    public float sensitivity = 1;

    Vector2 diffAngles = Vector2.zero;
    Vector3 prevPos = Vector3.zero;
    void Update()
    {
        if (!readyToUpdate) return;
        if (Input.GetMouseButtonDown(0))
        {
            prevPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            var x = -(prevPos.y - Input.mousePosition.y) * sensitivity;
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
