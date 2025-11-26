using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float rotationSpeed = 10f;
    public float jumpForce;
    private bool isMoving = false; // tracks if player is moving

    public float attackSpeed;                // <-- used by JumpDash
    private bool hasJumped = false;

    [Header("Slope Settings")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    [Header("Boost Settings")]
    public float boostSpeed = 40f;
    public float airBoostSpeed = 25f;
    public bool isBoosting = false;

    [Header("Camera")]
    public Transform cameraTarget;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    public float groundDamping = 5f;

    [Header("Animator / Model")]
    public Animator anim;
    public Transform model;

    // Private variables
    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private float currentSpeed = 0f;
    private bool grounded;

    // Jump dash state
    private bool isJumpDashing = false;
    private bool hasJumpDashed = false;

    // Sound Effects
    public AudioSource jumpSource;
    public AudioSource runSource;
    public AudioClip jumpSound;
    public AudioClip runningSound;
    public AudioSource boostSource;
    public AudioClip boostSound;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (!anim)
            anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // read inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // ground check (center offset)
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        grounded = Physics.Raycast(rayOrigin, Vector3.down, playerHeight * 0.5f + 0.1f, whatIsGround);

        // reset states when grounded
        if (grounded)
        {
            hasJumped = false;
            hasJumpDashed = false;
            isJumpDashing = false;
        }

        // apply damping
        rb.linearDamping = grounded ? groundDamping : 0f;

        // FIRST JUMP: normal jump (only if grounded)
        if (grounded && Input.GetButtonDown("Jump"))
        {
            Jump();
            // do not return — allow other update logic after jumping
        }
        else
        {
            // SECOND JUMP while airborne -> JumpDash (only if not used yet)
            if (!grounded && !hasJumpDashed && Input.GetButtonDown("Jump"))
            {
                JumpDash();
                hasJumpDashed = true;
            }
        }

        // Boost: only allow boosting if not currently jump-dashing
        if (!isJumpDashing)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                StartBoost();
            else
                StopBoost();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        // camera-relative basis
        Vector3 camForward = cameraTarget.forward;
        Vector3 camRight = cameraTarget.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * verticalInput + camRight * horizontalInput;
        float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);

        // acceleration / deceleration
        if (inputMagnitude > 0.1f)
            currentSpeed += acceleration * Time.fixedDeltaTime;
        else
            currentSpeed -= deceleration * Time.fixedDeltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // BOOST overrides normal movement (priority)
        if (isBoosting)
        {
            Vector3 forward = cameraTarget.forward;
            forward.y = 0f;
            forward.Normalize();
            float useBoost = grounded ? boostSpeed : airBoostSpeed;
            rb.linearVelocity = new Vector3(forward.x * useBoost, rb.linearVelocity.y, forward.z * useBoost);
            return;
        }

       
        if (isJumpDashing)
        {
            
            return;
        }

        // normal movement 
        Vector3 moveVel;
        if (OnSlope() && grounded)
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, slopeHit.normal).normalized;
            moveVel = slopeDir * currentSpeed;
        }
        else
        {
            moveVel = inputDir.normalized * currentSpeed;
        }

        // apply horizontal velocity while preserving vertical
        rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);

        // clamp horizontal speed
        float maxSlopeSpeed = maxSpeed * 1.5f;
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > maxSlopeSpeed)
        {
            Vector3 clamped = flatVel.normalized * maxSlopeSpeed;
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }

        // model rotation 
        if (inputMagnitude > 0.01f && model != null)
        {
            float yRotation = 0f;
            if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
                yRotation = verticalInput > 0 ? 0f : 180f;
            else
                yRotation = horizontalInput > 0 ? 90f : -90f;

            Vector3 currentEuler = model.localEulerAngles;
            Vector3 targetEuler = new Vector3(currentEuler.x, yRotation, currentEuler.z);
            model.localRotation = Quaternion.Lerp(model.localRotation, Quaternion.Euler(targetEuler), rotationSpeed * Time.fixedDeltaTime);
        }

        // running SFX & movement flag
    isMoving = inputMagnitude > 0.1f;

    if (isMoving && grounded)
    {
    if (!runSource.isPlaying)
        runSource.PlayOneShot(runningSound);
    }
    else
        {
            if (runSource.isPlaying)
                runSource.Stop();
        }
    }


    private void StartBoost()
    {
        if (!isMoving) return;
        isBoosting = true;

        if (isBoosting && !boostSource.isPlaying)
        {
            boostSource.clip = boostSound;
            boostSource.loop = true;
            boostSource.Play();   
        }
       
    
    }

    private void StopBoost()
    {
     if (!isBoosting) return;
     isBoosting = false;

         if (boostSource && boostSource.isPlaying)
         {
            boostSource.Stop();
         }
    }


    // normal single jump - preserves horizontal momentum
    void Jump()
    {
        if (!grounded || hasJumped)
            return;

        hasJumped = true;

        Vector3 newVelocity = rb.linearVelocity;
        newVelocity.y = jumpForce;
        rb.linearVelocity = newVelocity;

        anim.SetTrigger("Jump");
        if (jumpSource) jumpSource.PlayOneShot(jumpSound);
    }

    // jump dash using attackSpeed (sets horizontal velocity once)
    void JumpDash()
    {
        if (isJumpDashing) return;

        isJumpDashing = true;

        // determine horizontal dash direction:
        // prefer current horizontal momentum; if nearly zero, use camera forward
        Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horiz.magnitude < 0.1f)
        {
            Vector3 f = cameraTarget.forward;
            f.y = 0f;
            horiz = f.normalized;
        }
        else
        {
            horiz = horiz.normalized;
        }

        // apply dash: keep vertical velocity unchanged
        rb.linearVelocity = new Vector3(horiz.x * attackSpeed, rb.linearVelocity.y, horiz.z * attackSpeed);
        Debug.Log("JumpDash applied, attackSpeed = " + attackSpeed);
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle > 0f;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 inputDir)
    {
        return Vector3.ProjectOnPlane(inputDir, slopeHit.normal).normalized;
    }

    private void UpdateAnimator()
    {
        if (!anim) return;

        anim.SetBool("isGrounded", grounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        float speedPercent = grounded ? currentSpeed / maxSpeed : 0f;
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), speedPercent, Time.deltaTime * 5f));
    }
}
