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

    [Header("Locomotion")] 
    public float MaxSpeed = 8.0f;
    public float Acceleration = 200.0f;
    public float MaxForce = 150.0f;
    public float LateralDrag = 20.0f;

    private Rigidbody rb;

    private Vector3 moveDir = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        /* Movement */
        /* Get input and determine a move direction */
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        Vector3 planeTarget = new Vector3(horizontal, 0f, vertical);
        if (planeTarget.sqrMagnitude > 0)
        {
            /* Project the movement direction relative to the camera */
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            Quaternion cameraDirection = Quaternion.LookRotation(cameraForward, Vector3.up);
            Vector3 moveTarget = cameraDirection * planeTarget;
            moveTarget.Normalize();
            
            /* Identify ideal movement direction and speed */
            moveTarget = MaxSpeed * moveTarget;
            
            /* Identify the actual applicable goal velocity */
            Vector3 lateralVelocity = rb.velocity;
            lateralVelocity.y = 0;
            Vector3 goalVelocity = Vector3.MoveTowards(lateralVelocity, moveTarget, Acceleration * Time.fixedDeltaTime);
            Vector3 neededAccel = (goalVelocity - lateralVelocity) / Time.fixedDeltaTime;
            if (neededAccel.magnitude * rb.mass > MaxForce) // compute and clamp the max force applied
            {
                var slowdown = MaxForce / (neededAccel.magnitude * rb.mass);
                neededAccel *= slowdown;
            }
        
            /* Apply the lateral movement force */
            rb.AddForce(neededAccel * rb.mass);
            
            /* Update movement direction for rotation */
            var q = Quaternion.LookRotation(moveTarget, Vector3.up);
            moveDir = q.eulerAngles;
        }
        else
        {
            /* Apply drag to velocity */
            Vector3 lateralVelocity = rb.velocity;
            lateralVelocity.y = 0;
            rb.AddForce(-lateralVelocity*LateralDrag);
            
            /* Update movedir based on actual movement direction */
            if (lateralVelocity.sqrMagnitude > 0.01f)
            {
                lateralVelocity.Normalize();
        
                var q = Quaternion.LookRotation(lateralVelocity, Vector3.up);
                moveDir = q.eulerAngles;
            }
        }
        
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
        var targetRot = Quaternion.Euler(moveDir);
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