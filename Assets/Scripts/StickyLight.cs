using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyLight : MonoBehaviour
{

    Rigidbody rb;
    Collider col;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            col.enabled = false;
        }
    }
}
