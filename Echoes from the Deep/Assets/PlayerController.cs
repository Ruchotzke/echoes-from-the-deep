using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hovering")] 
    public float MaxAttachHeight = 4.0f;
    public float TargetHeight = 1.0f;
    public float SpringForce;
    public float DampingForce;

    [Header("Upright Force")] 
    public float SpringTorque;
    public float DampingTorque;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        /* Hovering */
        /* Raycast down to get height */
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, MaxAttachHeight, ~LayerMask.NameToLayer("Player")))
        {
            /* We hit the ground, compute force */
            float heightdiff = TargetHeight - hit.distance;
            float force = SpringForce * heightdiff - DampingForce*rb.velocity.y;
            
            /* Apply force */
            rb.AddForce(Vector3.up * force);
        }
        else
        {
            /* We are off the ground */
            rb.AddForce(Physics.gravity);
        }
        
        /* Remain Upright */
        /* Determine the difference */
        var targetRot = Quaternion.Euler(Vector3.zero);
        var delta = targetRot * Quaternion.Inverse(transform.rotation);
        if (delta.w < 0.0f)
        {
            delta.Set(-delta.x, -delta.y, -delta.z, -delta.w);
        }
        
        /* Apply a corrective force */
        delta.ToAngleAxis(out var angle, out var axis);
        axis.Normalize();
        angle = angle * Mathf.Deg2Rad;
        rb.AddTorque((axis * (angle * SpringTorque)) - (rb.angularVelocity * DampingTorque));

    }
}