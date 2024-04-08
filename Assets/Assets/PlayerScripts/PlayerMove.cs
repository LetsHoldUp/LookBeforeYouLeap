using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float fallMultiplier;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool jumpReady = true;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground check")]
    public float playerHeight;
    public LayerMask GroundMask;
    bool grounded;

    [Header("Other Objects")]
    public AudioSource landSound;
    public Transform orientation;
    PlayerTools myTools;
    Rigidbody myRigidBody;

    [Header("Movement states")]
    public bool WireHookState = false;

    // Left, Right, Forward, Back movement inputs
    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    void Start()
    {
        myRigidBody = GetComponent<Rigidbody>();
        myRigidBody.freezeRotation = true;

        myTools = GetComponent<PlayerTools>();
    }

    void Update()
    {
        UpdateMoveInput();
        CapMoveSpeed();
        HastenFall();

        bool thisframegrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, GroundMask);

        // If we just landed, refill our cells
        switch (thisframegrounded)
        {
            case true:
                if(grounded.Equals(thisframegrounded) == false)
                {
                    landSound.Play();
                    myTools.ResetCells();
                }
                break;
        }

        grounded = thisframegrounded;

        switch (grounded)
        {
            case true:
                myRigidBody.drag = groundDrag;
                break;
            case false:
                myRigidBody.drag = 0;
                break;
        }
    
        if(Input.GetKey(jumpKey) && jumpReady && grounded)
        {
            Jump();

            jumpReady = false;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void UpdateMoveInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void MovePlayer()
    {
        /*switch (WireHookState)
        {
            case true:
                return;
        }*/

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        switch (grounded)
        {
            case true:
                myRigidBody.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
                break;
            case false:
                myRigidBody.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

                break;
        }

    }

    private void CapMoveSpeed()
    {
        switch (grounded)
        {
            case false:
                return;
        }

        Vector3 flatVelocity = new Vector3(myRigidBody.velocity.x, 0f, myRigidBody.velocity.z);

        if (flatVelocity.magnitude > moveSpeed)
        {
            Vector3 cappedVelocity = flatVelocity.normalized * moveSpeed;
            myRigidBody.velocity = new Vector3(cappedVelocity.x, myRigidBody.velocity.y, cappedVelocity.z);
        }
    }

    private void Jump()
    {
        myRigidBody.velocity = new Vector3(myRigidBody.velocity.x, 0f, myRigidBody.velocity.z);

        myRigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        jumpReady = true;
    }

    public bool GetGrounded()
    {
        return grounded;
    }

    public void KillVelocity()
    {
        myRigidBody.velocity = Vector3.zero;
    }

    private void HastenFall()
    {
        switch (WireHookState)
        {
            case true:
                return;
        }

        switch (myRigidBody.velocity.y)
        {
            case < 0:
                myRigidBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier) * Time.deltaTime;
                break;
        }
    }
}
