using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 12f;           // Maximum horizontal speed
    public float acceleration = 20f;       // How fast the player accelerates
    public float deceleration = 25f;       // How fast the player slows down
    public float rotationSpeed = 10f;      // How fast the player rotates toward movement

    [Header("Camera")]
    public Transform cameraTarget;         // Camera target for relative movement

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    public float groundDamping = 5f;       // linearDamping when grounded

    [Header("Animator")]
    public Animator anim;                  // Animator on Sonic model (child)

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

        // If Animator not assigned, try to find it in children
        if (!anim)
            anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        // Apply damping when grounded
        rb.linearDamping = grounded ? groundDamping : 0f;

        // Update animation
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

        // Apply movement while keeping vertical velocity intact
        Vector3 moveVel = inputDir.normalized * currentSpeed;
        rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);

        // Rotate toward movement direction (always faces where moving)
        if (inputMagnitude > 0.1f)
        {
            Vector3 moveDirFlat = new Vector3(inputDir.x, 0f, inputDir.z).normalized;
            if (moveDirFlat.sqrMagnitude > 0f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirFlat);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (!anim) return;

        // Normalize speed for animation (0 → 1)
        float speedPercent = currentSpeed / maxSpeed;

        // Smoothly update Speed parameter for blend tree
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), speedPercent, Time.deltaTime * 5f));
    }
}
