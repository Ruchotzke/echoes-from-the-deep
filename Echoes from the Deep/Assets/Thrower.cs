using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Thrower : MonoBehaviour
{
    public Rigidbody pf_Box;
    public float Force = 100.0f;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            /* Throw a box */
            var dir = Camera.main.ScreenPointToRay(Input.mousePosition);
            var rb = Instantiate(pf_Box);
            rb.transform.position = dir.origin;
            rb.AddForce(dir.direction * Force);
            Destroy(rb.gameObject, 5.0f);
        }
    }
}
