using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTools : MonoBehaviour
{
    public enum LHTools 
    { 
        None,
        SpecialBlast,   // Launches the player away from where they are looking
        ZapArm,         // Lets the user charge up a punch, which when released launches a lightning in the direction you are looking
        Float           // Temporarily disables your gravity, allowing you float in the direction you are heading in
    }

    public enum RHTools
    {
        None,
        Wire,           // Launches a hook into the world, allowing the player to swing around
        Zippity         // Launches a hook into the world, pulling the player towards it while draining a cell
    }

    [Header("References")]
    public Transform LeftHandGunTip;
    public Transform RightHandGunTip;
    public Transform CamTransform;
    public Transform PlayerTransform;

    [Header("Ammo")]
    public int CellCount;
    public float CellSize;

    // This should be thought of like a stamina bar, moves will either have a tick usage, or a instant usage
    public float CurCellCharge;

    [Header("Special blast stats")]
    public AudioSource SBSound;
    public ParticleSystem SBParticles;
    public float SB_Cost;
    public float SB_Force;

    [Header("Hook stats")]
    public AudioSource WireHookConnect;
    public AudioSource WireHookDisconnect;
    public LineRenderer WireHookLineRenderer;
    public float WireHookCostRate;
    public LayerMask Grapplable;
    public float MaxWireHookDist = 25f;
    public float MinWireHookDist = 20f;
    private Vector3 SwingPoint;
    private SpringJoint SwingJoint;

    [Header("Hook Prediction")]
    public GameObject hookRedicle;
    private RaycastHit HookPredictionHit;
    public float HookPredictionSphereCastRadius;
    private Transform predictionPoint;
    private Vector3 currentGrapplePos;

    // Left hand inputs
    bool M0Press;
    bool M0Release;
    float M0Hold;

    // Right hand inputs
    bool M1Press;
    bool M1Release;
    float M1Hold;

    // Other player objects
    PlayerMove myMove;
    Rigidbody myRigidBody;
    Camera myPOVCamera;

    // Currently equipped tools
    [Header("Currently equipped")]
    public LHTools CurLeftTool = LHTools.SpecialBlast;
    public RHTools CurRightTool = RHTools.Wire;


    void Start()
    {
        myMove = GetComponent<PlayerMove>();
        myRigidBody = GetComponent<Rigidbody>();
        myPOVCamera = Camera.main;

        if(predictionPoint == null)
        {
            predictionPoint = Instantiate(hookRedicle).transform;
        }

        SwingJoint = PlayerTransform.gameObject.AddComponent<SpringJoint>();
        SwingJoint.autoConfigureConnectedAnchor = false;
        SwingJoint.connectedAnchor = Vector3.up*-11;
        SwingJoint.spring = 5f;
        SwingJoint.damper = 10f;
        SwingJoint.massScale = 2f;
        WireHookLineRenderer.positionCount = 2;
        WireHookLineRenderer.positionCount = 0;
        Destroy(SwingJoint, 0.1f);

    }

    void Update()
    {
        UpdateToolsInput();
        CheckForSwingPoints();
        UpdateWireHookLength();

        switch (CurLeftTool)
        {
            case LHTools.SpecialBlast:
                AttemptSpecialBlast(M0Press);
                break;
        }

        switch (CurRightTool)
        {
            case RHTools.Wire:
                if (M1Press) StartWireHook();
                if (M1Release) StopWireHook();
                break;
        }
    }

    void LateUpdate()
    {
        DrawWireHook();    
    }

    private void UpdateToolsInput()
    {
        M0Press = Input.GetButtonDown("Fire0");
        M0Release = Input.GetButtonUp("Fire0");

        M1Press = Input.GetButtonDown("Fire1");
        M1Release = Input.GetButtonUp("Fire1");
    }

    public bool DoIHaveEnoughCharge(float charge)
    {
        if (CurCellCharge >= charge)
        {
            return true;
        }
        return false;
    }

    public void ResetCells()
    {
        CurCellCharge = CellSize * CellCount;
    }

    // Special Blast
    private void AttemptSpecialBlast(bool M0Press)
    {
        // Check if we have Ammo
        // Check if we are grounded
        // If we are grounded, we will still launch the attack, but it will not knock away the player
        // If we are in the air, we will launch the attack and the player through the air

        switch (M0Press)
        {
            case false:
                return;
        }

        // SB uses an entire cell, so we need to see if we have a whole cell's worth of charge available
        switch (DoIHaveEnoughCharge(CellSize))
        {
            case false:
                return;
        }

        // Apply the launch effects
        CurCellCharge -= SB_Cost;
        SBSound.Play();
        SBParticles.Play();
        switch (myMove.GetGrounded())
        {
            case false:
                // Launch user away
                myRigidBody.AddForce(-1 * myPOVCamera.transform.forward * (SB_Force * CellSize), ForceMode.Impulse);
                break;
            case true:
                Vector3 direction = (-1 * myPOVCamera.transform.forward + this.gameObject.transform.up).normalized;
                myRigidBody.AddForce(direction * SB_Force, ForceMode.Impulse);

                break;
        }
    }

    // Hook helpers
    private void CheckForSwingPoints()
    {
        if (SwingJoint != null) return;
        switch (CurRightTool)
        {
            case RHTools.None:
                predictionPoint.gameObject.SetActive(false);
                return;
        }

        // Raycast for exactly what we are looking at
        RaycastHit raycastHit;
        Physics.Raycast(myPOVCamera.transform.position, myPOVCamera.transform.forward, out raycastHit, MaxWireHookDist);

        // We should prioritize objects that have the grapplable tag
        RaycastHit grappleSphereCastHit;
        Physics.SphereCast(myPOVCamera.transform.position, HookPredictionSphereCastRadius, myPOVCamera.transform.forward, out grappleSphereCastHit, MaxWireHookDist, Grapplable);

        // Finally tag anything else
        RaycastHit anySphereCastHit;
        Physics.SphereCast(myPOVCamera.transform.position, HookPredictionSphereCastRadius, myPOVCamera.transform.forward, out anySphereCastHit, MaxWireHookDist);

        HookPredictionHit = DecideTarget(raycastHit, grappleSphereCastHit, anySphereCastHit);
        Vector3 realHitPoint = HookPredictionHit.point;

        switch (realHitPoint)
        {
            case Vector3 hit when hit != Vector3.zero:
                predictionPoint.gameObject.SetActive(true);
                predictionPoint.position = realHitPoint;
                break;

            case Vector3 hit when hit == Vector3.zero:
                predictionPoint.gameObject.SetActive(false);
                break;
        }

        RaycastHit DecideTarget(RaycastHit raycast, RaycastHit grapplecast, RaycastHit anycast)
        {
            switch (raycast)
            {
                case RaycastHit hit when hit.point != Vector3.zero:
                    switch (canBeGrappled(hit))
                    {
                        case false:
                            break;
                    }
                    return raycast;
            }
            switch (grapplecast)
            {
                case RaycastHit hit when hit.point != Vector3.zero:
                    switch (canBeGrappled(hit))
                    {
                        case false:
                            break;
                    }
                    return grapplecast;
            }
            switch (anycast)
            {
                case RaycastHit hit when hit.point != Vector3.zero:
                    switch (canBeGrappled(hit))
                    {
                        case false:
                            break;
                    }
                    return anycast;
            }

            return new RaycastHit();
        }

        bool canBeGrappled(RaycastHit hit)
        {
            if (hit.collider.gameObject.CompareTag("Ungrapplable") == true)
            {
                return false;
            }
            if (hit.collider.gameObject.CompareTag("Player") == true)
            {
                return false;
            }
            if (hit.collider.gameObject.CompareTag("MainCamera") == true)
            {
                Debug.Log("Camera hit during raycast");
                return false;
            }
            return true;
        }
    }

    private void UpdateWireHookLength()
    {
        if (!SwingJoint) return;

        float distanceFromPoint = Vector3.Distance(PlayerTransform.position, SwingPoint);

        if (distanceFromPoint >= SwingJoint.maxDistance) 
        {
            return;
        }

        SwingJoint.maxDistance = distanceFromPoint;

        if (distanceFromPoint < MinWireHookDist)
        {
            SwingJoint.maxDistance = MinWireHookDist;
        }

    }

    // Wire hook
    private void StartWireHook()
    {
        if (predictionPoint.gameObject.activeSelf == false) return;
        switch (CurCellCharge)
        {
            case <= 0:
                return;
        }

        myMove.WireHookState = true;

        SwingPoint = HookPredictionHit.point;
        SwingJoint = PlayerTransform.gameObject.AddComponent<SpringJoint>();
        SwingJoint.autoConfigureConnectedAnchor = false;
        SwingJoint.connectedAnchor = SwingPoint;

        float distanceFromPoint = Vector3.Distance(PlayerTransform.position, SwingPoint);

        SwingJoint.maxDistance = distanceFromPoint * .7f;
        SwingJoint.minDistance = -10;

        SwingJoint.spring = 5f;
        SwingJoint.damper = 10f;
        SwingJoint.massScale = 500f;

        WireHookLineRenderer.positionCount = 2;

        currentGrapplePos = RightHandGunTip.position;

        WireHookConnect.Play();

    }

    private void StopWireHook()
    {
        switch (myMove.WireHookState)
        {
            case false:
                return;
        }

        myMove.WireHookState = false;
        WireHookLineRenderer.positionCount = 0;
        Destroy(SwingJoint);

        WireHookDisconnect.Play();
    }

    void DrawWireHook()
    {
        if (!SwingJoint) return;
        if (WireHookLineRenderer.positionCount == 0) return;


        currentGrapplePos = Vector3.Lerp(currentGrapplePos, SwingPoint, Time.deltaTime * 4f);

        WireHookLineRenderer.SetPosition(0, RightHandGunTip.position);
        WireHookLineRenderer.SetPosition(1, currentGrapplePos);

        CurCellCharge -= WireHookCostRate * Time.deltaTime;

        switch (CurCellCharge)
        {
            case <= 0:
                StopWireHook();
                return;
        }
    }

}

/*if (raycastHit.point != Vector3.zero && raycastHit.collider.gameObject.CompareTag("Player") == false)
        {
            realHitPoint = raycastHit.point;
            HookPredictionHit = raycastHit;
        }
        else if (grappleSphereCastHit.point != Vector3.zero && grappleSphereCastHit.collider.gameObject.CompareTag("Player") == false)
        {
            realHitPoint = grappleSphereCastHit.point;
            HookPredictionHit = grappleSphereCastHit;
        }
        else if (anySphereCastHit.point != Vector3.zero && anySphereCastHit.collider.gameObject.CompareTag("Player") == false)
        {
            realHitPoint = anySphereCastHit.point;
            HookPredictionHit = anySphereCastHit;
        }
        else
        {
            realHitPoint = Vector3.zero;
        }*/