using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : DestroyableBehaviour
{
    [Header("Missile")]
    public float maxSpeed;
    public float acceleration;
    public int designationChannel = 32;
    public float damage;

    [HideInInspector] public ITarget target;
    [HideInInspector] public bool go = false;

    float speed = 0;
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target.GetPosition());
        transform.position = transform.position + transform.forward * speed * Time.deltaTime;
        if (go)
            speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);

        if ((target.GetPosition() - transform.position).magnitude < maxSpeed * 2 * Time.deltaTime)
        {
            var tdb = target as DestroyableBehaviour;
            tdb?.TakeDamage(damage);
            Kill();
        }
    }
}
