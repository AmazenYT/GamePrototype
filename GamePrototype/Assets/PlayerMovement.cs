using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float rotationSpeed = 10f;

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

        // Ground check
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // offset for pivot
        grounded = Physics.Raycast(rayOrigin, Vector3.down, playerHeight * 0.6f, whatIsGround);

        // Apply damping
        rb.linearDamping = grounded ? groundDamping : 0f;

        // Update Animator
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        // Camera-relative directions
        Vector3 camForward = cameraTarget.forward;
        Vector3 camRight = cameraTarget.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * verticalInput + camRight * horizontalInput;
        float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);

        // Smooth acceleration / deceleration
        if (inputMagnitude > 0.1f)
            currentSpeed += acceleration * Time.fixedDeltaTime;
        else
            currentSpeed -= deceleration * Time.fixedDeltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // Apply movement, keep vertical velocity intact
        Vector3 moveVel = inputDir.normalized * currentSpeed;
        rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);

        // Rotate child model on Z axis only (discrete angles)
        if (inputMagnitude > 0.01f && model != null)
        {
            float yRotation = 0f;

    // Determine dominant input
    if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
    {
        // Forward/back
        yRotation = verticalInput > 0 ? 0f : 180f;
    }
    else
    {
        // Left/right
        yRotation = horizontalInput > 0 ? 90f : -90f;
    }

    // Preserve X rotation (90°) and Z rotation
    Vector3 currentEuler = model.localEulerAngles;
    Vector3 targetEuler = new Vector3(currentEuler.x, yRotation, currentEuler.z);

    // Smooth rotation
    model.localRotation = Quaternion.Lerp(model.localRotation, Quaternion.Euler(targetEuler), rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimator()
    {
        if (!anim) return;

        // Grounded parameter
        anim.SetBool("isGrounded", grounded);

        // Speed for Blend Tree (only while grounded)
        float speedPercent = grounded ? currentSpeed / maxSpeed : 0f;
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), speedPercent, Time.deltaTime * 5f));
    }
}
