using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    /* Import external objects */
    PlayerStats playerStats;
    public Collider playerCollider;
    public Camera playerCamera;
    public Rigidbody rigidBody;

    /* Player Actions */
    PlayerInput playerInput;
    InputAction moveAction;
    InputAction sprintAction;
    InputAction jumpAction;
    InputAction lookAction;
    
    /* Movement Values */
    public float moveSpeed = 1.5f;

    /* Jump Values */
    //public bool isGrounded;
    public float maxJumpHeight = 2.0f;
    public float maxJumpTime = 1.0f;
    public float initialJumpVelocity = 1.0f;

    /* Look Values */
    public float verticalSensitivity = 0.75f;
    public float horizontalSnesitivity = 0.75f;
    float cameraPitch = 0f;

    /* Gravity Values */
    const float gravityConstant = 9.8f;


    /*      TO-DO List
     *  Implement jumping
     *  Get camera mouse controls working
     *  See if gravity is needed and implement if necessary
     *  Rework "CollideAndSlide" so that it is actually useful
     *  
    */

    // Called at runtime before Update
    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
        sprintAction = playerInput.actions.FindAction("Sprint");
        jumpAction = playerInput.actions.FindAction("Jump");
        lookAction = playerInput.actions.FindAction("Look");

        Cursor.lockState = CursorLockMode.Locked; // Locks the mouse cursor
        // Camera.main.aspect = (9 / 16); // Sets the camera's aspect ratio


    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Jump();
        Look();

        UnityEngine.Debug.DrawRay(transform.position, -Vector3.up, Color.green);
    }

    /*
    void Gravity()
    {
        if (!isGrounded)
        {

        }
            
    }*/

    void Move()
    {
        Vector2 direction = moveAction.ReadValue<Vector2>();
        float isSprinting = sprintAction.ReadValue<float>();
        Vector3 gravity = new Vector3(0, 0, 0);

        Vector3 movement = transform.right * direction.x + transform.forward * direction.y;

        movement += CollideAndSlide(movement, transform.position, 0, false, movement);
        movement += CollideAndSlide(gravity, transform.position + movement, 0, true, gravity);

        if ((isSprinting == 1) && (!playerStats.outOfStamina)) { moveSpeed = 3f; } // Conditonal for if the player is sprinting and has stamina
        else { moveSpeed = 1.5f; } // Sets to default move speed otherwise

        transform.position += movement * moveSpeed * Time.deltaTime;
    }

    void Jump()
    {            
        if (isGrounded() && jumpAction.triggered)
        {
            UnityEngine.Debug.Log("Jumping");
            transform.position += new Vector3(0, 1.0f, 0);
        }
    }

    // Allows the player to look around
    void Look()
    {
        Vector2 lookDirection = lookAction.ReadValue<Vector2>(); // Gets the mouse movement as a 2D vector

        // Calculates the movement based on mouse sensitivity
        float lookY = -lookDirection.y * verticalSensitivity;
        float lookX = lookDirection.x * horizontalSnesitivity;

        // Adds the vertical movement then clamps it to the bounds
        cameraPitch += lookY;
        cameraPitch = Mathf.Clamp(cameraPitch, -75f, 75f);

        transform.GetChild(0).localRotation = Quaternion.Euler(cameraPitch, 0f, 0f); // Rotates the camera up and down

        transform.Rotate(0, lookX, 0, Space.Self); // Rotates the player left and right
    }

    void Gravity()
    {
        if (!isGrounded()) // Uses a downward raycast to see if the player is touching a surface
        {
            transform.position -= new Vector3(0, gravityConstant * Time.deltaTime, 0);
        }
    }

    // Uses a raycast to see if the player is touching the ground
    bool isGrounded()
    {
        RaycastHit hit; // Stores information on the hit
        UnityEngine.Debug.DrawRay(transform.position, -Vector3.up * 1.1f, Color.green); // DEBUG
        return Physics.SphereCast(transform.position, 0.9f, -Vector3.up, out hit, 1.1f); // Casts a sphere and returns if there was a hit
    }




    int maxBounces = 5;
    float maxSlopeAngle = 55;
    float skinWidth = 0.015f;

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
        {
            float mag = vec.magnitude;
            vec = Vector3.ProjectOnPlane(vec, normal).normalized;
            vec *= mag;
            return vec;
        }

        Bounds bounds;
        bounds = playerCollider.bounds;
        bounds.Expand(-2 * skinWidth);

        if (depth >= maxBounces) {
            return Vector3.zero;
        }

        float dist = vel.magnitude + skinWidth;

        RaycastHit hit;
        if ( Physics.SphereCast(pos, bounds.extents.x, vel.normalized, out hit, dist, default) )
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth) {
                snapToSurface = Vector3.zero;
            }

            if (angle <= maxSlopeAngle)
            {
                if (gravityPass) {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            else
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                    );

                if (isGrounded() && !gravityPass)
                {
                    leftover = ProjectAndScale(
                        new Vector3(leftover.x, 0, leftover.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z)
                        ).normalized;
                    leftover *= scale;
                }
                else {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

                float mag = leftover.magnitude;
            leftover = Vector3.ProjectOnPlane(leftover, hit.normal).normalized;
            leftover *= mag;

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }

        return vel;
    }

    void JumpValues()
    {
        float timeToApex = maxJumpTime / 2;
        //gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }
}
