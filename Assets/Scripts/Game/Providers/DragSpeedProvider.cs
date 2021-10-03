using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragSpeedProvider : RotationProvider
{
    [Tooltip("How many pixel should touch be off landing posiion to produce max output")]
    public float maxRadius = 100f;


    Vector2 diffAngles = Vector2.zero;
    Vector3 initialScreenPos = Vector3.zero;
    void Update()
    {
        if (!readyToUpdate) return;
        if (Input.GetMouseButtonDown(0))
        {
            initialScreenPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            var x = (initialScreenPos.y - Input.mousePosition.y) / maxRadius;
            var y = -(initialScreenPos.x - Input.mousePosition.x) / maxRadius;
            x = Mathf.Clamp(x, -1, 1);
            y = Mathf.Clamp(y, -1, 1);
            diffAngles.Set(x, y);
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

public abstract class TouchBasedRotationProvider : RotationProvider
{
    protected Dictionary<int, TouchData> data = new Dictionary<int, TouchData>();
    protected new void Start()
    {
        data.Clear();
        base.Start();
    }
    protected void UpdateTouches()
    {
        foreach (var item in Input.touches)
        {
            switch (item.phase)
            {
                case TouchPhase.Began:
                    data[item.fingerId] = new TouchData() { initialScreenPos = item.position, currentScreenPos = item.position };
                    break;
                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    data[item.fingerId].currentScreenPos = item.position;
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                default:
                    data.Remove(item.fingerId);
                    break;
            }
        }
    }
    protected class TouchData
    {
        private Vector3 _initialScreenPos;
        public Vector3 initialScreenPos
        {
            get => _initialScreenPos;
            set
            {
                _initialScreenPos = value;
            }
        }
        public Vector3 currentScreenPos;
    }
}
