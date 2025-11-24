using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float rotationSpeed = 10f;
    public float jumpForce;
    private bool hasJumped = false;
    public float maxSlopeAngle;
    private RaycastHit slopeHit;


    [Header("Camera")]
    public Transform cameraTarget;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    public float groundDamping = 5f;

    [Header("Animator / Model")]
    public Animator anim;          // Animator on child model
    public Transform model;        // Sonic model (child of player)

    // Private variables
    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private float currentSpeed = 0f;
    private bool grounded;

    // Sound Effects
    public AudioSource jumpSource;
    public AudioSource runSource;
    public AudioClip jumpSound;
    public AudioClip runningSound;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Auto-find Animator if not assigned
        if (!anim)
            anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        Jump();

        // Ground check
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // offset for pivot
        grounded = Physics.Raycast(rayOrigin, Vector3.down, playerHeight * 0.5f + 0.1f, whatIsGround);



        // Apply damping
        rb.linearDamping = grounded ? groundDamping : 0f;
       
        if (grounded)
        {
            hasJumped = false; // Reset jump when touching the ground
        }


        // Update Animator
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
{
    // --- Camera-relative input ---
    Vector3 camForward = cameraTarget.forward;
    Vector3 camRight = cameraTarget.right;
    camForward.y = 0f;
    camRight.y = 0f;
    camForward.Normalize();
    camRight.Normalize();

    Vector3 inputDir = camForward * verticalInput + camRight * horizontalInput;
    float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);

    // --- Smooth acceleration/deceleration ---
    if (inputMagnitude > 0.1f)
        currentSpeed += acceleration * Time.fixedDeltaTime;
    else
        currentSpeed -= deceleration * Time.fixedDeltaTime;

    currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    // --- Movement along slope or flat ---
    Vector3 moveVel;

    if (OnSlope() && grounded)
    {
        // Project input along slope plane
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, slopeHit.normal).normalized;
        moveVel = slopeDir * currentSpeed;

        // Preserve vertical velocity affected by gravity
        rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);
    }
    else
    {
        // Flat ground or in air
        moveVel = inputDir.normalized * currentSpeed;
        rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);
    }

    // --- Clamp horizontal speed to avoid flying off ---
    float maxSlopeSpeed = maxSpeed * 1.5f;
    Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    if (flatVel.magnitude > maxSlopeSpeed)
    {
        Vector3 clamped = flatVel.normalized * maxSlopeSpeed;
        rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
    }

    // --- Model rotation (Z-axis discrete angles) ---
    if (inputMagnitude > 0.01f && model != null)
    {
        float yRotation = 0f;

        if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
            yRotation = verticalInput > 0 ? 0f : 180f;
        else
            yRotation = horizontalInput > 0 ? 90f : -90f;

        Vector3 currentEuler = model.localEulerAngles;
        Vector3 targetEuler = new Vector3(currentEuler.x, yRotation, currentEuler.z);
        model.localRotation =
            Quaternion.Lerp(model.localRotation, Quaternion.Euler(targetEuler),
            rotationSpeed * Time.fixedDeltaTime);
    }

    // --- Running sound effect ---
    bool isMoving = inputMagnitude > 0.1f;
    bool isOnGround = grounded;

    if (isMoving && isOnGround)
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



    void Jump()
{
    if (!grounded || hasJumped)
        return;

    hasJumped = true;

    // Preserve horizontal velocity from slope
    Vector3 jumpVel = rb.linearVelocity;
    jumpVel.y = jumpForce; // set upward jump
    rb.linearVelocity = jumpVel;

    // Trigger animation & sound
    anim.SetTrigger("Jump");
    if (jumpSource)
        jumpSource.PlayOneShot(jumpSound);
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

        // Grounded parameter
        anim.SetBool("isGrounded", grounded);

        // Speed for Blend Tree (only while grounded)
        float speedPercent = grounded ? currentSpeed / maxSpeed : 0f;
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), speedPercent, Time.deltaTime * 5f));
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);

    }
}
