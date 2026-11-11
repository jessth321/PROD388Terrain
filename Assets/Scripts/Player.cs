using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float speed;
    public float jumpForce;
    public float throwForce;
    public float pickaxeRange;

    [Header("")]
    [Range(0f,500f)]
    public float deformationRadius;
    [Range(0f, 1f)]
    public float deformationBlend;
    [Range(0f, 1f)]
    public float deformationFrequency;
    [Range(0f, 1f)]
    public float deformationAmp;
    [Range(0, 12)]
    public int deformationOct;
    [Range(0f, 20f)]
    public float deformationLac;
    [Range(0f, 1f)]
    public float deformationPers;

    Vector2 mouseDelta;
    Vector3 direction;
    public bool isFlying;
    bool isSprinting;

    Rigidbody rb;
    CapsuleCollider cc;

    Animator animController;

    public Transform cam;
    public GameObject rightHand;
    public GameObject chippedRockParticles;
    public GameObject stickyBomb;
    public GameObject stickyLight;


    public MarchingCubes mc;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        animController = GetComponent<Animator>();

        transform.localScale = transform.localScale / (mc.noiseScale);

        GetComponent<ConstantForce>().force /= mc.noiseScale;


        //cam = GetComponentInChildren<Camera>();

        //cam.depthTextureMode = DepthTextureMode.Depth;

        //depthTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 24);
        //cam.targetTexture = depthTex;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, mouseDelta.x * 0.25f, 0), Space.World);
        cam.Rotate(new Vector3(mouseDelta.y * -0.15f, 0, 0), Space.Self);

        if (isFlying)
        {
            cc.enabled = false;
            rb.isKinematic = true;

            rightHand.SetActive(false);

            transform.localPosition += new Vector3(transform.forward.x, 0, transform.forward.z).normalized * direction.z * Time.deltaTime * speed * 2;
            transform.localPosition += transform.right * direction.x * Time.deltaTime * speed;
            transform.localPosition += Vector3.up * direction.y * Time.deltaTime * speed;

        }
        else
        {
            cc.enabled = true;
            rb.isKinematic = false;

            rightHand.SetActive(true);

            rb.AddForce(transform.forward * direction.z * Time.deltaTime * (speed / mc.noiseScale) * 1000);
            rb.AddForce(transform.right * direction.x * Time.deltaTime * (speed / mc.noiseScale) * 1000);

            if (!isSprinting && new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude > speed / mc.noiseScale)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized * (speed / mc.noiseScale) + Vector3.up * rb.velocity.y;
            }
            else if (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude > (speed / mc.noiseScale) * 3)
            {

                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized * (speed / mc.noiseScale) * 3 + Vector3.up * rb.velocity.y;
            }

            if (direction.magnitude == 0)
            {
                cc.material.dynamicFriction = 4;
                cc.material.staticFriction = 4;
            }
            else
            {
                cc.material.dynamicFriction = 0.1f;
                cc.material.staticFriction = 0;
            }



        }

    }

    void OnMouseDelta(InputValue val)
    {
        mouseDelta = val.Get<Vector2>();
    }
    void OnMovement(InputValue val)
    {
        direction = val.Get<Vector3>();

        if (!isFlying)
        {
            if (direction.y < 0)
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }
            direction = new Vector3(direction.x, 0, direction.z);
        }
    }

    void OnDeform(InputValue val)
    {
        if (isFlying && Physics.Raycast(cam.position, cam.forward, out RaycastHit rayHit1))
        {
            Vector3 position;
            position = rayHit1.point;
            mc.DeformChunks(new MarchingCubes.Deformation(position, deformationRadius, deformationBlend, deformationFrequency, deformationAmp, (float)deformationOct, deformationLac, deformationPers));
        }
        else if (!isFlying)
        {
            animController.SetTrigger("swingPickaxe");
        }
    }

    public void PickaxeDeform()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit rayHit2, pickaxeRange))
        {
            Vector3 position;
            position = rayHit2.point;
            mc.DeformChunks(new MarchingCubes.Deformation(position, deformationRadius, deformationBlend, deformationFrequency, deformationAmp, (float)deformationOct, deformationLac, deformationPers));

            GameObject obj = Instantiate(chippedRockParticles, position, Quaternion.Euler(-90, 0, 0));

            Destroy(obj, 1f);
        }
    }

    public void OnThrowStickyBomb()
    {
        GameObject bomb = Instantiate(stickyBomb, cam.transform.position + (Vector3.down / mc.noiseScale) + (Vector3.ProjectOnPlane(cam.forward, Vector3.up) / mc.noiseScale), Quaternion.identity);
        bomb.GetComponent<Rigidbody>().AddForce(cam.forward * throwForce, ForceMode.Impulse);
    }
    public void OnThrowStickyLight()
    {
        GameObject light = Instantiate(stickyLight, cam.transform.position + Vector3.down + Vector3.ProjectOnPlane(cam.forward, Vector3.up), Quaternion.identity);
        light.GetComponent<Rigidbody>().AddForce(cam.forward * throwForce, ForceMode.Impulse);
    }

    void OnToggleFlying()
    {
        isFlying = !isFlying;
    }

    void OnJump()
    {
        if (!isFlying)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce / (mc.noiseScale), ForceMode.Impulse);
        }
    }
}
