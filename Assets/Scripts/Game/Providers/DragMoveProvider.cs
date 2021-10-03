using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragMoveProvider : RotationProvider
{
    public float maxSensitivity = 1;
    public float minSensitivity = 0.05f;
    public AnimationCurve sensitivityIncrSpeed;
    public AnimationCurve sensitivityFadeSpeed;

    Vector2 diffAngles = Vector2.zero;
    Vector3 prevPos = Vector3.zero;
    private float sensitivity;

    protected new void Start()
    {
        base.Start();
        sensitivity = minSensitivity;
    }
    void Update()
    {
        if (!readyToUpdate) return;
        if (Input.GetMouseButtonDown(0))
        {
            prevPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            var x = (-(prevPos.y - Input.mousePosition.y) * sensitivity);
            var y = (prevPos.x - Input.mousePosition.x) * sensitivity;
            x = Mathf.Clamp(x, -1, 1);
            y = Mathf.Clamp(y, -1, 1);
            diffAngles.Set(x, y);
            sensitivity += Mathf.Clamp01(diffAngles.sqrMagnitude) * sensitivityIncrSpeed.Evaluate(sensitivity) * Time.deltaTime;
            prevPos = Input.mousePosition;
        }
        else
        {
            diffAngles.Set(0, 0);
        }
        sensitivity = Mathf.Clamp(sensitivity - sensitivityFadeSpeed.Evaluate(sensitivity) * Time.deltaTime * (1 - Mathf.Clamp01(diffAngles.sqrMagnitude)), minSensitivity, maxSensitivity);
    }

    public override Vector2 GetAngles()
    {
        Update();
        return diffAngles;
    }
}
