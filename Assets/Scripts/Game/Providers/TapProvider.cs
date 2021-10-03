using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TapProvider : RotationProvider
{

    // Start is called before the first frame update
    protected new void Start()
    {
        base.Start();
        currentRotationSystem = new GameObject().transform;
        currentRotationSystem.gameObject.hideFlags = HideFlags.HideAndDontSave;
    }


    Ray target;
    Transform currentRotationSystem;
    Vector3 diffRotation = Vector2.zero;
    void Update()
    {
        if (!readyToUpdate) return;

        currentRotationSystem.rotation = feedback.currentRotation;

        if (Input.GetMouseButtonDown(0))
        {
            target = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        var targetDirection = currentRotationSystem.InverseTransformDirection(target.direction);
        var vertical = -Mathf.Atan2(targetDirection.y, targetDirection.z) * Mathf.Rad2Deg;
        var horizontal = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

        var max = feedback.maxRotSpeed.x * Time.deltaTime;
        vertical = Mathf.Clamp(vertical, -max, max);
        vertical = vertical / max;

        max = feedback.maxRotSpeed.y * Time.deltaTime;
        horizontal = Mathf.Clamp(horizontal, -max, max);
        horizontal = horizontal / max;

        diffRotation.Set(vertical, horizontal, 0);
    }
    public override Vector2 GetAngles()
    {
        Update();
        return diffRotation;
    }
    public void ResetTarget()
    {
        target = new Ray(gameObject.transform.position, currentRotationSystem.forward);
    }
    protected void OnGUI()
    {
        // GUI.Label(new Rect(100, 450, 200, 40), "Diff rotation: " + diffRotation);
    }
}
