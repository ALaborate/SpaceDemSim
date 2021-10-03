using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelerometerProvider : RotationProvider
{
    public float inputFilter = 0.95f;

    private Transform referenceFrame;
    // Start is called before the first frame update
    protected new void Start()
    {
        base.Start();
        referenceFrame = new GameObject().transform;
        referenceFrame.gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    private Quaternion prevRotation = Quaternion.identity;
    private Vector3 diffAngles = Vector3.down;
    // Update is called once per frame

    void Update()
    {
        if (!readyToUpdate) return;

        var acc = Input.acceleration;
        acc.z = -acc.z;
        var rot = Quaternion.LookRotation(acc, Vector3.forward);
        prevRotation = Quaternion.Lerp(rot, prevRotation, inputFilter);

        if (calibrate)
        {
            referenceFrame.rotation = rot;
        }

        acc = referenceFrame.InverseTransformDirection(acc.normalized);
        var delta = new Vector3(Mathf.Clamp(acc.y, -1, 1), Mathf.Clamp(acc.x, -1, 1));
        diffAngles = delta;
    }

    private bool calibrate = false;
    // those public methods are called from event triggers on button under AccelerometerCalibrate gameobject
    public void OnCalibrateDown() { calibrate = true; }
    public void OnCalibrateUp() { calibrate = false; }


    void OnGUI()
    {
        // GUI.Label(new Rect(100, 400, 200, 40), "Ref dif : " + diffDirection);
        // GUI.Label(new Rect(100, 450, 200, 40), "Acceleration : " + Input.acceleration);
    }

    public override Vector2 GetAngles()
    {
        Update();
        return diffAngles;
    }
}
