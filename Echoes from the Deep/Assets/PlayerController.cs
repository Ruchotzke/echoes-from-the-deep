using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hovering")] 
    public float TargetHeight = 1.0f;
    public float SpringForce;
    public float DampingForce;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        /* Hovering */
        /* Raycast down to get height */
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 4.0f))
        {
            /* We hit the ground, compute force */
            float heightdiff = TargetHeight - hit.distance;
            float force = SpringForce * heightdiff - DampingForce*rb.velocity.y;
            
            /* Apply force */
            rb.AddForce(Vector3.up * force);
        }
        
        

    }
}