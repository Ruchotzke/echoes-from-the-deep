using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera")] 
    public Camera Camera;
    public float Sensitivity = 400.0f;
    
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

    [Header("Jumping")] 
    public float JumpForce = 10.0f;
    public float JumpGracePeriod = 1.0f;
    public float CoyoteTime = 0.5f;
    public int MaxJumps = 1;

    private Rigidbody rb;
    
    private float verticalAngle = 0.0f;
    private Vector3 forwardDirectionEuler;

    private bool inJumpGracePeriod = false;

    private bool isAirborne = false;
    private float timeSinceAirborne = 0.0f;
    private int jumpsRemaining;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        /* Lock the mouse */
        Cursor.lockState = CursorLockMode.Locked;
        
        /* Update the initial forward direction */
        forwardDirectionEuler = transform.forward;
        forwardDirectionEuler.y = 0;
        forwardDirectionEuler.Normalize();
        
        /* Handle airtime */
        StartCoroutine(AirtimeTimer());
        jumpsRemaining = MaxJumps;
    }

    private void Update()
    {
        /* Handle mouse movement */
        Vector3 mouse = new Vector3(Input.GetAxisRaw("Mouse X"), 0f, Input.GetAxisRaw("Mouse Y"));
        
        /* Vertical angle */
        verticalAngle = Mathf.Clamp(verticalAngle - Sensitivity * mouse.z * Time.deltaTime, -89f, 89f);
        Camera.transform.localRotation = Quaternion.Euler(verticalAngle, 0.0f, 0.0f);
        
        /* Forward direction target */
        forwardDirectionEuler.y += Sensitivity * mouse.x * Time.deltaTime;
        
        /* Jumping */
        if (Input.GetKeyDown(KeyCode.Space) && (jumpsRemaining > 0 || timeSinceAirborne <= CoyoteTime))
        {
            if (rb.velocity.y < 0) rb.velocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
            rb.AddForce(Vector3.up * JumpForce);
            jumpsRemaining -= 1;
            Debug.Log(jumpsRemaining);
            if (!inJumpGracePeriod) StartCoroutine(OnJumpCoroutine());
        }
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
            /* Project the movement direction relative to the player */
            Quaternion cameraDirection = Quaternion.LookRotation(Quaternion.Euler(forwardDirectionEuler) * Vector3.forward, Vector3.up);
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
            
        }
        else
        {
            /* Apply drag to velocity */
            Vector3 lateralVelocity = rb.velocity;
            lateralVelocity.y = 0;
            rb.AddForce(-lateralVelocity*LateralDrag);
        }
        
        /* Hovering */
        /* Raycast down to get height */
        if (!inJumpGracePeriod && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, MaxAttachHeight, ~LayerMask.NameToLayer("Player")))
        {
            isAirborne = false;
            jumpsRemaining = MaxJumps;
            
            /* We hit the ground, compute force */
            float heightdiff = TargetHeight - hit.distance;
            float force = SpringForce * heightdiff - DampingForce*rb.velocity.y;
            
            /* Apply force */
            rb.AddForce(Vector3.up * force);
            
            /* Apply an equal and opposite force to the object below you */
            var forceNeeded = Mathf.Abs(rb.velocity.y) / Time.fixedDeltaTime * rb.mass;
            hit.rigidbody?.AddForce(10.0f * forceNeeded * Vector3.down);
        }
        else
        {
            /* We are airborne */
            isAirborne = true;
            rb.AddForce(Physics.gravity);
        }
        
        /* Remain Upright */
        /* Determine the difference */
        var targetRot = Quaternion.Euler(forwardDirectionEuler);
        var delta = targetRot * Quaternion.Inverse(transform.rotation);
        if (delta.w < 0.0f)
        {
            delta.Set(-delta.x, -delta.y, -delta.z, -delta.w);
        }
        
        /* Apply a corrective force */
        delta.ToAngleAxis(out var angle, out var axis);
        axis.Normalize();
        angle *= Mathf.Deg2Rad;
        rb.AddTorque((axis * (angle * SpringTorque)) - (rb.angularVelocity * DampingTorque));
        
    }

    /// <summary>
    /// The successful jump coroutine.
    /// </summary>
    /// <returns></returns>
    private IEnumerator OnJumpCoroutine()
    {
        inJumpGracePeriod = true;
        yield return new WaitForSeconds(JumpGracePeriod);
        inJumpGracePeriod = false;
    }

    private IEnumerator AirtimeTimer()
    {
        while (true)
        {
            if (isAirborne)
            {
                timeSinceAirborne += Time.deltaTime;
            }
            else
            {
                timeSinceAirborne = 0.0f;
            }

            yield return new WaitForFixedUpdate();
        }
    }
}